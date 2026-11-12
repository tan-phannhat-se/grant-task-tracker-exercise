import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { GrantDetail, GrantListItem, OverdueTask } from './api/grantsApi'
import App from './App'

const closedGrant: GrantListItem = {
  id: 3,
  referenceNumber: 'GR-100003',
  organisationName: 'Harbour Arts Collective',
  applicationDate: '2026-04-25',
  status: 'Closed',
  pendingTaskCount: 0,
}

const openGrant: GrantListItem = {
  id: 1,
  referenceNumber: 'GR-100001',
  organisationName: 'Riverside Community Garden Inc.',
  applicationDate: '2026-09-12',
  status: 'Open',
  pendingTaskCount: 1,
}

const overdueTask: OverdueTask = {
  id: 2,
  grantId: 1,
  title: 'Verify bank details',
  priority: 'High',
  referenceNumber: 'GR-100001',
  organisationName: 'Riverside Community Garden Inc.',
  daysOverdue: 3,
}

const answers: Record<string, unknown> = {
  '/api/grants?status=All': [openGrant, closedGrant],
  '/api/grants?status=Closed': [closedGrant],
  '/api/grants/1': { ...openGrant, tasks: [] } satisfies GrantDetail,
  '/api/grants/3': { ...closedGrant, tasks: [] } satisfies GrantDetail,
  '/api/tasks/overdue?grantStatus=Open': [overdueTask],
}

/** Answers like the API, by URL; a URL with no answer gets a 404. */
function fakeApi(url: string): Promise<Response> {
  const body = answers[url]
  return Promise.resolve(body ? Response.json(body) : new Response(null, { status: 404 }))
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(fakeApi))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('App', () => {
  /**
   * FE-34: Grants, filter Closed, open GR-100003 → its detail; Back → the grant list,
   * still filtered on Closed.
   * Catches: the wrong grant opened, or the officer's filter lost on the way back.
   */
  it('OpenGrantFromList_BackKeepsTheFilter', async () => {
    const user = userEvent.setup()
    render(<App />)
    await screen.findByText('GR-100001')
    await user.selectOptions(screen.getByLabelText('Status'), 'Closed')

    await user.click(await screen.findByText('Harbour Arts Collective'))

    expect(
      await screen.findByRole('heading', { name: 'GR-100003 · Harbour Arts Collective' }),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: '← Back' }))

    expect(await screen.findByText('GR-100003')).toBeInTheDocument()
    expect(screen.getByLabelText('Status')).toHaveValue('Closed')
  })

  /**
   * FE-35: Overdue tasks, open a task's row → its grant's detail; Back → the overdue list.
   * Catches: Back always going to the grant list, so the officer loses the overdue list.
   */
  it('OpenGrantFromOverdueList_BackReturnsToTheOverdueList', async () => {
    const user = userEvent.setup()
    render(<App />)

    await user.click(screen.getByRole('button', { name: 'Overdue tasks' }))
    await user.click(await screen.findByText('Verify bank details'))

    expect(
      await screen.findByRole('heading', { name: 'GR-100001 · Riverside Community Garden Inc.' }),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: '← Back' }))

    expect(await screen.findByRole('heading', { name: 'Overdue tasks' })).toBeInTheDocument()
  })
})
