import { useEffect, useState } from 'react'
import {
  ApiError,
  closeGrant,
  completeTask,
  getGrant,
  type GrantDetail as GrantDetailData,
} from './api/grantsApi'
import AddTaskForm from './AddTaskForm'
import TaskList from './TaskList'

const loadErrorMessage = 'Could not load the grant. Please try again.'
const completeErrorMessage = 'Could not complete the task. Please try again.'
const closeErrorMessage = 'Could not close the grant. Please try again.'

/** What the grant detail screen needs: which grant to show and how to go back to the list. */
export interface GrantDetailProps {
  grantId: number
  onBack: () => void
}

/** One grant with its tasks: complete a task, add a task, close the grant. */
export default function GrantDetail({ grantId, onBack }: GrantDetailProps) {
  const [grant, setGrant] = useState<GrantDetailData | null>(null)
  // Goes up by one after a task is added, so the grant loads again
  const [reloadCount, setReloadCount] = useState(0)
  // An error that does not belong to the Add task form, shown at the top of the screen
  const [screenError, setScreenError] = useState<string | null>(null)

  useEffect(() => {
    // An older answer must not replace a newer one: the grant may reload
    // after a task was added while an earlier load is still on its way.
    let isLatest = true
    getGrant(grantId)
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
  }, [grantId, reloadCount])

  /** Loads the grant again, so a new task shows in the server's order with its flags. */
  function reloadGrant() {
    setReloadCount((count) => count + 1)
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

  /** Closes the grant and shows it as the API returns it; a refusal shows the API's reason. */
  function handleClose() {
    setScreenError(null)
    closeGrant(grantId)
      .then((closed) => setGrant(closed))
      .catch((error: unknown) => {
        setScreenError(messageFor(error, closeErrorMessage))
      })
  }

  return (
    <section>
      <button type="button" onClick={onBack}>
        ← Back
      </button>
      {screenError && (
        <p role="alert" className="error">
          {screenError}
        </p>
      )}

      {grant && (
        <>
          <h2>{`${grant.referenceNumber} · ${grant.organisationName}`}</h2>
          <p>
            Status: <span className="label">{grant.status}</span> · Applied {grant.applicationDate}
          </p>
          {grant.status === 'Open' && (
            <button type="button" onClick={handleClose}>
              Close grant
            </button>
          )}
          <TaskList tasks={grant.tasks} onComplete={handleComplete} />
          {/* A new key per grant empties the form, so input and errors never carry over */}
          <AddTaskForm key={grant.id} grantId={grant.id} onAdded={reloadGrant} />
        </>
      )}
    </section>
  )
}

/**
 * The API client's message (e.g. "Cannot reach the server…") when there is one,
 * otherwise a general one.
 */
function messageFor(error: unknown, generalMessage: string): string {
  return error instanceof ApiError ? error.message : generalMessage
}
