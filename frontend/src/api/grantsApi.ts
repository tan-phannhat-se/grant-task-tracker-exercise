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

/** The error body the API sends with a 400, 404 or 409. */
interface ProblemDetails {
  detail?: string
  code?: string
  field?: string
}

const checkTheFormMessage = 'Check the form and try again.'
const somethingWentWrongMessage = 'Something went wrong. Please try again.'

/** Lists every grant, open and closed. */
export function getGrants(): Promise<GrantListItem[]> {
  return send('/grants')
}

/** Loads one grant with its tasks. */
export function getGrant(grantId: number): Promise<GrantDetail> {
  return send(`/grants/${grantId}`)
}

/** Adds a task to a grant and returns the saved task. */
export function addTask(grantId: number, task: NewTask): Promise<Task> {
  return send(`/grants/${grantId}/tasks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(task),
  })
}

/** Completes a task and returns it with its completed date. */
export function completeTask(taskId: number): Promise<Task> {
  return send(`/tasks/${taskId}/complete`, { method: 'PUT' })
}

/** Sends one request and returns the JSON body, or throws an ApiError. */
async function send<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, init)
  if (!response.ok) {
    throw await toApiError(response)
  }
  return (await response.json()) as T
}

/** Turns a failed response into an ApiError; only a ProblemDetails body has a code and a message. */
async function toApiError(response: Response): Promise<ApiError> {
  const contentType = response.headers.get('Content-Type') ?? ''
  if (!contentType.startsWith('application/problem+json')) {
    // A badly formatted value gets a 400 with an empty body, so the field is unknown.
    const message = response.status === 400 ? checkTheFormMessage : somethingWentWrongMessage
    return new ApiError(response.status, null, null, message)
  }

  const problem = (await response.json()) as ProblemDetails
  return new ApiError(
    response.status,
    problem.code ?? null,
    problem.field ?? null,
    problem.detail ?? somethingWentWrongMessage,
  )
}
