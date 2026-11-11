import { useEffect, useState } from 'react'
import {
  ApiError,
  completeTask,
  getGrant,
  getGrants,
  type GrantDetail,
  type GrantListItem,
} from './api/grantsApi'
import TaskList from './TaskList'

const loadErrorMessage = 'Could not load the grants. Please try again.'
const completeErrorMessage = 'Could not complete the task. Please try again.'

/** The one screen: choose a grant, see and work on its tasks. */
export default function GrantTasksScreen() {
  const [grants, setGrants] = useState<GrantListItem[]>([])
  const [selectedGrantId, setSelectedGrantId] = useState<number | null>(null)
  const [grant, setGrant] = useState<GrantDetail | null>(null)
  // An error that does not belong to the Add task form, shown at the top of the screen
  const [screenError, setScreenError] = useState<string | null>(null)

  useEffect(() => {
    getGrants()
      .then((list) => {
        setGrants(list)
        setSelectedGrantId(list[0]?.id ?? null) // opens on the first grant
      })
      .catch(() => setScreenError(loadErrorMessage))
  }, [])

  useEffect(() => {
    if (selectedGrantId === null) {
      return
    }
    // When the officer switches grants quickly, an older answer must not replace a newer one.
    let isLatest = true
    getGrant(selectedGrantId)
      .then((detail) => {
        if (isLatest) {
          setGrant(detail)
        }
      })
      .catch(() => {
        if (isLatest) {
          setScreenError(loadErrorMessage)
        }
      })
    return () => {
      isLatest = false
    }
  }, [selectedGrantId])

  /** Completes a task and shows it as the API returns it, so a repeat keeps the first date. */
  function handleComplete(taskId: number) {
    setScreenError(null)
    completeTask(taskId)
      .then((updated) => {
        setGrant((current) => {
          if (current === null) {
            return null
          }
          const tasks = current.tasks.map((task) => (task.id === updated.id ? updated : task))
          return { ...current, tasks }
        })
      })
      .catch((error: unknown) => {
        setScreenError(error instanceof ApiError ? error.message : completeErrorMessage)
      })
  }

  return (
    <main>
      <h1>Grant tasks</h1>
      {screenError && (
        <p role="alert" className="error">
          {screenError}
        </p>
      )}

      <label htmlFor="grant">Grant</label>
      <select
        id="grant"
        value={selectedGrantId ?? ''}
        onChange={(event) => setSelectedGrantId(Number(event.target.value))}
      >
        {grants.map((item) => (
          <option key={item.id} value={item.id}>
            {`${item.referenceNumber} · ${item.organisationName}`}
          </option>
        ))}
      </select>

      {grant && (
        <section>
          <p>
            Status: <span className="label">{grant.status}</span> · Applied {grant.applicationDate}
          </p>
          <TaskList tasks={grant.tasks} onComplete={handleComplete} />
        </section>
      )}
    </main>
  )
}
