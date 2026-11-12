import { act, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Task } from './api/grantsApi'
import AddTaskForm from './AddTaskForm'

const savedTask: Task = {
  id: 9,
  grantId: 1,
  title: 'Saved',
  dueDate: '2026-11-20',
  priority: 'High',
  completedDate: null,
  isOverdue: false,
  completedLate: false,
}

const fetchMock = vi.fn()

beforeEach(() => {
  vi.stubGlobal('fetch', fetchMock)
  fetchMock.mockResolvedValue(Response.json(savedTask, { status: 201 }))
})

afterEach(() => {
  vi.unstubAllGlobals()
  fetchMock.mockReset()
})

type User = ReturnType<typeof userEvent.setup>

/** Fills the form like an officer (empty values are left untouched) and clicks Add task. */
async function fillAndSubmit(user: User, title: string, dueDate: string) {
  if (title !== '') {
    await user.click(screen.getByLabelText('Title'))
    await user.paste(title)
  }
  if (dueDate !== '') {
    await user.type(screen.getByLabelText('Due date'), dueDate)
  }
  await user.selectOptions(screen.getByLabelText('Priority'), 'High')
  await user.click(screen.getByRole('button', { name: 'Add task' }))
}

describe('AddTaskForm client checks', () => {
  /**
   * FE-6: title empty or only spaces → "Title is required." under Title, nothing sent.
   * Catches: the check missing, or the title not trimmed before it is checked.
   */
  it.each(['', '   '])('EmptyTitle_ShowsErrorUnderTitle_SendsNothing (%j)', async (title) => {
    const user = userEvent.setup()
    render(<AddTaskForm grantId={1} onAdded={() => {}} />)

    await fillAndSubmit(user, title, '2026-11-20')

    expect(screen.getByLabelText('Title')).toHaveAccessibleDescription('Title is required.')
    expect(fetchMock).not.toHaveBeenCalled()
  })

  /**
   * FE-7a: a 101-character title → "Title must be 100 characters or fewer." under Title,
   * nothing sent.
   * Catches: the length check missing.
   */
  it('TitleOf101Characters_ShowsErrorUnderTitle_SendsNothing', async () => {
    const user = userEvent.setup()
    render(<AddTaskForm grantId={1} onAdded={() => {}} />)

    await fillAndSubmit(user, 'a'.repeat(101), '2026-11-20')

    expect(screen.getByLabelText('Title')).toHaveAccessibleDescription(
      'Title must be 100 characters or fewer.',
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })

  /**
   * FE-7b: 100 characters with 2 spaces on each side → sent, trimmed, to POST /api/grants/1/tasks.
   * Catches: `>=` instead of `>` (100 refused), or the length counted before trimming.
   */
  it('TitleOf100CharactersAfterTrimming_IsSent', async () => {
    const user = userEvent.setup()
    const title = 'a'.repeat(100)
    render(<AddTaskForm grantId={1} onAdded={() => {}} />)

    await fillAndSubmit(user, `  ${title}  `, '2026-11-20')

    expect(fetchMock).toHaveBeenCalledTimes(1)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/grants/1/tasks')
    expect(JSON.parse(init.body)).toEqual({ title, dueDate: '2026-11-20', priority: 'High' })
  })

  /**
   * FE-8: no due date → "Due date is required." under Due date, nothing sent.
   * Catches: the date check missing (the API would answer with a less helpful error).
   */
  it('NoDueDate_ShowsErrorUnderDueDate_SendsNothing', async () => {
    const user = userEvent.setup()
    render(<AddTaskForm grantId={1} onAdded={() => {}} />)

    await fillAndSubmit(user, 'Check documents', '')

    expect(screen.getByLabelText('Due date')).toHaveAccessibleDescription('Due date is required.')
    expect(fetchMock).not.toHaveBeenCalled()
  })
})

/** A ProblemDetails answer, as the API sends for a broken rule. */
function problem(status: number, code: string, detail: string, field?: string): Response {
  return new Response(JSON.stringify({ status, code, detail, field }), {
    status,
    headers: { 'Content-Type': 'application/problem+json' },
  })
}

describe('AddTaskForm API errors', () => {
  /**
   * FE-9: the API refuses the due date (400 due-date-in-past on dueDate, BR-2)
   * → its message under Due date.
   * Catches: an API error that names a field lost, or shown under the wrong input.
   */
  it('ApiRefusesDueDate_ShowsMessageUnderDueDate', async () => {
    fetchMock.mockResolvedValue(
      problem(400, 'due-date-in-past', 'Due date must be on or after 2026-11-11.', 'dueDate'),
    )
    const user = userEvent.setup()
    render(<AddTaskForm grantId={1} onAdded={() => {}} />)

    await fillAndSubmit(user, 'Check documents', '2026-11-01')

    expect(await screen.findByText('Due date must be on or after 2026-11-11.')).toBeInTheDocument()
    expect(screen.getByLabelText('Due date')).toHaveAccessibleDescription(
      'Due date must be on or after 2026-11-11.',
    )
    expect(screen.getByLabelText('Title')).not.toHaveAccessibleDescription()
  })

  /**
   * FE-10: the API refuses because the grant is closed (409 grant-closed, no field, BR-1)
   * → its message in the box at the top of the form.
   * Catches: an API error without a field swallowed, so nothing tells the officer why.
   */
  it('ApiRefusesClosedGrant_ShowsMessageAtTopOfForm', async () => {
    fetchMock.mockResolvedValue(
      problem(409, 'grant-closed', 'Grant GR-100003 is closed, so no task can be added.'),
    )
    const user = userEvent.setup()
    render(<AddTaskForm grantId={3} onAdded={() => {}} />)

    await fillAndSubmit(user, 'Check documents', '2026-11-20')

    const form = screen.getByRole('form', { name: 'Add task' })
    expect(await within(form).findByRole('alert')).toHaveTextContent(
      'Grant GR-100003 is closed, so no task can be added.',
    )
  })
})

const unreachableMessage = 'Cannot reach the server. Check your connection and try again.'
const uuidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

/** The Idempotency-Key header of the n-th request the form sent. */
function keyOfCall(n: number): string {
  const init = fetchMock.mock.calls[n][1] as RequestInit
  return (init.headers as Record<string, string>)['Idempotency-Key']
}

describe('AddTaskForm safe retries', () => {
  beforeEach(() => {
    // A new answer for every request: a Response body can be read only once.
    fetchMock.mockImplementation(() => Promise.resolve(Response.json(savedTask, { status: 201 })))
  })

  /**
   * NET-4: the server cannot be reached → "Cannot reach the server…" at the top of the form,
   * and the form keeps what the officer typed.
   * Catches: the failure swallowed, or the form cleared so the officer must type it all again.
   */
  it('ServerUnreachable_ShowsMessageAtTopOfForm_KeepsTheValues', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))
    const user = userEvent.setup()
    render(<AddTaskForm grantId={1} onAdded={() => {}} />)

    await fillAndSubmit(user, 'Check documents', '2026-11-20')

    const form = screen.getByRole('form', { name: 'Add task' })
    expect(await within(form).findByRole('alert')).toHaveTextContent(unreachableMessage)
    expect(screen.getByLabelText('Title')).toHaveValue('Check documents')
  })

  /**
   * KEY-2: a failed add clicked again → the same key (a UUID); after a success,
   * the next task gets a new key.
   * Catches: a new key per click (a retry of a request that did reach the server adds the task
   * twice), or one key forever (the next task is refused as a reused key).
   */
  it('RetryAfterFailure_SendsTheSameKey_NextTaskGetsANewKey', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'))
    const onAdded = vi.fn()
    const user = userEvent.setup()
    render(<AddTaskForm grantId={1} onAdded={onAdded} />)

    await fillAndSubmit(user, 'Check documents', '2026-11-20')
    await screen.findByRole('alert')
    await user.click(screen.getByRole('button', { name: 'Add task' }))
    await waitFor(() => expect(onAdded).toHaveBeenCalledTimes(1))
    await fillAndSubmit(user, 'Next task', '2026-11-21')
    await waitFor(() => expect(onAdded).toHaveBeenCalledTimes(2))

    expect(keyOfCall(0)).toMatch(uuidPattern)
    expect(keyOfCall(1)).toBe(keyOfCall(0))
    expect(keyOfCall(2)).not.toBe(keyOfCall(0))
  })

  /**
   * FE-18: a double-click on Add task while the first request is on its way → one request;
   * the button is disabled until the answer comes, then usable again.
   * Catches: a second request with the same key; when both reach the server at once, the second
   * gets a 500 and the officer sees an error for a task that was added.
   */
  it('DoubleClickWhileSending_SendsOneRequest', async () => {
    let answer: (() => void) | null = null
    fetchMock.mockImplementation(
      () =>
        new Promise<Response>((resolve) => {
          answer = () => resolve(Response.json(savedTask, { status: 201 }))
        }),
    )
    const user = userEvent.setup()
    render(<AddTaskForm grantId={1} onAdded={() => {}} />)
    await user.click(screen.getByLabelText('Title'))
    await user.paste('Check documents')
    await user.type(screen.getByLabelText('Due date'), '2026-11-20')
    const addButton = screen.getByRole('button', { name: 'Add task' })

    await user.dblClick(addButton)

    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(addButton).toBeDisabled()
    await act(async () => answer?.())
    expect(addButton).toBeEnabled()
  })

  /**
   * KEY-3: a failed add, then the officer edits the title and clicks again → a new key.
   * Catches: the key kept after an edit, so the API refuses the corrected task as a reused key.
   */
  it('EditAfterFailure_SendsANewKey', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))
    const user = userEvent.setup()
    render(<AddTaskForm grantId={1} onAdded={() => {}} />)

    await fillAndSubmit(user, 'Check documents', '2026-11-20')
    await screen.findByRole('alert')
    await user.type(screen.getByLabelText('Title'), ' again')
    await user.click(screen.getByRole('button', { name: 'Add task' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2))

    expect(keyOfCall(1)).not.toBe(keyOfCall(0))
  })
})
