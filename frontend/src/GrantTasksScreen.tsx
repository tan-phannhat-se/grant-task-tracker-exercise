import { useEffect, useState } from 'react'
import {
  ApiError,
  completeTask,
  getGrant,
  getGrants,
  type GrantDetail,
  type GrantListItem,
} from './api/grantsApi'
import AddTaskForm from './AddTaskForm'
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
      .catch((error: unknown) => setScreenError(messageFor(error, loadErrorMessage)))
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
          setScreenError(null) // the grant loaded, so an older error no longer applies
        }
      })
      .catch((error: unknown) => {
        if (isLatest) {
          setScreenError(messageFor(error, loadErrorMessage))
        }
      })
    return () => {
      isLatest = false
    }
  }, [selectedGrantId])

  /** Hides the shown grant at once, so its tasks and form never sit under another grant's name. */
  function handleGrantChange(grantId: number) {
    setGrant(null)
    setSelectedGrantId(grantId)
  }

  /** Loads the grant again, so a new task shows in the server's order with its flags. */
  function reloadGrant(grantId: number) {
    getGrant(grantId)
      .then((detail) => {
        setGrant(detail)
        setScreenError(null)
      })
      .catch((error: unknown) => setScreenError(messageFor(error, loadErrorMessage)))
  }

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
        setScreenError(messageFor(error, completeErrorMessage))
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
        onChange={(event) => handleGrantChange(Number(event.target.value))}
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
          {/* A new key per grant empties the form, so input and errors never carry over */}
          <AddTaskForm key={grant.id} grantId={grant.id} onAdded={() => reloadGrant(grant.id)} />
        </section>
      )}
    </main>
  )
}

/**
 * The API client's message (e.g. "Cannot reach the server…") when there is one,
 * otherwise a general one.
 */
function messageFor(error: unknown, generalMessage: string): string {
  return error instanceof ApiError ? error.message : generalMessage
}
