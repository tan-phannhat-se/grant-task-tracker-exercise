import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { GrantDetail, GrantListItem, Task } from './api/grantsApi'
import GrantTasksScreen from './GrantTasksScreen'

const grants: GrantListItem[] = [
  {
    id: 1,
    referenceNumber: 'GR-100001',
    organisationName: 'Riverside Community Garden Inc.',
    applicationDate: '2026-09-12',
    status: 'Open',
    pendingTaskCount: 1,
  },
  {
    id: 3,
    referenceNumber: 'GR-100003',
    organisationName: 'Harbour Arts Collective',
    applicationDate: '2026-04-25',
    status: 'Closed',
    pendingTaskCount: 0,
  },
]

function pendingTask(id: number, grantId: number, title: string): Task {
  return {
    id,
    grantId,
    title,
    dueDate: '2026-11-20',
    priority: 'High',
    completedDate: null,
    isOverdue: false,
    completedLate: false,
  }
}

const grantDetails: Record<string, GrantDetail> = {
  '/api/grants/1': { ...grants[0], tasks: [pendingTask(1, 1, 'Verify bank details')] },
  '/api/grants/3': { ...grants[1], tasks: [pendingTask(4, 3, 'Final acquittal review')] },
}

/** Answers like the API: the grant list, or one grant by its URL. */
function fakeApi(url: string): Promise<Response> {
  const body = url === '/api/grants' ? grants : grantDetails[url]
  return Promise.resolve(body ? Response.json(body) : new Response(null, { status: 404 }))
}

beforeEach(() => {
  vi.stubGlobal('fetch', vi.fn(fakeApi))
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('GrantTasksScreen', () => {
  /**
   * FE-1: opens on the first grant and shows its tasks; choosing another grant → its tasks and status.
   * Catches: tasks not reloaded when the grant changes, or the screen opening empty.
   */
  it('ChooseGrant_ShowsThatGrantsTasksAndStatus', async () => {
    const user = userEvent.setup()
    render(<GrantTasksScreen />)

    expect(await screen.findByText('Verify bank details')).toBeInTheDocument()

    await user.selectOptions(screen.getByLabelText('Grant'), 'GR-100003 · Harbour Arts Collective')

    expect(await screen.findByText('Final acquittal review')).toBeInTheDocument()
    expect(screen.queryByText('Verify bank details')).not.toBeInTheDocument()
    expect(screen.getByText('Closed')).toBeInTheDocument()
  })
})
