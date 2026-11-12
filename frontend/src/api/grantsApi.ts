/** Typed calls to the Grant Task Tracker API; the dev server forwards /api to it. */

export type Priority = 'Low' | 'Medium' | 'High'

export type GrantStatus = 'Open' | 'Closed'

/** The grant statuses a list can be filtered by; All means no filter. */
export type GrantStatusFilter = 'All' | GrantStatus

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

/** One task of the overdue list, with its grant; most overdue first. */
export interface OverdueTask {
  id: number
  grantId: number
  title: string
  priority: Priority
  referenceNumber: string
  organisationName: string
  daysOverdue: number
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
const serverUnreachableCode = 'server-unreachable'
const serverUnreachableMessage = 'Cannot reach the server. Check your connection and try again.'
const requestTimeLimitMs = 15_000
// What a proxy answers when it cannot reach the API behind it
const proxyCannotReachApiStatuses = [502, 503, 504]

/** Lists the grants with the given status; without a status, every grant. */
export function getGrants(status?: GrantStatusFilter): Promise<GrantListItem[]> {
  return send(status === undefined ? '/grants' : `/grants?status=${status}`)
}

/** Loads one grant with its tasks. */
export function getGrant(grantId: number): Promise<GrantDetail> {
  return send(`/grants/${grantId}`)
}

/**
 * Adds a task to a grant and returns the saved task.
 * Sending the same request again with the same key returns the same task, never a second one.
 */
export function addTask(grantId: number, task: NewTask, idempotencyKey: string): Promise<Task> {
  return send(`/grants/${grantId}/tasks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Idempotency-Key': idempotencyKey },
    body: JSON.stringify(task),
  })
}

/** Completes a task and returns it with its completed date. */
export function completeTask(taskId: number): Promise<Task> {
  return send(`/tasks/${taskId}/complete`, { method: 'PUT' })
}

/** Closes a grant and returns it; closing a closed grant changes nothing. */
export function closeGrant(grantId: number): Promise<GrantDetail> {
  return send(`/grants/${grantId}/close`, { method: 'PUT' })
}

/** Lists the overdue tasks of grants with the given status, most overdue first. */
export function getOverdueTasks(grantStatus: GrantStatusFilter): Promise<OverdueTask[]> {
  return send(`/tasks/overdue?grantStatus=${grantStatus}`)
}

/** Sends one request and returns the JSON body, or throws an ApiError. */
async function send<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      ...init,
      signal: AbortSignal.timeout(requestTimeLimitMs),
    })
  } catch {
    // No answer at all: no connection, or the time limit passed.
    throw serverUnreachable(0)
  }
  if (!response.ok) {
    throw await toApiError(response)
  }
  return (await response.json()) as T
}

/** Turns a failed response into an ApiError; only ProblemDetails has a code and a message. */
async function toApiError(response: Response): Promise<ApiError> {
  if (proxyCannotReachApiStatuses.includes(response.status)) {
    return serverUnreachable(response.status)
  }
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

/** The error for a server that cannot be reached; status 0 when there was no answer at all. */
function serverUnreachable(status: number): ApiError {
  return new ApiError(status, serverUnreachableCode, null, serverUnreachableMessage)
}
