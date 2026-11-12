import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { GrantListItem, GrantStatusFilter } from './api/grantsApi'
import GrantList from './GrantList'

const openGrant: GrantListItem = {
  id: 1,
  referenceNumber: 'GR-100001',
  organisationName: 'Riverside Community Garden Inc.',
  applicationDate: '2026-09-12',
  status: 'Open',
  pendingTaskCount: 2,
}

const closedGrant: GrantListItem = {
  id: 3,
  referenceNumber: 'GR-100003',
  organisationName: 'Harbour Arts Collective',
  applicationDate: '2026-04-25',
  status: 'Closed',
  pendingTaskCount: 0,
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
    '/api/grants?status=All': () => Promise.resolve(Response.json([openGrant, closedGrant])),
    '/api/grants?status=Open': () => Promise.resolve(Response.json([openGrant])),
    '/api/grants?status=Closed': () => Promise.resolve(Response.json([closedGrant])),
  }
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  vi.unstubAllGlobals()
  fetchMock.mockClear()
})

/** The list with its filter held in state, as the app holds it. */
function GrantListWithFilter({ onOpen }: { onOpen: (grantId: number) => void }) {
  const [status, setStatus] = useState<GrantStatusFilter>('All')
  return <GrantList status={status} onStatusChange={setStatus} onOpen={onOpen} />
}

/** The table row that holds the given reference number. */
function rowOf(referenceNumber: string): HTMLElement {
  return screen.getByRole('row', { name: new RegExp(referenceNumber) })
}

describe('GrantList', () => {
  /**
   * FE-19: opens on every grant → each row shows reference, organisation, application date,
   * status and the number of pending tasks.
   * Catches: the pending task count missing, or taken from the wrong field.
   */
  it('Opens_ShowsEachGrantWithItsPendingTaskCount', async () => {
    render(<GrantListWithFilter onOpen={() => {}} />)

    await screen.findByText('GR-100001')
    const cells = within(rowOf('GR-100001'))
      .getAllByRole('cell')
      .map((cell) => cell.textContent)

    expect(cells).toEqual([
      'GR-100001',
      'Riverside Community Garden Inc.',
      '2026-09-12',
      'Open',
      '2',
    ])
    expect(fetchMock).toHaveBeenCalledWith('/api/grants?status=All', expect.anything())
  })

  /**
   * FE-20: choose Closed → the API is asked for closed grants; only the rows it returns show.
   * Catches: the filter ignored, or applied on the screen instead of by the API.
   */
  it('ChooseClosed_ShowsOnlyTheGrantsTheApiReturns', async () => {
    const user = userEvent.setup()
    render(<GrantListWithFilter onOpen={() => {}} />)
    await screen.findByText('GR-100001')

    await user.selectOptions(screen.getByLabelText('Status'), 'Closed')

    expect(await screen.findByText('GR-100003')).toBeInTheDocument()
    expect(screen.queryByText('GR-100001')).not.toBeInTheDocument()
    expect(fetchMock).toHaveBeenCalledWith('/api/grants?status=Closed', expect.anything())
  })

  /**
   * FE-21: the API returns no grant for the filter → "No grants match this filter.", no table.
   * Catches: a bare table header that looks like a loading error.
   */
  it('NoGrantForTheFilter_SaysSo', async () => {
    answers['/api/grants?status=Closed'] = () => Promise.resolve(Response.json([]))
    const user = userEvent.setup()
    render(<GrantListWithFilter onOpen={() => {}} />)
    await screen.findByText('GR-100001')

    await user.selectOptions(screen.getByLabelText('Status'), 'Closed')

    expect(await screen.findByText('No grants match this filter.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  /**
   * FE-22a: a filter fails to load → its message, and the previous filter's rows are gone.
   * Catches: rows of another filter shown under the new one.
   */
  it('LoadFails_ShowsMessageAndNoRows', async () => {
    answers['/api/grants?status=Open'] = () => Promise.resolve(new Response(null, { status: 503 }))
    const user = userEvent.setup()
    render(<GrantListWithFilter onOpen={() => {}} />)
    await screen.findByText('GR-100001')

    await user.selectOptions(screen.getByLabelText('Status'), 'Open')

    expect(await screen.findByRole('alert')).toHaveTextContent(unreachableMessage)
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  /**
   * FE-22b: a message is shown, then the next filter loads → the message is gone.
   * Catches: an old error left on screen after the server is back.
   */
  it('LoadFails_NextLoadClearsTheMessage', async () => {
    answers['/api/grants?status=Open'] = () => Promise.resolve(new Response(null, { status: 503 }))
    const user = userEvent.setup()
    render(<GrantListWithFilter onOpen={() => {}} />)
    await screen.findByText('GR-100001')
    await user.selectOptions(screen.getByLabelText('Status'), 'Open')
    await screen.findByRole('alert')

    await user.selectOptions(screen.getByLabelText('Status'), 'Closed')

    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  /**
   * FE-23: the filter changes again before the first answer comes → the latest filter's rows
   * stay when the older answer arrives.
   * Catches: an older answer replacing a newer one, so the rows do not match the filter shown.
   */
  it('OlderAnswerArrivesLast_KeepsTheLatestFiltersRows', async () => {
    let answerOpen: () => void = () => {}
    answers['/api/grants?status=Open'] = () =>
      new Promise<Response>((resolve) => {
        answerOpen = () => resolve(Response.json([openGrant]))
      })
    const user = userEvent.setup()
    render(<GrantListWithFilter onOpen={() => {}} />)
    await screen.findByText('GR-100001')

    await user.selectOptions(screen.getByLabelText('Status'), 'Open')
    await user.selectOptions(screen.getByLabelText('Status'), 'Closed')
    await screen.findByText('GR-100003')
    await act(async () => answerOpen())

    expect(screen.getByText('GR-100003')).toBeInTheDocument()
    expect(screen.queryByText('GR-100001')).not.toBeInTheDocument()
  })

  /**
   * FE-24a: click anywhere on a grant's row → onOpen with that grant's id.
   * Catches: the wrong grant opened, or only part of the row clickable.
   */
  it('ClickARow_OpensThatGrant', async () => {
    const onOpen = vi.fn()
    const user = userEvent.setup()
    render(<GrantListWithFilter onOpen={onOpen} />)
    await screen.findByText('GR-100003')

    await user.click(screen.getByText('Harbour Arts Collective'))

    expect(onOpen).toHaveBeenCalledWith(3)
  })

  /**
   * FE-24b: Tab to a grant's reference and press Enter → onOpen once, with that grant's id.
   * Catches: a grant that only a mouse can open, or one press opening it twice.
   */
  it('EnterOnTheReference_OpensThatGrantOnce', async () => {
    const onOpen = vi.fn()
    const user = userEvent.setup()
    render(<GrantListWithFilter onOpen={onOpen} />)
    await screen.findByText('GR-100003')

    screen.getByRole('button', { name: 'GR-100003' }).focus()
    await user.keyboard('{Enter}')

    expect(onOpen).toHaveBeenCalledTimes(1)
    expect(onOpen).toHaveBeenCalledWith(3)
  })
})
