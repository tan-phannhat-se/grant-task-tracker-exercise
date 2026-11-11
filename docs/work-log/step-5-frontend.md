# Work log · Step 5 · Frontend · 2026-11-12 (in progress)
**Done:**
- Plan approved: D50–D54 (layout, where errors show, closed grant, client checks, API types in one module); tasks T5.1–T5.6 in `plan.md`
- Real responses checked with `curl` before writing the client: shapes match D35; the empty-body 400 has no `Content-Type`
- T5.1 red: API-1..3; `grantsApi.ts` stub throws
- T5.1 green: `send` (one place for fetch and errors), `toApiError` (reads the body only when it is ProblemDetails), four calls; D50 adds "opens on the first grant" (tan chose)
- T5.2 red: FE-1 (screen, fake `fetch` by URL; also checks the "Closed" status shows) · FE-2..4 (`TaskList` alone, no `fetch`); `GrantTasksScreen` and `TaskList` stubs render nothing
- T5.2 green: `TaskList` (row class `overdue` + "Overdue" text label, Complete button not wired yet), `GrantTasksScreen` (loads grants, opens on the first, reloads on change; an older answer cannot replace a newer one), `App.tsx`, `index.css`
- Tried in the browser with the real API: GR-100001 opens with "Verify bank details" highlighted; GR-100003 shows Closed; GR-100004 shows an empty table
- FE-12 (tan asked): a grant with no tasks shows "No tasks yet."; red seen first, then green; D50 updated
- T5.3 red: FE-5 (fake API answers PUT with a completed date long past); `TaskList` gets an `onComplete` prop, the screen passes a stub that does nothing
- T5.3 green: Complete calls the API and puts the returned task in its row; D51 extended (tan chose: errors outside the form go in the box at the top of the screen)
- FE-13 (needed by that choice): test written, seen red (error swallowed), then `catch` added; `loadError` renamed `screenError`
- Tried in the browser: Complete on "Verify bank details" → "Completed on 2026-11-11" + "Completed late", highlight gone (the server's today is UTC, the default time zone)
- T5.4 red: FE-6 (2 inputs), FE-7a, FE-7b (100 characters with spaces around, so it also catches "counted before trimming"), FE-8; `AddTaskForm` stub renders nothing. Checked first that `user.type` fills a date input in jsdom (throwaway test, deleted)
- D51's extension was committed inside step 6's commit `768362f` (shared `plan.md`)
- T5.4 green: `AddTaskForm` (title trimmed then checked, due date required, priority starts at Medium; errors linked to their input with `aria-describedby`; sends with `addTask`); D53 adds "Priority starts at Medium" (tan chose)
- T5.5 red: FE-9, FE-10 (`AddTaskForm` alone), FE-11 (screen; the fake API now adds tasks, with fresh data for each test so no test sees another's task)
- `./test.sh` checks frontend lines over 100 characters (tan approved); counted in UTF-8 because `bash` grep counts bytes by default; the 10 long comment lines wrapped

**Files touched:**
- `frontend/src/api/`: `grantsApi.ts` · `grantsApi.test.ts` (T5.1)
- `frontend/src/`: `GrantTasksScreen.tsx` + `.test.tsx` · `TaskList.tsx` + `.test.tsx` · `App.tsx` · `index.css` (T5.2) · same four files (T5.3) · `AddTaskForm.tsx` + `.test.tsx` (T5.4) · `AddTaskForm.test.tsx` · `GrantTasksScreen.test.tsx` (T5.5) · `TaskList.tsx` · `api/grantsApi.ts` · `api/grantsApi.test.ts` (wrapped lines)
- `test.sh` (frontend line length)
- `docs/plan.md` (D50: opens on the first grant; D53: priority) · `CLAUDE.md` (frontend exception to "one type per file") · `docs/ai-log.md`
- `docs/plan.md` (Step 5 task table, D50–D54) · `docs/work-log/step-5-frontend.md`

### Review
| # | Task | Result |
|---|------|--------|
| 5 | T5.5 | 🔧 10 comment lines over 100 characters (Prettier does not wrap comments): all wrapped; `./test.sh` now checks it |
| 4 | T5.4 | 🔧 see last item · ✅ title rule in one function (`titleErrorFor`) · two `if`s, not a `switch`: TypeScript has no `case > 100` like C#, and `switch (true)` would be a trick · 🔧 props interfaces (`TaskListProps`, `AddTaskFormProps`) sat in their component's file against "one type per file": tan agreed, exception added to `CLAUDE.md`, AI log entry |
| 3 | T5.3 | ✅ no short-circuit tricks (`if` blocks) · `screenError` named for what it holds now |
| 2 | T5.2 | 🔧 `isLatest && setGrant(...)` rewritten as a plain `if` ("no clever tricks") · ✅ no new package, no state library |
| 1 | T5.1 | ✅ diff re-read against D35, D49, D51, D54: no extra calls (close and overdue not used by this screen) |

**Missing:** T5.5–T5.6 · the form is not on the screen yet (T5.5)

**Review it yourself:**
1. `docs/plan.md` D50–D54: do they say what you chose?
2. `frontend/src/api/grantsApi.ts`: do the types match the `curl` output (`GET /grants/1`)?
3. `grantsApi.test.ts`: each test's "Catches" line names a different bug.
4. `GrantTasksScreen.tsx`: the `isLatest` flag in the second `useEffect`: can you explain why it is there?

### Test
| # | Task | Red | Green | Break on purpose → result |
|---|------|-----|-------|---------------------------|
| 5 | T5.5 | ✅ 3 failed (FE-9, FE-10: message never shown, unhandled rejection · FE-11: form not on the screen) | ⏳ | ⏳ |
| 4 | T5.4 | ✅ 5 failed (no form: no Title or Due date label) | ✅ 15 passed · frontend checks green; `./test.sh` red only on step 6's 2 `HealthCheckTests` | ✅ 7 breaks, each red: not trimmed before check → FE-6, FE-7b · `>=` → FE-7b · length check removed → FE-7a · empty check removed → FE-6 · due date check removed → FE-8 · error not linked to input → FE-6, FE-7a · untrimmed title sent → FE-7b |
| 3 | T5.3 | ✅ 1 failed (no "Completed on 2026-11-01": the button does nothing) · FE-13: 1 failed (no alert, unhandled rejection) | ✅ 10 passed · frontend checks green; `./test.sh` red only on step 6's red backend tests (`HealthCheckTests`, `AddTaskRetryTests`) | ✅ 5 breaks, each red: button calls nothing → FE-5, FE-13 · wrong id sent → FE-13 · screen uses its own today → FE-5 · row not replaced → FE-5 · API message ignored → FE-13 |
| 2 | T5.2 | ✅ 4 failed (stubs render nothing: no row, no task text) | ✅ 7 passed · `./test.sh` exit 0 | ✅ 7 breaks, each red: every row overdue → FE-2 · no Overdue label → FE-2 · completed shown as pending → FE-3, FE-4 · late label on every completed → FE-4 · drop-down change ignored → FE-1 · opens empty → FE-1 · status not shown → FE-1 · FE-12: empty check removed → FE-12 |
| 1 | T5.1 | ✅ 3 failed (`Not implemented`) | ✅ 3 passed · `./test.sh` exit 0 | ✅ 5 breaks, each red: no `/api` prefix → API-1 · POST→PUT → API-1 · `detail` ignored → API-2 · `field` dropped → API-2 · always parse JSON → API-3 |

**Test cases** (16; each catches a different bug)
| # | Scenario → expected | Catches |
|---|---------------------|---------|
| API-1 | `addTask` → POST `/api/grants/1/tasks` with the JSON body | Wrong URL, method or body |
| API-2 | 400 ProblemDetails with `code` and `field` → `ApiError` with `code`, `field`, message = `detail` | Error body ignored |
| API-3 | 400 with an empty body → `ApiError` "Check the form and try again." | Crash on an empty body |
| FE-1 | Choose a grant in the drop-down → its tasks show | Tasks not reloaded on change |
| FE-2 | `isOverdue` task → "Overdue" label; other tasks → none | Highlight missing, or on every task |
| FE-3 | Pending → Complete button; completed → no button, "Completed on {date}" | Complete offered on a completed task |
| FE-4 | `completedLate` → "Completed late" label | Label missing, or on the wrong task |
| FE-5 | Click Complete → PUT; row shows the date from the response | Screen uses its own "today" |
| FE-6 | Title empty or spaces → error, no request | Check missing, or not trimmed |
| FE-7 | Title 101 characters → error (7a); 100 with spaces around → sent trimmed (7b) | Length check missing · `>=` instead of `>` · counted before trimming |
| FE-8 | No due date → error, no request | Date check missing |
| FE-9 | API 400 with `field: dueDate` → error under Due date | Field errors lost or shown in the wrong place |
| FE-10 | API 409 `grant-closed` → error in the box at the top of the form | Errors without a field swallowed |
| FE-11 | Task added → it is in the table, form cleared | Table not refreshed |
| FE-13 | Complete fails (404 `task-not-found`) → the API's message at the top of the screen (added with D51) | A failed Complete swallowed |
| FE-12 | No tasks → "No tasks yet.", no table (added at tan's request) | A bare table header that looks like a loading error |

**Missing:** T5.5–T5.6 · no test for: clearing an old error when Complete is clicked again ·  the `isLatest` guard (switching grants faster than the API answers), the load error message (step 6 tests the unreachable server) · the "Something went wrong" message (an error that is not ProblemDetails and not a 400) has no test yet; the unreachable-server case is OP-3 in step 6

**Test it yourself:**

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| API client (T5.1) | `(cd frontend && npx vitest run src/api/grantsApi.test.ts)` | 3 passed | In `grantsApi.ts` change `method: 'POST'` to `'PUT'` → API-1 fails |
| Screen and task table (T5.2) | `(cd frontend && npx vitest run src/TaskList.test.tsx src/GrantTasksScreen.test.tsx)` | 4 passed | In `TaskList.tsx` change `className={task.isOverdue ? 'overdue' : undefined}` to `className="overdue"` → FE-2 fails |
| In the browser | API running, `(cd frontend && npm run dev)`, open http://localhost:5173 | Opens on GR-100001; "Verify bank details" pink with "Overdue"; choose GR-100003 → "Closed", "Completed on …"; GR-100004 → "No tasks yet."; Complete on "Verify bank details" → "Completed on {today}" + "Completed late" | — |
| Complete (T5.3) | `(cd frontend && npx vitest run src/GrantTasksScreen.test.tsx)` | 3 passed | In `TaskList.tsx` change `onComplete(task.id)` to `onComplete(task.grantId)` → FE-13 fails |
| Form checks (T5.4) | `(cd frontend && npx vitest run src/AddTaskForm.test.tsx)` | 5 passed | In `AddTaskForm.tsx` change `trimmedTitle.length > titleMaxLength` to `>=` → FE-7b fails |
| API errors and adding (T5.5) | `(cd frontend && npx vitest run src/AddTaskForm.test.tsx src/GrantTasksScreen.test.tsx)` | Now (red): 3 failed · after green: all passed | ⏳ written at green |
| Line length | `./test.sh` | Section "Frontend line length (max 100)" prints nothing | Add a 101-character comment line in `TaskList.tsx` → `./test.sh` prints it and stops |
| Types | `(cd frontend && npx tsc -b)` | No output | Change `dueDate: string` to `number` in `Task` → type errors in the test |
| Real responses | API running, then `curl -s http://localhost:5080/grants/1` | Fields match `Task` and `GrantDetail` | — |
