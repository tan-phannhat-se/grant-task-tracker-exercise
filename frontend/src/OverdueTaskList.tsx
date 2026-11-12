import { useEffect, useState } from 'react'
import {
  ApiError,
  getOverdueTasks,
  type GrantStatusFilter,
  type OverdueTask,
} from './api/grantsApi'

const loadErrorMessage = 'Could not load the overdue tasks. Please try again.'

/** What the overdue list needs: the filter held by the parent, and how to open a grant. */
export interface OverdueTaskListProps {
  grantStatus: GrantStatusFilter
  onGrantStatusChange: (grantStatus: GrantStatusFilter) => void
  onOpenGrant: (grantId: number) => void
}

/** The overdue tasks of every grant with the chosen status; a row opens the task's grant. */
export default function OverdueTaskList({
  grantStatus,
  onGrantStatusChange,
  onOpenGrant,
}: OverdueTaskListProps) {
  // null while the tasks for the chosen status are loading, or when they failed to load
  const [tasks, setTasks] = useState<OverdueTask[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    // An older answer must not replace a newer one: the officer may change the filter
    // while the tasks are still loading.
    let isLatest = true
    getOverdueTasks(grantStatus)
      .then((list) => {
        if (isLatest) {
          setTasks(list)
          setError(null) // the tasks loaded, so an older error no longer applies
        }
      })
      .catch((loadError: unknown) => {
        if (isLatest) {
          setError(loadError instanceof ApiError ? loadError.message : loadErrorMessage)
        }
      })
    return () => {
      isLatest = false
    }
  }, [grantStatus])

  /** Hides the rows at once, so tasks of another status never sit under the new filter. */
  function handleGrantStatusChange(newStatus: GrantStatusFilter) {
    setTasks(null)
    onGrantStatusChange(newStatus)
  }

  return (
    <section>
      <h2>Overdue tasks</h2>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}

      <label htmlFor="overdue-grant-status-filter">Grant status</label>
      <select
        id="overdue-grant-status-filter"
        value={grantStatus}
        onChange={(event) => handleGrantStatusChange(event.target.value as GrantStatusFilter)}
      >
        <option>Open</option>
        <option>Closed</option>
        <option>All</option>
      </select>

      {tasks?.length === 0 && <p>No overdue tasks.</p>}
      {tasks && tasks.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Reference</th>
              <th>Organisation</th>
              <th>Task</th>
              <th>Priority</th>
              <th>Days overdue</th>
            </tr>
          </thead>
          <tbody>
            {tasks.map((task) => (
              <tr key={task.id} className="clickable" onClick={() => onOpenGrant(task.grantId)}>
                <td>
                  {/* For keyboard users; its click reaches the row, so the grant opens once. */}
                  <button type="button">{task.referenceNumber}</button>
                </td>
                <td>{task.organisationName}</td>
                <td>{task.title}</td>
                <td>{task.priority}</td>
                <td>{task.daysOverdue}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}
