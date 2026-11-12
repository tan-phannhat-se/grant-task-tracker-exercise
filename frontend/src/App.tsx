import { useState } from 'react'
import type { GrantStatusFilter } from './api/grantsApi'
import GrantDetail from './GrantDetail'
import GrantList from './GrantList'
import OverdueTaskList from './OverdueTaskList'

/** The two lists the officer can start from; a grant opened from one goes back to it. */
type ListView = 'grants' | 'overdue'

/** What the app shows: one of the lists, or one grant opened from a list. */
type View = { name: ListView } | { name: 'grant'; grantId: number; backTo: ListView }

/** The app: the grant list, the overdue list, and a grant's detail opened from either. */
export default function App() {
  const [view, setView] = useState<View>({ name: 'grants' })
  // Held here, not in the lists, so each filter stays after a trip to a grant and back
  const [grantStatus, setGrantStatus] = useState<GrantStatusFilter>('All')
  const [overdueGrantStatus, setOverdueGrantStatus] = useState<GrantStatusFilter>('Open')

  /** Shows the view the officer is on. */
  function renderView() {
    switch (view.name) {
      case 'grants':
        return (
          <GrantList
            status={grantStatus}
            onStatusChange={setGrantStatus}
            onOpen={(grantId) => setView({ name: 'grant', grantId, backTo: 'grants' })}
          />
        )
      case 'overdue':
        return (
          <OverdueTaskList
            grantStatus={overdueGrantStatus}
            onGrantStatusChange={setOverdueGrantStatus}
            onOpenGrant={(grantId) => setView({ name: 'grant', grantId, backTo: 'overdue' })}
          />
        )
      case 'grant':
        return (
          <GrantDetail
            // A new screen for each grant, so nothing of the previous grant carries over
            key={view.grantId}
            grantId={view.grantId}
            onBack={() => setView({ name: view.backTo })}
          />
        )
    }
  }

  return (
    <main>
      <h1>Grant tasks</h1>
      <nav>
        <button type="button" onClick={() => setView({ name: 'grants' })}>
          Grants
        </button>{' '}
        <button type="button" onClick={() => setView({ name: 'overdue' })}>
          Overdue tasks
        </button>
      </nav>
      {renderView()}
    </main>
  )
}
