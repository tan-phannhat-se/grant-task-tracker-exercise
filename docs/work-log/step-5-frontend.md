# Work log · Step 5 · Frontend · 2026-11-12 (in progress)
**Done:**
- Plan approved: D50–D54 (layout, where errors show, closed grant, client checks, API types in one module); tasks T5.1–T5.6 in `plan.md`
- Real responses checked with `curl` before writing the client: shapes match D35; the empty-body 400 has no `Content-Type`
- T5.1 red: API-1..3; `grantsApi.ts` stub throws

**Files touched:**
- `frontend/src/api/`: `grantsApi.ts` · `grantsApi.test.ts` (T5.1)
- `docs/plan.md` (Step 5 task table, D50–D54) · `docs/work-log/step-5-frontend.md`

### Review
| # | Task | Result |
|---|------|--------|
| 1 | T5.1 | ⏳ |

**Missing:** T5.2–T5.6

**Review it yourself:**
1. `docs/plan.md` D50–D54: do they say what you chose?
2. `frontend/src/api/grantsApi.ts`: do the types match the `curl` output (`GET /grants/1`)?
3. `grantsApi.test.ts`: each test's "Catches" line names a different bug.

### Test
| # | Task | Red | Green | Break on purpose → result |
|---|------|-----|-------|---------------------------|
| 1 | T5.1 | ⏳ | ⏳ | ⏳ |

**Test cases** (14; each catches a different bug)
| # | Scenario → expected | Catches |
|---|---------------------|---------|
| API-1 | `addTask` → POST `/api/grants/1/tasks` with the JSON body | Wrong URL, method or body |
| API-2 | 409 ProblemDetails → `ApiError` with `code`, `field`, message = `detail` | Error body ignored |
| API-3 | 400 with an empty body → `ApiError` "Check the form and try again." | Crash on an empty body |
| FE-1 | Choose a grant in the drop-down → its tasks show | Tasks not reloaded on change |
| FE-2 | `isOverdue` task → "Overdue" label; other tasks → none | Highlight missing, or on every task |
| FE-3 | Pending → Complete button; completed → no button, "Completed on {date}" | Complete offered on a completed task |
| FE-4 | `completedLate` → "Completed late" label | Label missing, or on the wrong task |
| FE-5 | Click Complete → PUT; row shows the date from the response | Screen uses its own "today" |
| FE-6 | Title empty or spaces → error, no request | Check missing, or not trimmed |
| FE-7 | Title 101 characters → error; 100 → sent | Length check missing · `>=` instead of `>` |
| FE-8 | No due date → error, no request | Date check missing |
| FE-9 | API 400 with `field: dueDate` → error under Due date | Field errors lost or shown in the wrong place |
| FE-10 | API 409 `grant-closed` → error in the box at the top of the form | Errors without a field swallowed |
| FE-11 | Task added → it is in the table, form cleared | Table not refreshed |

**Missing:** T5.2–T5.6

**Test it yourself:**

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| API client (T5.1) | `cd frontend && npx vitest run src/api/grantsApi.test.ts` | Now (red): 3 failed, `Not implemented` · after green: 3 passed | ⏳ written at green |
| Types | `cd frontend && npx tsc -b` | No output | Change `dueDate: string` to `number` in `Task` → type errors in the test |
| Real responses | API running, then `curl -s http://localhost:5080/grants/1` | Fields match `Task` and `GrantDetail` | — |
