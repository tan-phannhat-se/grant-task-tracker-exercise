import { useEffect, useState } from 'react'
import { ApiError, getGrants, type GrantListItem, type GrantStatusFilter } from './api/grantsApi'

const loadErrorMessage = 'Could not load the grants. Please try again.'

/**
 * What the grant list needs: the chosen status, held by the app so it stays when the officer
 * comes back from a grant, and what to do when a grant is opened.
 */
export interface GrantListProps {
  status: GrantStatusFilter
  onStatusChange: (status: GrantStatusFilter) => void
  onOpen: (grantId: number) => void
}

/** Every grant, filtered by status, with its number of pending tasks; a row opens the grant. */
export default function GrantList({ status, onStatusChange, onOpen }: GrantListProps) {
  // null while the grants for the chosen status are loading, or when they failed to load
  const [grants, setGrants] = useState<GrantListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    // An older answer must not replace a newer one: the officer may change the filter
    // while the grants are still loading.
    let isLatest = true
    getGrants(status)
      .then((list) => {
        if (isLatest) {
          setGrants(list)
          setError(null) // the grants loaded, so an older error no longer applies
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
  }, [status])

  /** Hides the rows at once, so grants of another status never sit under the new filter. */
  function handleStatusChange(newStatus: GrantStatusFilter) {
    setGrants(null)
    onStatusChange(newStatus)
  }

  return (
    <section>
      <h2>Grants</h2>
      {error && (
        <p role="alert" className="error">
          {error}
        </p>
      )}

      <label htmlFor="grant-status-filter">Status</label>
      <select
        id="grant-status-filter"
        value={status}
        onChange={(event) => handleStatusChange(event.target.value as GrantStatusFilter)}
      >
        <option>All</option>
        <option>Open</option>
        <option>Closed</option>
      </select>

      {grants?.length === 0 && <p>No grants match this filter.</p>}
      {grants && grants.length > 0 && (
        <table>
          <thead>
            <tr>
              <th>Reference</th>
              <th>Organisation</th>
              <th>Applied</th>
              <th>Status</th>
              <th>Pending tasks</th>
            </tr>
          </thead>
          <tbody>
            {grants.map((grant) => (
              <tr key={grant.id} className="clickable" onClick={() => onOpen(grant.id)}>
                <td>
                  {/* For keyboard users; its click reaches the row, so the grant opens once. */}
                  <button type="button">{grant.referenceNumber}</button>
                </td>
                <td>{grant.organisationName}</td>
                <td>{grant.applicationDate}</td>
                <td>{grant.status}</td>
                <td>{grant.pendingTaskCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}
