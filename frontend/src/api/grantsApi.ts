/** Typed calls to the Grant Task Tracker API; the dev server forwards /api to it. */

export type Priority = 'Low' | 'Medium' | 'High'

export type GrantStatus = 'Open' | 'Closed'

/** A task as the API returns it; the server works out overdue and completed late. */
export interface Task {
  id: number
  grantId: number
  title: string
  dueDate: string // yyyy-MM-dd
  priority: Priority
  completedDate: string | null
  isOverdue: boolean
  completedLate: boolean
}

/** One row of the grant list. */
export interface GrantListItem {
  id: number
  referenceNumber: string
  organisationName: string
  applicationDate: string
  status: GrantStatus
  pendingTaskCount: number
}

/** One grant with its tasks, most urgent first. */
export interface GrantDetail {
  id: number
  referenceNumber: string
  organisationName: string
  applicationDate: string
  status: GrantStatus
  tasks: Task[]
}

/** The body of "add a task". */
export interface NewTask {
  title: string
  dueDate: string
  priority: Priority
}

/** Any failed call, in one shape the screen can show. */
export class ApiError extends Error {
  status: number
  code: string | null
  field: string | null

  constructor(status: number, code: string | null, field: string | null, message: string) {
    super(message)
    this.status = status
    this.code = code
    this.field = field
  }
}

/** Lists every grant, open and closed. */
export async function getGrants(): Promise<GrantListItem[]> {
  throw new Error('Not implemented')
}

/** Loads one grant with its tasks. */
export async function getGrant(grantId: number): Promise<GrantDetail> {
  throw new Error(`Not implemented: ${grantId}`)
}

/** Adds a task to a grant and returns the saved task. */
export async function addTask(grantId: number, task: NewTask): Promise<Task> {
  throw new Error(`Not implemented: ${grantId} ${task.title}`)
}

/** Completes a task and returns it with its completed date. */
export async function completeTask(taskId: number): Promise<Task> {
  throw new Error(`Not implemented: ${taskId}`)
}
