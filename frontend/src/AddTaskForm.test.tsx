import { render, screen } from '@testing-library/react'
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
   * FE-7a: a 101-character title → "Title must be 100 characters or fewer." under Title, nothing sent.
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
