# Work log · Step 5b · Frontend views · 2026-11-12 (done)
**Done:**
- tan found the step 5 screen left two endpoints (close, overdue) without a screen; AI log entry; plan and test list approved, D67 (replaces D50's drop-down)
- Three views in `App`: Grant list (status filter, pending count) → Grant detail (tasks, Add, Complete, Close grant) ← Overdue tasks across grants; each list keeps its filter after Back
- T5b.4 (Grant detail) and T5b.5 (Overdue) built in two sessions at once (tan's request), each limited to its own two files; their results rechecked here (tests, breaks, browser)
- An independent reviewer sub-agent read the Grant list; its findings fixed (keyboard button, filter held by `App`, FE-22 split)
- Tried against the real API in the browser: filter Closed → GR-100003 only · open GR-100001 · Close → BR-5 message at the top · Overdue (Open) → "Verify bank details", High, 3 days (Priority column added at tan's request) · row → its grant · Back → Overdue
- 52 frontend tests, `./test.sh` exit 0

**Files touched:**
- `frontend/src/`: `App.tsx` · `GrantList.tsx` · `GrantDetail.tsx` · `OverdueTaskList.tsx` (each with `.test.tsx`; all new but `App.tsx`) · `index.css` · `GrantTasksScreen.tsx` + test removed
- `frontend/src/api/`: `grantsApi.ts` · `grantsApi.test.ts`
- `docs/`: `plan.md` (Step 5b tasks, D67) · `ai-log.md` · `work-log/step-5b-views.md`

### Review
| # | Task | Result |
|---|------|--------|
| 1 | T5b.2 | 🔧 reviewer: mouse-only rows → reference `<button>` (FE-24b) · filter reset after Back → held in `App` · FE-22 caught two bugs → FE-22a/b |
| 2 | T5b.2 | 🔧 two ways to open a grant by keyboard (Grant list: button handler + `stopPropagation`; Overdue: the click reaches the row) → both use the simpler one |
| 3 | Break script | 🔧 a failed step skipped the restore and left a broken line in `grantsApi.ts`; restored, the script now restores on any exit; AI log entry |
| 4 | T5b.4 | 🔧 back button said "← Grants" but returns to the list it came from → "← Back" |
| 5 | T5b.2 | ✅ not taken: "Try again", "Loading…" (not in the brief; changing the filter reloads) |
| 6 | All | ✅ one `switch` on the view in `App` · no router, no new package · no Dxx in code |

**Missing:** Close always sending grant 1 stays green (every Close test uses grant 1) · in Grant detail, the `isLatest` guard and the form's `key` are no longer caught by any test: each grant is now a fresh screen (tan to decide, see questions) · rows in the API's order not checked

**Review it yourself:**
1. `App.tsx`: why the filters live in `App`, and why `key={view.grantId}` on `GrantDetail`.
2. `GrantList.tsx`: why `setGrants(null)` before the filter changes; why the button has no click handler.
3. `GrantDetail.tsx` `handleClose`: why the error is cleared first and the response shown as returned.
4. `docs/plan.md` D67: is "no router" the trade-off you want?

### Test
| # | Task | Red | Green | Break on purpose → result |
|---|------|-----|-------|---------------------------|
| 1 | T5b.4 | ✅ 9 failed (stub) | ✅ 9 passed | 🔧 4 red + 1 green (Close always grant 1, under Missing); the session's own run: see its report |
| 2 | T5b.1 | ✅ API-4 failed (no query) | ✅ 12 passed | ✅ 3 red: query name · close as GET · overdue query name |
| 3 | T5b.2 | ✅ 6 failed (stub) · FE-24b failed (no button) | ✅ 8 passed | ✅ 11 red: count field · filter ignored · empty message · rows kept · error kept · no guard · row not clickable · wrong id · opened twice · no button (×2) |
| 4 | T5b.3 | ✅ FE-34, FE-35 failed (no navigation) | ✅ 52 passed · `./test.sh` exit 0 | ✅ 3 red: wrong grant opened · Back always to Grants · filter reset on Back |
| 5 | T5b.5 | ✅ 6 failed (stub) | ✅ 6 passed | ✅ 11 red (session `overdue-list`): days missing · default filter · filter ignored · empty message · error kept · rows kept · no guard · task id as grant id · no button · opened twice |

**Test cases** (each catches a different bug)
| # | Scenario → expected | Catches |
|---|---------------------|---------|
| API-4..6 | `?status=Closed` · PUT `/grants/3/close` · `?grantStatus=All` | Filter not sent · wrong URL or method · wrong query name |
| FE-19..24b | Grant list: row fields · Closed asked from the API · "No grants match this filter." · load fails → message, no rows · next load clears it · older answer last · row and Enter open the grant once | Count field · filter ignored · bare header · other filter's rows · old error · older answer wins · wrong grant, mouse only, opened twice |
| FE-1, 5, 11, 13, 15, NET-6 | Grant detail: kept from step 5 | As in step 5 |
| FE-25..27 | Close → Closed, button gone · refused (BR-5) → API message, next Close clears it · Closed grant → no button | Wrong call or screen not updated · refusal swallowed, old error · close offered on a closed grant |
| FE-28..33 | Overdue: default Open, priority and days shown · All asked · "No overdue tasks." · load fails, then clears · older answer last · row and Enter open the task's grant | Default filter · filter ignored · bare header · old error · older answer · task id as grant id |
| FE-34, 35 | List → grant → Back keeps the filter · Overdue → grant → Back to Overdue | Filter lost · Back to the wrong list |

**Missing:** dropped with the drop-down: FE-14, FE-16, FE-17 (switching grant inside one screen), NET-5 (covered by FE-22a)

**Test it yourself:**

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| Everything | `./test.sh` | exit 0 | — |
| Grant list | `(cd frontend && npx vitest run src/GrantList.test.tsx)` | 8 passed | `<td>{grant.pendingTaskCount}</td>` → `<td>{grant.id}</td>` → FE-19 fails |
| Grant detail | `(cd frontend && npx vitest run src/GrantDetail.test.tsx)` | 9 passed | `{grant.status === 'Open' && (` → `{true && (` → FE-25, FE-27 fail |
| Overdue | `(cd frontend && npx vitest run src/OverdueTaskList.test.tsx)` | 6 passed | `onOpenGrant(task.grantId)` → `onOpenGrant(task.id)` → FE-33 fails |
| App | `(cd frontend && npx vitest run src/App.test.tsx)` | 2 passed | `setView({ name: view.backTo })` → `setView({ name: 'grants' })` → FE-35 fails |
| In the browser | API and `(cd frontend && npm run dev)`, http://localhost:5173 | Grants: Status Closed → GR-100003 only · click GR-100001 → its tasks · Close grant → "Cannot close … Verify bank details" at the top · ← Back → still filtered · Overdue tasks → "Verify bank details", 3 · click → GR-100001 · ← Back → Overdue tasks | — |
