import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { GrantDetail as GrantDetailData, Task } from './api/grantsApi'
import GrantDetail from './GrantDetail'

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

/** The grants by URL, new for each test, so a change made in one test is never seen by another. */
function freshGrantDetails(): Record<string, GrantDetailData> {
  return {
    '/api/grants/1': {
      id: 1,
      referenceNumber: 'GR-100001',
      organisationName: 'Riverside Community Garden Inc.',
      applicationDate: '2026-09-12',
      status: 'Open',
      tasks: [pendingTask(1, 1, 'Verify bank details'), pendingTask(2, 1, 'Request quote')],
    },
    '/api/grants/3': {
      id: 3,
      referenceNumber: 'GR-100003',
      organisationName: 'Harbour Arts Collective',
      applicationDate: '2026-04-25',
      status: 'Closed',
      tasks: [pendingTask(4, 3, 'Final acquittal review')],
    },
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

// What the API sends when grant 1 cannot be closed because a High-priority task is pending.
const closeRefused = new Response(
  JSON.stringify({
    status: 409,
    code: 'pending-high-priority-tasks',
    detail: 'Cannot close a grant with a High-priority task pending: Verify bank details',
  }),
  { status: 409, headers: { 'Content-Type': 'application/problem+json' } },
)

/**
 * Answers like the API: one grant, adding a task to grant 1, closing grant 1,
 * completing task 1 (completing task 2 fails).
 */
function fakeApi(url: string, init?: RequestInit): Promise<Response> {
  if (init?.method === 'POST' && url === '/api/grants/1/tasks') {
    const added: Task = { ...pendingTask(10, 1, ''), ...JSON.parse(String(init.body)) }
    grantDetails['/api/grants/1'].tasks.push(added)
    return Promise.resolve(Response.json(added, { status: 201 }))
  }
  if (init?.method === 'PUT' && url === '/api/grants/1/close') {
    grantDetails['/api/grants/1'].status = 'Closed'
    return Promise.resolve(Response.json(grantDetails['/api/grants/1']))
  }
  if (init?.method === 'PUT') {
    const isTaskOne = url === '/api/tasks/1/complete'
    return Promise.resolve(isTaskOne ? Response.json(completedTask) : taskNotFound.clone())
  }
  const body = grantDetails[url]
  return Promise.resolve(body ? Response.json(body) : new Response(null, { status: 404 }))
}

beforeEach(() => {
  grantDetails = freshGrantDetails()
  vi.stubGlobal('fetch', vi.fn(fakeApi))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('GrantDetail', () => {
  /**
   * FE-1: opens grant 1 → its reference heading, status and tasks.
   * Catches: the wrong grant loaded, or the screen opening empty.
   */
  it('OpenGrant_ShowsItsHeadingStatusAndTasks', async () => {
    render(<GrantDetail grantId={1} onBack={() => {}} />)

    expect(
      await screen.findByRole('heading', { name: 'GR-100001 · Riverside Community Garden Inc.' }),
    ).toBeInTheDocument()
    expect(screen.getByText('Open')).toBeInTheDocument()
    expect(screen.getByText('Verify bank details')).toBeInTheDocument()
    expect(screen.getByText('Request quote')).toBeInTheDocument()
  })

  /**
   * FE-5: click Complete → PUT /api/tasks/1/complete;
   * the row shows the completed date from the response.
   * Catches: the screen showing its own "today" (a repeat would then show the wrong date),
   * or the button sending no request.
   */
  it('CompleteTask_ShowsCompletedDateFromResponse', async () => {
    const user = userEvent.setup()
    render(<GrantDetail grantId={1} onBack={() => {}} />)
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
    render(<GrantDetail grantId={1} onBack={() => {}} />)
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
    render(<GrantDetail grantId={1} onBack={() => {}} />)
    await screen.findByText('Verify bank details')

    await user.type(screen.getByLabelText('Title'), 'Review budget')
    await user.type(screen.getByLabelText('Due date'), '2026-11-25')
    await user.click(screen.getByRole('button', { name: 'Add task' }))

    expect(await screen.findByRole('row', { name: /Review budget/ })).toBeInTheDocument()
    expect(screen.getByLabelText('Title')).toHaveValue('')
    expect(screen.getByLabelText('Due date')).toHaveValue('')
  })

  /**
   * FE-15: an error is shown (Complete failed), then a task is added
   * and the grant reloads → error gone.
   * Catches: the reload after adding a task leaving an old error on screen.
   */
  it('ErrorShownThenGrantReloads_ErrorIsCleared', async () => {
    const user = userEvent.setup()
    render(<GrantDetail grantId={1} onBack={() => {}} />)
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
   * NET-6: GR-100003 cannot be loaded (the proxy answers 503)
   * → "Cannot reach the server…" at the top of the screen.
   * Catches: a general message when the grant fails to load,
   * so the officer cannot tell the server is down.
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

    render(<GrantDetail grantId={3} onBack={() => {}} />)

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Cannot reach the server. Check your connection and try again.',
    )
  })

  /**
   * FE-25: an Open grant, click Close grant → PUT /api/grants/1/close;
   * the status shows Closed and the Close grant button is gone.
   * Catches: the wrong URL or method, or the screen not showing the closed grant.
   */
  it('CloseGrant_ShowsClosedGrant_NoCloseButton', async () => {
    const user = userEvent.setup()
    render(<GrantDetail grantId={1} onBack={() => {}} />)

    await user.click(await screen.findByRole('button', { name: 'Close grant' }))

    expect(await screen.findByText('Closed')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Close grant' })).not.toBeInTheDocument()
    expect(fetch).toHaveBeenCalledWith(
      '/api/grants/1/close',
      expect.objectContaining({ method: 'PUT' }),
    )
  })

  /**
   * FE-26: Close refused (409, a High-priority task pending) → its detail in the box at the top,
   * status still Open; the next Close succeeds → the box is gone.
   * Catches: a refusal swallowed, or the old error left after a successful close.
   */
  it('CloseGrant_Refused_ShowsMessageAtTop_ClearedByNextClose', async () => {
    let isFirstClose = true
    vi.stubGlobal(
      'fetch',
      vi.fn((url: string, init?: RequestInit) => {
        if (isFirstClose && url === '/api/grants/1/close') {
          isFirstClose = false
          return Promise.resolve(closeRefused.clone())
        }
        return fakeApi(url, init)
      }),
    )
    const user = userEvent.setup()
    render(<GrantDetail grantId={1} onBack={() => {}} />)

    await user.click(await screen.findByRole('button', { name: 'Close grant' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Cannot close a grant with a High-priority task pending: Verify bank details',
    )
    expect(screen.getByText('Open')).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Close grant' }))

    expect(await screen.findByText('Closed')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  /**
   * FE-27: a Closed grant (GR-100003) → no Close grant button.
   * Catches: closing offered on a grant that is already closed.
   */
  it('ClosedGrant_HasNoCloseButton', async () => {
    render(<GrantDetail grantId={3} onBack={() => {}} />)

    expect(await screen.findByText('Final acquittal review')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Close grant' })).not.toBeInTheDocument()
  })
})
