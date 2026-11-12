import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { addTask, ApiError, closeGrant, getGrants, getOverdueTasks, type Task } from './grantsApi'

const savedTask: Task = {
  id: 7,
  grantId: 1,
  title: 'Check documents',
  dueDate: '2026-11-20',
  priority: 'High',
  completedDate: null,
  isOverdue: false,
  completedLate: false,
}

const key = '3f2b6c1e-8d4a-4f0b-9c7e-2a1d5e6f7a8b'
const unreachable = {
  code: 'server-unreachable',
  field: null,
  message: 'Cannot reach the server. Check your connection and try again.',
}

const fetchMock = vi.fn()

beforeEach(() => {
  vi.stubGlobal('fetch', fetchMock)
})

afterEach(() => {
  vi.unstubAllGlobals()
  fetchMock.mockReset()
})

describe('grantsApi', () => {
  /**
   * API-1: addTask → POST /api/grants/1/tasks with the task as JSON, returns the saved task.
   * Catches: wrong URL, method, header or body.
   */
  it('addTask_PostsTaskAsJson_ReturnsSavedTask', async () => {
    fetchMock.mockResolvedValue(Response.json(savedTask, { status: 201 }))

    const result = await addTask(
      1,
      { title: 'Check documents', dueDate: '2026-11-20', priority: 'High' },
      key,
    )

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/grants/1/tasks')
    expect(init.method).toBe('POST')
    expect(init.headers).toMatchObject({ 'Content-Type': 'application/json' })
    expect(JSON.parse(init.body)).toEqual({
      title: 'Check documents',
      dueDate: '2026-11-20',
      priority: 'High',
    })
    expect(result).toEqual(savedTask)
  })

  /**
   * API-2: 400 ProblemDetails with code and field
   * → ApiError with code, field, and detail as message.
   * Catches: the API's error body ignored, so the screen cannot say what is wrong or where.
   */
  it('addTask_ProblemDetails_ThrowsApiErrorWithCodeFieldAndDetail', async () => {
    const problem = {
      status: 400,
      detail: 'Due date must be on or after 2026-11-11.',
      code: 'due-date-in-past',
      field: 'dueDate',
    }
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify(problem), {
        status: 400,
        headers: { 'Content-Type': 'application/problem+json' },
      }),
    )

    const error = await addTask(
      1,
      { title: 'x', dueDate: '2020-01-01', priority: 'Low' },
      key,
    ).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      code: 'due-date-in-past',
      field: 'dueDate',
      message: 'Due date must be on or after 2026-11-11.',
    })
  })

  /**
   * API-3: 400 with an empty body (a badly formatted value)
   * → ApiError "Check the form and try again."
   * Catches: a crash while reading an empty body, so the officer sees nothing.
   */
  it('addTask_EmptyBody400_ThrowsCheckTheFormError', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 400 }))

    const error = await addTask(
      1,
      { title: 'x', dueDate: '2026-11-20', priority: 'Low' },
      key,
    ).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      code: null,
      field: null,
      message: 'Check the form and try again.',
    })
  })

  /**
   * KEY-1: addTask with a key → the key in the Idempotency-Key header.
   * Catches: the key not sent, so a retried request adds the task twice.
   */
  it('addTask_SendsTheKeyInTheIdempotencyKeyHeader', async () => {
    fetchMock.mockResolvedValue(Response.json(savedTask, { status: 201 }))

    await addTask(1, { title: 'x', dueDate: '2026-11-20', priority: 'Low' }, key)

    const [, init] = fetchMock.mock.calls[0]
    expect(init.headers).toMatchObject({ 'Idempotency-Key': key })
  })

  /**
   * API-4: getGrants('Closed') → GET /api/grants?status=Closed.
   * Catches: the filter not sent, so every grant comes back whatever the officer chose.
   */
  it('getGrants_WithStatus_SendsItAsTheStatusFilter', async () => {
    fetchMock.mockResolvedValue(Response.json([]))

    await getGrants('Closed')

    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/grants?status=Closed')
  })

  /**
   * API-5: closeGrant(3) → PUT /api/grants/3/close.
   * Catches: the wrong URL or method, so the grant is never closed.
   */
  it('closeGrant_PutsToTheGrantsCloseUrl', async () => {
    fetchMock.mockResolvedValue(Response.json({}))

    await closeGrant(3)

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/grants/3/close')
    expect(init.method).toBe('PUT')
  })

  /**
   * API-6: getOverdueTasks('All') → GET /api/tasks/overdue?grantStatus=All.
   * Catches: the filter sent under the grant list's name (status), which the overdue list ignores.
   */
  it('getOverdueTasks_SendsTheGrantStatusFilter', async () => {
    fetchMock.mockResolvedValue(Response.json([]))

    await getOverdueTasks('All')

    const [url] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/tasks/overdue?grantStatus=All')
  })
})

describe('grantsApi when the server cannot be reached', () => {
  /**
   * NET-1: fetch fails (no connection, or the timeout) → ApiError server-unreachable, status 0.
   * Catches: the browser's "Failed to fetch" shown to the officer, or the failure swallowed.
   */
  it('getGrants_FetchFails_ThrowsServerUnreachable', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))

    const error = await getGrants().catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 0, ...unreachable })
  })

  /**
   * NET-2: a proxy answers 502, 503 or 504 with an empty body → ApiError server-unreachable.
   * Catches: a proxy that cannot reach the API shown as "Something went wrong".
   */
  it.each([502, 503, 504])('getGrants_ProxyAnswers%i_ThrowsServerUnreachable', async (status) => {
    fetchMock.mockResolvedValue(new Response(null, { status }))

    const error = await getGrants().catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status, ...unreachable })
  })

  /**
   * NET-3: every request is sent with an abort signal, so it ends after a time limit.
   * Catches: no time limit, so a request that never gets an answer leaves the screen waiting.
   */
  it('getGrants_IsSentWithATimeLimit', async () => {
    fetchMock.mockResolvedValue(Response.json([]))

    await getGrants()

    const [, init] = fetchMock.mock.calls[0]
    expect(init.signal).toBeInstanceOf(AbortSignal)
  })
})
