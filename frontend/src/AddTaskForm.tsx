import { useState, type FormEvent } from 'react'
import { addTask, type Priority } from './api/grantsApi'

const titleMaxLength = 100 // the API's limit (BR-3)

/** What the form needs: the grant to add to, and what to do once a task is added. */
export interface AddTaskFormProps {
  grantId: number
  onAdded: () => void
}

/** The Add task form: checks the input, sends it, shows the API's errors. */
export default function AddTaskForm({ grantId }: AddTaskFormProps) {
  const [title, setTitle] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [priority, setPriority] = useState<Priority>('Medium')
  const [titleError, setTitleError] = useState<string | null>(null)
  const [dueDateError, setDueDateError] = useState<string | null>(null)

  /** Checks the input first; only input that passes is sent. */
  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmedTitle = title.trim()
    const newTitleError = titleErrorFor(trimmedTitle)
    const newDueDateError = dueDate === '' ? 'Due date is required.' : null
    setTitleError(newTitleError)
    setDueDateError(newDueDateError)
    if (newTitleError !== null || newDueDateError !== null) {
      return
    }

    addTask(grantId, { title: trimmedTitle, dueDate, priority })
  }

  return (
    <form onSubmit={handleSubmit}>
      <h2>Add task</h2>

      <label htmlFor="task-title">Title</label>
      <input
        id="task-title"
        value={title}
        onChange={(event) => setTitle(event.target.value)}
        aria-invalid={titleError !== null}
        aria-describedby={titleError ? 'task-title-error' : undefined}
      />
      {titleError && (
        <p id="task-title-error" className="error">
          {titleError}
        </p>
      )}

      <label htmlFor="task-due-date">Due date</label>
      <input
        id="task-due-date"
        type="date"
        value={dueDate}
        onChange={(event) => setDueDate(event.target.value)}
        aria-invalid={dueDateError !== null}
        aria-describedby={dueDateError ? 'task-due-date-error' : undefined}
      />
      {dueDateError && (
        <p id="task-due-date-error" className="error">
          {dueDateError}
        </p>
      )}

      <label htmlFor="task-priority">Priority</label>
      <select
        id="task-priority"
        value={priority}
        onChange={(event) => setPriority(event.target.value as Priority)}
      >
        <option>Low</option>
        <option>Medium</option>
        <option>High</option>
      </select>

      <button type="submit">Add task</button>
    </form>
  )
}

/** The client's copy of BR-3 on the trimmed title, worded like the API. */
function titleErrorFor(trimmedTitle: string): string | null {
  if (trimmedTitle.length === 0) {
    return 'Title is required.'
  }
  if (trimmedTitle.length > titleMaxLength) {
    return `Title must be ${titleMaxLength} characters or fewer.`
  }
  return null
}
