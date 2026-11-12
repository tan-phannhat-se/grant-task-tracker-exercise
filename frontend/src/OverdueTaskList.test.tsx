import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { GrantStatusFilter, OverdueTask } from './api/grantsApi'
import OverdueTaskList from './OverdueTaskList'

// Task ids differ from grant ids, so a task id passed as a grant id shows.
const agreementTask: OverdueTask = {
  id: 11,
  grantId: 1,
  title: 'Send the signed funding agreement',
  priority: 'High',
  referenceNumber: 'GR-100001',
  organisationName: 'Riverside Community Garden Inc.',
  daysOverdue: 5,
}

const progressTask: OverdueTask = {
  id: 21,
  grantId: 2,
  title: 'Check the progress report',
  priority: 'Medium',
  referenceNumber: 'GR-100002',
  organisationName: 'Northside Youth Football Club',
  daysOverdue: 2,
}

const closedGrantTask: OverdueTask = {
  id: 31,
  grantId: 3,
  title: 'Archive the final report',
  priority: 'Low',
  referenceNumber: 'GR-100003',
  organisationName: 'Harbour Arts Collective',
  daysOverdue: 9,
}

const unreachableMessage = 'Cannot reach the server. Check your connection and try again.'

// The API's answer for each filter; a test may replace one.
let answers: Record<string, () => Promise<Response>>

/** Answers like the API, by URL; a URL with no answer gets a 404. */
function fakeApi(url: string): Promise<Response> {
  const answer = answers[url]
  return answer ? answer() : Promise.resolve(new Response(null, { status: 404 }))
}

const fetchMock = vi.fn(fakeApi)

beforeEach(() => {
  answers = {
    '/api/tasks/overdue?grantStatus=Open': () =>
      Promise.resolve(Response.json([agreementTask, progressTask])),
    '/api/tasks/overdue?grantStatus=Closed': () =>
      Promise.resolve(Response.json([closedGrantTask])),
    '/api/tasks/overdue?grantStatus=All': () =>
      Promise.resolve(Response.json([closedGrantTask, agreementTask, progressTask])),
  }
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  vi.unstubAllGlobals()
  fetchMock.mockClear()
})

/** Holds the filter like the parent screen does, starting at Open. */
function ListWithFilter({ onOpenGrant = () => {} }: { onOpenGrant?: (grantId: number) => void }) {
  const [grantStatus, setGrantStatus] = useState<GrantStatusFilter>('Open')
  return (
    <OverdueTaskList
      grantStatus={grantStatus}
      onGrantStatusChange={setGrantStatus}
      onOpenGrant={onOpenGrant}
    />
  )
}

/** The table row that holds the given reference number. */
function rowOf(referenceNumber: string): HTMLElement {
  return screen.getByRole('row', { name: new RegExp(referenceNumber) })
}

describe('OverdueTaskList', () => {
  /**
   * FE-28: opens with Open → the API is asked for grantStatus=Open; each row shows reference,
   * organisation, task title, priority and days overdue.
   * Catches: the wrong default filter, or days overdue or priority missing.
   */
  it('Opens_AsksForOpenGrantsAndShowsPriorityAndDaysOverdue', async () => {
    render(<ListWithFilter />)

    await screen.findByText('GR-100001')
    const cells = within(rowOf('GR-100001'))
      .getAllByRole('cell')
      .map((cell) => cell.textContent)

    expect(cells).toEqual([
      'GR-100001',
      'Riverside Community Garden Inc.',
      'Send the signed funding agreement',
      'High',
      '5',
    ])
    expect(screen.getByLabelText('Grant status')).toHaveValue('Open')
    expect(fetchMock).toHaveBeenCalledWith('/api/tasks/overdue?grantStatus=Open', expect.anything())
  })

  /**
   * FE-29: choose All → the API is asked with grantStatus=All; only the rows it returns show.
   * Catches: the filter ignored, or applied on the screen instead of by the API.
   */
  it('ChooseAll_AsksTheApiForEveryGrant', async () => {
    answers['/api/tasks/overdue?grantStatus=All'] = () =>
      Promise.resolve(Response.json([closedGrantTask]))
    const user = userEvent.setup()
    render(<ListWithFilter />)
    await screen.findByText('GR-100001')

    await user.selectOptions(screen.getByLabelText('Grant status'), 'All')

    expect(await screen.findByText('GR-100003')).toBeInTheDocument()
    expect(screen.queryByText('GR-100001')).not.toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/tasks/overdue?grantStatus=All', expect.anything())
  })

  /**
   * FE-30: no overdue task for the filter → "No overdue tasks.", no table.
   * Catches: a bare table header that looks like a loading error.
   */
  it('NoOverdueTask_SaysSo', async () => {
    answers['/api/tasks/overdue?grantStatus=Open'] = () => Promise.resolve(Response.json([]))
    render(<ListWithFilter />)

    expect(await screen.findByText('No overdue tasks.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  /**
   * FE-31: a filter fails to load (503) → the server-unreachable message and no rows;
   * the next filter loads → the message is gone.
   * Catches: an old error left on screen, or another filter's rows under the new one.
   */
  it('LoadFails_ShowsMessageAndNoRows_NextLoadClearsIt', async () => {
    answers['/api/tasks/overdue?grantStatus=Closed'] = () =>
      Promise.resolve(new Response(null, { status: 503 }))
    const user = userEvent.setup()
    render(<ListWithFilter />)
    await screen.findByText('GR-100001')

    await user.selectOptions(screen.getByLabelText('Grant status'), 'Closed')

    expect(await screen.findByRole('alert')).toHaveTextContent(unreachableMessage)
    expect(screen.queryByRole('table')).not.toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Grant status'), 'All')

    expect(await screen.findByText('GR-100003')).toBeInTheDocument()
    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  /**
   * FE-32: the filter changes again before the first answer comes → the latest filter's rows
   * stay when the older answer arrives.
   * Catches: an older answer replacing a newer one, so the rows do not match the filter shown.
   */
  it('OlderAnswerArrivesLast_KeepsTheLatestFiltersRows', async () => {
    let answerAll: () => void = () => {}
    answers['/api/tasks/overdue?grantStatus=All'] = () =>
      new Promise<Response>((resolve) => {
        answerAll = () => resolve(Response.json([closedGrantTask, agreementTask]))
      })
    const user = userEvent.setup()
    render(<ListWithFilter />)
    await screen.findByText('GR-100001')

    await user.selectOptions(screen.getByLabelText('Grant status'), 'All')
    await user.selectOptions(screen.getByLabelText('Grant status'), 'Closed')
    await screen.findByText('GR-100003')
    await act(async () => answerAll())

    expect(screen.getByText('GR-100003')).toBeInTheDocument()
    expect(screen.queryByText('GR-100001')).not.toBeInTheDocument()
  })

  /**
   * FE-33: click a row → onOpenGrant with the task's grant id; press Enter on the reference
   * button → the grant opens too, once.
   * Catches: the task id passed as a grant id, or the grant not reachable by keyboard.
   */
  it('ClickARowOrPressEnter_OpensTheTasksGrant', async () => {
    const onOpenGrant = vi.fn()
    const user = userEvent.setup()
    render(<ListWithFilter onOpenGrant={onOpenGrant} />)
    await screen.findByText('GR-100002')

    await user.click(screen.getByText('Northside Youth Football Club'))

    expect(onOpenGrant).toHaveBeenCalledWith(2)

    onOpenGrant.mockClear()
    screen.getByRole('button', { name: 'GR-100002' }).focus()
    await user.keyboard('{Enter}')

    expect(onOpenGrant).toHaveBeenCalledTimes(1)
    expect(onOpenGrant).toHaveBeenCalledWith(2)
  })
})
