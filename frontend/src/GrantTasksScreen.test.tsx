import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { GrantDetail, GrantListItem, Task } from './api/grantsApi'
import GrantTasksScreen from './GrantTasksScreen'

const grants: GrantListItem[] = [
  {
    id: 1,
    referenceNumber: 'GR-100001',
    organisationName: 'Riverside Community Garden Inc.',
    applicationDate: '2026-09-12',
    status: 'Open',
    pendingTaskCount: 1,
  },
  {
    id: 3,
    referenceNumber: 'GR-100003',
    organisationName: 'Harbour Arts Collective',
    applicationDate: '2026-04-25',
    status: 'Closed',
    pendingTaskCount: 0,
  },
]

function pendingTask(id: number, grantId: number, title: string): Task {
  return {
    id,
    grantId,
    title,
    dueDate: '2026-11-20',
    priority: 'High',
    completedDate: null,
    isOverdue: false,
    completedLate: false,
  }
}

/** The grants by URL, new for each test, so a task added in one test is never seen by another. */
function freshGrantDetails(): Record<string, GrantDetail> {
  return {
    '/api/grants/1': {
      ...grants[0],
      tasks: [pendingTask(1, 1, 'Verify bank details'), pendingTask(2, 1, 'Request quote')],
    },
    '/api/grants/3': { ...grants[1], tasks: [pendingTask(4, 3, 'Final acquittal review')] },
  }
}

let grantDetails = freshGrantDetails()

// A date long past, as when a repeat keeps the first date: the screen must show it, not today.
const completedTask: Task = {
  ...pendingTask(1, 1, 'Verify bank details'),
  completedDate: '2026-11-01',
}

// What the API sends when a task cannot be found, e.g. it was deleted after the screen loaded.
const taskNotFound = new Response(
  JSON.stringify({ status: 404, detail: 'Task 2 was not found.', code: 'task-not-found' }),
  { status: 404, headers: { 'Content-Type': 'application/problem+json' } },
)

/**
 * Answers like the API: the grants, one grant, adding a task to grant 1,
 * completing task 1 (completing task 2 fails).
 */
function fakeApi(url: string, init?: RequestInit): Promise<Response> {
  if (init?.method === 'POST' && url === '/api/grants/1/tasks') {
    const added: Task = { ...pendingTask(10, 1, ''), ...JSON.parse(String(init.body)) }
    grantDetails['/api/grants/1'].tasks.push(added)
    return Promise.resolve(Response.json(added, { status: 201 }))
  }
  if (init?.method === 'PUT') {
    const isTaskOne = url === '/api/tasks/1/complete'
    return Promise.resolve(isTaskOne ? Response.json(completedTask) : taskNotFound.clone())
  }
  const body = url === '/api/grants' ? grants : grantDetails[url]
  return Promise.resolve(body ? Response.json(body) : new Response(null, { status: 404 }))
}

beforeEach(() => {
  grantDetails = freshGrantDetails()
  vi.stubGlobal('fetch', vi.fn(fakeApi))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('GrantTasksScreen', () => {
  /**
   * FE-1: opens on the first grant and shows its tasks;
   * choosing another grant → its tasks and status.
   * Catches: tasks not reloaded when the grant changes, or the screen opening empty.
   */
  it('ChooseGrant_ShowsThatGrantsTasksAndStatus', async () => {
    const user = userEvent.setup()
    render(<GrantTasksScreen />)

    expect(await screen.findByText('Verify bank details')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Grant'), 'GR-100003 · Harbour Arts Collective')

    expect(await screen.findByText('Final acquittal review')).toBeInTheDocument()
    expect(screen.queryByText('Verify bank details')).not.toBeInTheDocument()
    expect(screen.getByText('Closed')).toBeInTheDocument()
  })

  /**
   * FE-5: click Complete → PUT /api/tasks/1/complete;
   * the row shows the completed date from the response.
   * Catches: the screen showing its own "today" (a repeat would then show the wrong date),
   * or the button sending no request.
   */
  it('CompleteTask_ShowsCompletedDateFromResponse', async () => {
    const user = userEvent.setup()
    render(<GrantTasksScreen />)
    const row = await screen.findByRole('row', { name: /Verify bank details/ })

    await user.click(within(row).getByRole('button', { name: 'Complete' }))

    const updatedRow = await screen.findByRole('row', { name: /Verify bank details/ })
    expect(await within(updatedRow).findByText('Completed on 2026-11-01')).toBeInTheDocument()
    expect(within(updatedRow).queryByRole('button', { name: 'Complete' })).not.toBeInTheDocument()
    expect(fetch).toHaveBeenCalledWith(
      '/api/tasks/1/complete',
      expect.objectContaining({ method: 'PUT' }),
    )
  })

  /**
   * FE-13: Complete fails (404 task-not-found)
   * → the API's message in the box at the top of the screen.
   * Catches: a failed Complete swallowed, so the officer thinks the task was completed.
   */
  it('CompleteTaskFails_ShowsApiMessageAtTop', async () => {
    const user = userEvent.setup()
    render(<GrantTasksScreen />)
    const row = await screen.findByRole('row', { name: /Request quote/ })

    await user.click(within(row).getByRole('button', { name: 'Complete' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Task 2 was not found.')
    expect(within(row).getByRole('button', { name: 'Complete' })).toBeInTheDocument()
  })

  /**
   * FE-11: add a valid task → it shows in the table, and the form is empty again.
   * Catches: the table not refreshed after adding, or the form keeping its values
   * (the officer would add the same task twice).
   */
  it('AddTask_NewTaskShowsInTable_FormIsCleared', async () => {
    const user = userEvent.setup()
    render(<GrantTasksScreen />)
    await screen.findByText('Verify bank details')

    await user.type(screen.getByLabelText('Title'), 'Review budget')
    await user.type(screen.getByLabelText('Due date'), '2026-11-25')
    await user.click(screen.getByRole('button', { name: 'Add task' }))

    expect(await screen.findByRole('row', { name: /Review budget/ })).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveValue('')
    expect(screen.getByLabelText('Due date')).toHaveValue('')
  })

  /**
   * FE-14: loading GR-100003 fails (server down) → error at the top;
   * then GR-100001 loads → error gone.
   * Catches: an old error left on screen after the server is back, so the officer
   * thinks the new grant failed too.
   */
  it('LoadFailsThenAnotherGrantLoads_ErrorIsCleared', async () => {
    const failOnce = new Set(['/api/grants/3'])
    vi.stubGlobal(
      'fetch',
      vi.fn((url: string, init?: RequestInit) =>
        failOnce.delete(url)
          ? Promise.resolve(new Response(null, { status: 503 }))
          : fakeApi(url, init),
      ),
    )
    const user = userEvent.setup()
    render(<GrantTasksScreen />)
    await screen.findByText('Verify bank details')

    await user.selectOptions(screen.getByLabelText('Grant'), 'GR-100003 · Harbour Arts Collective')
    expect(await screen.findByRole('alert')).toBeInTheDocument()

    await user.selectOptions(
      screen.getByLabelText('Grant'),
      'GR-100001 · Riverside Community Garden Inc.',
    )

    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  /**
   * FE-15: an error is shown (Complete failed), then a task is added
   * and the grant reloads → error gone.
   * Catches: the reload after adding a task leaving an old error on screen.
   */
  it('ErrorShownThenGrantReloads_ErrorIsCleared', async () => {
    const user = userEvent.setup()
    render(<GrantTasksScreen />)
    const row = await screen.findByRole('row', { name: /Request quote/ })
    await user.click(within(row).getByRole('button', { name: 'Complete' }))
    expect(await screen.findByRole('alert')).toBeInTheDocument()

    await user.type(screen.getByLabelText('Title'), 'Review budget')
    await user.type(screen.getByLabelText('Due date'), '2026-11-25')
    await user.click(screen.getByRole('button', { name: 'Add task' }))

    await screen.findByRole('row', { name: /Review budget/ })
    await waitFor(() => expect(screen.queryByRole('alert')).not.toBeInTheDocument())
  })

  /**
   * FE-16: GR-100001 is shown, then GR-100003 fails to load → GR-100001's tasks and form are gone.
   * Catches: the previous grant left on screen under the new grant's name, so Add task
   * would add to the wrong grant.
   */
  it('NewGrantFailsToLoad_PreviousGrantIsNoLongerShown', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((url: string, init?: RequestInit) =>
        url === '/api/grants/3'
          ? Promise.resolve(new Response(null, { status: 503 }))
          : fakeApi(url, init),
      ),
    )
    const user = userEvent.setup()
    render(<GrantTasksScreen />)
    await screen.findByText('Verify bank details')

    await user.selectOptions(screen.getByLabelText('Grant'), 'GR-100003 · Harbour Arts Collective')

    expect(await screen.findByRole('alert')).toBeInTheDocument()
    expect(screen.queryByText('Verify bank details')).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Add task' })).not.toBeInTheDocument()
  })

  /**
   * NET-5: the grant list cannot be loaded (no connection)
   * → "Cannot reach the server…" at the top of the screen.
   * Catches: a general "Could not load" message, so the officer cannot tell the server is down.
   */
  it('GrantListCannotReachServer_SaysSoAtTop', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    render(<GrantTasksScreen />)

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Cannot reach the server. Check your connection and try again.',
    )
  })

  /**
   * NET-6: GR-100003 cannot be loaded (the proxy answers 503)
   * → "Cannot reach the server…" at the top of the screen.
   * Catches: the same general message when one grant fails to load.
   */
  it('GrantCannotReachServer_SaysSoAtTop', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((url: string, init?: RequestInit) =>
        url === '/api/grants/3'
          ? Promise.resolve(new Response(null, { status: 503 }))
          : fakeApi(url, init),
      ),
    )
    const user = userEvent.setup()
    render(<GrantTasksScreen />)
    await screen.findByText('Verify bank details')

    await user.selectOptions(screen.getByLabelText('Grant'), 'GR-100003 · Harbour Arts Collective')

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Cannot reach the server. Check your connection and try again.',
    )
  })
})
