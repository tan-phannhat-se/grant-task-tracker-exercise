import { render, screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { Task } from './api/grantsApi'
import TaskList from './TaskList'

/** A pending task due in the future; each test changes only what it is about. */
function task(overrides: Partial<Task>): Task {
  return {
    id: 1,
    grantId: 1,
    title: 'Task',
    dueDate: '2026-11-20',
    priority: 'Medium',
    completedDate: null,
    isOverdue: false,
    completedLate: false,
    ...overrides,
  }
}

function rowOf(title: string) {
  return screen.getByRole('row', { name: new RegExp(title) })
}

describe('TaskList', () => {
  /**
   * FE-12: a grant with no tasks → "No tasks yet" instead of an empty table.
   * Catches: an empty grant shows a bare table header, which looks like a loading error.
   */
  it('NoTasks_ShowsNoTasksYet', () => {
    render(<TaskList tasks={[]} />)

    expect(screen.getByText('No tasks yet.')).toBeInTheDocument()
    expect(screen.queryByRole('table')).not.toBeInTheDocument()
  })

  /**
   * FE-2: an overdue task → highlighted row with an "Overdue" label; a task not overdue → neither.
   * Catches: the highlight missing, or put on every task.
   */
  it('OverdueTask_IsHighlightedAndLabelled_OtherTaskIsNot', () => {
    const tasks = [
      task({ id: 1, title: 'Verify bank details', dueDate: '2026-11-08', isOverdue: true }),
      task({ id: 2, title: 'Request quote', dueDate: '2026-11-16' }),
    ]

    render(<TaskList tasks={tasks} />)

    expect(rowOf('Verify bank details')).toHaveClass('overdue')
    expect(within(rowOf('Verify bank details')).getByText('Overdue')).toBeInTheDocument()
    expect(rowOf('Request quote')).not.toHaveClass('overdue')
    expect(within(rowOf('Request quote')).queryByText('Overdue')).not.toBeInTheDocument()
  })

  /**
   * FE-3: a pending task → Complete button; a completed task → no button, "Completed on {date}".
   * Catches: Complete offered on a task that is already completed.
   */
  it('PendingTask_HasCompleteButton_CompletedTaskShowsItsDate', () => {
    const tasks = [
      task({ id: 1, title: 'Request quote' }),
      task({
        id: 2,
        title: 'Check eligibility',
        dueDate: '2026-09-22',
        completedDate: '2026-09-22',
      }),
    ]

    render(<TaskList tasks={tasks} />)

    expect(within(rowOf('Request quote')).getByRole('button', { name: 'Complete' })).toBeEnabled()
    const completedRow = rowOf('Check eligibility')
    expect(within(completedRow).queryByRole('button', { name: 'Complete' })).not.toBeInTheDocument()
    expect(within(completedRow).getByText('Completed on 2026-09-22')).toBeInTheDocument()
  })

  /**
   * FE-4: a task completed after its due date → "Completed late"; one completed on time → no label.
   * Catches: the label missing, or shown on every completed task.
   */
  it('TaskCompletedLate_IsLabelled_TaskCompletedOnTimeIsNot', () => {
    const tasks = [
      task({
        id: 1,
        title: 'Late one',
        dueDate: '2026-11-08',
        completedDate: '2026-11-11',
        completedLate: true,
      }),
      task({ id: 2, title: 'On time', dueDate: '2026-09-22', completedDate: '2026-09-22' }),
    ]

    render(<TaskList tasks={tasks} />)

    expect(within(rowOf('Late one')).getByText('Completed late')).toBeInTheDocument()
    expect(within(rowOf('On time')).queryByText('Completed late')).not.toBeInTheDocument()
  })
})
