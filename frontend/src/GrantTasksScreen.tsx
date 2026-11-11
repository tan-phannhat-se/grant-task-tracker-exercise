import { useEffect, useState } from 'react'
import { getGrant, getGrants, type GrantDetail, type GrantListItem } from './api/grantsApi'
import TaskList from './TaskList'

const loadErrorMessage = 'Could not load the grants. Please try again.'

/** The one screen: choose a grant, see and work on its tasks. */
export default function GrantTasksScreen() {
  const [grants, setGrants] = useState<GrantListItem[]>([])
  const [selectedGrantId, setSelectedGrantId] = useState<number | null>(null)
  const [grant, setGrant] = useState<GrantDetail | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)

  useEffect(() => {
    getGrants()
      .then((list) => {
        setGrants(list)
        setSelectedGrantId(list[0]?.id ?? null) // opens on the first grant
      })
      .catch(() => setLoadError(loadErrorMessage))
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
          setLoadError(loadErrorMessage)
        }
      })
    return () => {
      isLatest = false
    }
  }, [selectedGrantId])

  return (
    <main>
      <h1>Grant tasks</h1>
      {loadError && (
        <p role="alert" className="error">
          {loadError}
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
          <TaskList tasks={grant.tasks} onComplete={() => {}} />
        </section>
      )}
    </main>
  )
}
