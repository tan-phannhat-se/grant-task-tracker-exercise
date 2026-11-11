import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { addTask, ApiError, type Task } from './grantsApi'

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

    const result = await addTask(1, {
      title: 'Check documents',
      dueDate: '2026-11-20',
      priority: 'High',
    })

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/grants/1/tasks')
    expect(init.method).toBe('POST')
    expect(init.headers).toEqual({ 'Content-Type': 'application/json' })
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

    const error = await addTask(1, { title: 'x', dueDate: '2020-01-01', priority: 'Low' }).catch(
      (e: unknown) => e,
    )

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

    const error = await addTask(1, { title: 'x', dueDate: '2026-11-20', priority: 'Low' }).catch(
      (e: unknown) => e,
    )

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({
      status: 400,
      code: null,
      field: null,
      message: 'Check the form and try again.',
    })
  })
})
