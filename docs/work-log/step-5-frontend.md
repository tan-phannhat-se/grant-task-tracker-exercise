# Work log · Step 5 · Frontend · 2026-11-12 (done)
**Done:**
- One screen, built test-first task by task (T5.1–T5.5): typed API client, grant drop-down, task table (overdue, completed on, completed late), Complete, Add task form (client checks, API errors)
- Decisions D50–D54 (tan); added while building: opens on the first grant and "No tasks yet." (D50), errors outside the form at the top of the screen (D51), priority starts at Medium (D53)
- Real responses checked with `curl` before writing the client; `user.type` on a date input checked with a throwaway test before relying on it
- `./test.sh` checks frontend lines over 100 characters, counted in UTF-8 (bash grep counts bytes by default)
- `CLAUDE.md`: frontend exception to "one type per file" (props interfaces, API types)
- Tried in the browser against the real API: overdue highlight, Closed label, "No tasks yet.", Complete, BR-2 under Due date, BR-1 at the top of the form, a valid task added in date order
- 21 frontend tests; 34 breaks on purpose, each turned a test red
- Fixes after the step: a screen error stayed after the next grant loaded fine (tan found it), now cleared on every successful load (FE-14, FE-15); after a failed load the previous grant's table and form stayed under the new grant's name (AI found it), now hidden as soon as a grant is chosen (FE-16)

**Files touched:**
- `frontend/src/`: `GrantTasksScreen.tsx` · `TaskList.tsx` · `AddTaskForm.tsx` (each with `.test.tsx`) · `App.tsx` · `index.css`
- `frontend/src/api/`: `grantsApi.ts` · `grantsApi.test.ts`
- root: `test.sh` · `CLAUDE.md`
- `docs/`: `plan.md` (Step 5 tasks, D50–D54) · `ai-log.md` (3 entries) · `work-log/step-5-frontend.md`

### Review
| # | Task | Result |
|---|------|--------|
| 0 | Fixes | 🔧 `screenError` cleared only by Complete: now cleared when a grant loads · 🔧 previous grant shown after a failed load: hidden on change; AI log entries |
| 1 | Log | 🔧 "Test it yourself" and "Review it yourself" left empty at T5.1 red: filled, AI log entry |
| 2 | T5.2 | 🔧 `isLatest && setGrant(...)` rewritten as a plain `if` ("no clever tricks") |
| 3 | T5.4 | 🔧 props interfaces sat in their component's file against "one type per file": tan agreed, `CLAUDE.md` exception |
| 4 | T5.5 | 🔧 10 comment lines over 100 characters (Prettier does not wrap comments): wrapped, now checked by `./test.sh` |
| 5 | T5.1 | ✅ diff against D35, D49, D51, D54: only the four calls this screen needs |
| 6 | T5.3–T5.5 | ✅ one `switch` on the API's `field` · title rule in one function (two `if`s: TypeScript has no `case > 100`) · no new package |

**Missing:** no test for: the `isLatest` guard (switching grants faster than the API answers) · the form emptied on a grant change (`key`) · an old screen error cleared by the next Complete · messages for errors that are not ProblemDetails (load, complete, add; step 6 adds "server unreachable")

**Review it yourself:**
1. `docs/plan.md` D50–D54: do they say what you chose?
2. `grantsApi.ts`: why `toApiError` reads `Content-Type` before the body.
3. `GrantTasksScreen.tsx`: why the `isLatest` flag, and why `key={grant.id}` on the form.
4. Each test's "Catches" line names a different bug.

### Test
| # | Task | Red | Green | Break on purpose → result |
|---|------|-----|-------|---------------------------|
| 1 | T5.1 | ✅ 3 failed (`Not implemented`) | ✅ 3 passed | ✅ 5 red: no `/api` prefix · POST→PUT · `detail` ignored · `field` dropped · always parse JSON |
| 2 | T5.2 | ✅ 4 failed (stubs render nothing) · FE-12: 1 failed | ✅ 8 passed | ✅ 8 red: every row overdue · no Overdue label · completed shown as pending · late label everywhere · drop-down ignored · opens empty · status hidden · empty check removed |
| 3 | T5.3 | ✅ FE-5 failed (button does nothing) · FE-13 failed (error swallowed) | ✅ 10 passed | ✅ 5 red: button calls nothing · wrong id · own "today" · row not replaced · API message ignored |
| 4 | T5.4 | ✅ 5 failed (no form) | ✅ 15 passed | ✅ 7 red: not trimmed · `>=` · each check removed · error not linked to its input · untrimmed title sent |
| 6 | Fixes | ✅ FE-14 failed (error never removed) · FE-15 written after a break was not caught · FE-16 failed (old tasks still shown) | ✅ 30 passed (with step 6's) · `./test.sh` exit 0 | ✅ 3 red: no clear after a grant change → FE-14 · no clear after reload → FE-15 · old grant kept on change → FE-16 |
| 5 | T5.5 | ✅ 3 failed (no API message · form not on the screen) | ✅ 18 passed · `./test.sh` exit 0 | ✅ 6 red: dueDate error misplaced · field-less error swallowed · errors not caught · `onAdded` not called · title not cleared · grant not reloaded |

**Test cases · API client and task table** (each catches a different bug)
| # | Scenario → expected | Catches |
|---|---------------------|---------|
| API-1 | `addTask` → POST `/api/grants/1/tasks` with the JSON body | Wrong URL, method or body |
| API-2 | 400 ProblemDetails → `ApiError` with `code`, `field`, message = `detail` | Error body ignored |
| API-3 | 400 with an empty body → "Check the form and try again." | Crash on an empty body |
| FE-1 | Opens on the first grant; choose another → its tasks and status | Tasks not reloaded · screen opens empty |
| FE-2 | `isOverdue` → highlighted row + "Overdue"; other tasks → neither | Highlight missing, or on every task |
| FE-3 | Pending → Complete button; completed → "Completed on {date}", no button | Complete offered on a completed task |
| FE-4 | `completedLate` → "Completed late"; on time → no label | Label missing, or on every completed task |
| FE-12 | No tasks → "No tasks yet.", no table (tan asked) | A bare header that looks like a loading error |

**Test cases · Complete and Add task**
| # | Scenario → expected | Catches |
|---|---------------------|---------|
| FE-5 | Complete → PUT; row shows the date from the response | Screen uses its own "today" |
| FE-6 | Title empty or spaces → error under Title, nothing sent | Check missing · not trimmed |
| FE-7 | 101 characters → error (7a); 100 with spaces around → sent trimmed (7b) | Check missing · `>=` · counted before trimming |
| FE-8 | No due date → error under Due date, nothing sent | Date check missing |
| FE-9 | API 400 on `dueDate` (BR-2) → message under Due date | Field errors lost or misplaced |
| FE-10 | API 409 `grant-closed` (BR-1) → message at the top of the form | Field-less errors swallowed |
| FE-11 | Valid task → new row in the table, form empty | Table not refreshed · form keeps its values |

**Test cases · Errors at the top of the screen**
| # | Scenario → expected | Catches |
|---|---------------------|---------|
| FE-13 | Complete fails (404) → API message at the top of the screen | A failed Complete swallowed |
| FE-14 | A grant fails to load, the next one loads → error gone | An old error left after the server is back |
| FE-15 | Error shown, a task is added and the grant reloads → error gone | The reload leaving an old error |
| FE-16 | A new grant fails to load → the previous grant's tasks and form are gone | A task added to the wrong grant |

**Missing:** none beyond the list under Review

**Test it yourself:**

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| Everything | `./test.sh` | exit 0 | — |
| API client | `(cd frontend && npx vitest run src/api)` | 3 passed | `grantsApi.ts`: `method: 'POST'` → `'PUT'` → API-1 fails |
| Task table | `(cd frontend && npx vitest run src/TaskList.test.tsx)` | 4 passed | `TaskList.tsx`: `className={task.isOverdue ? 'overdue' : undefined}` → `className="overdue"` → FE-2 fails |
| Screen | `(cd frontend && npx vitest run src/GrantTasksScreen.test.tsx)` | 7 passed | `TaskList.tsx`: `onComplete(task.id)` → `onComplete(task.grantId)` → FE-13 fails |
| Form | `(cd frontend && npx vitest run src/AddTaskForm.test.tsx)` | 7 passed | `AddTaskForm.tsx`: `case 'dueDate':` → `case 'due-date':` → FE-9 fails |
| Line length | `./test.sh` | "Frontend line length" prints nothing | Add a 101-character comment line → `./test.sh` prints it and stops |
| Types | `(cd frontend && npx tsc -b)` | No output | `Task.dueDate: string` → `number` → type error in a test |
| In the browser | API running, `(cd frontend && npm run dev)`, http://localhost:5173 | GR-100001 opens, "Verify bank details" pink + "Overdue" · Complete → "Completed on …" + "Completed late" · due 2020-01-01 → BR-2 under Due date · GR-100003 → "Closed", add → BR-1 at the top of the form · GR-100004 → "No tasks yet." · stop the API, change grant → error, no table, no form; start it, change grant → error gone, grant shown | — |
