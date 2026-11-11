import { render, screen, within } from '@testing-library/react'
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
