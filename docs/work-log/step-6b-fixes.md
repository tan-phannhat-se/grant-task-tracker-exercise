<!-- Copy to step-N-<name>.md at the start of a step. Keep every part; write "none" when a part is empty.
     Marks and limits: CLAUDE.md, Workflow step 7. -->
# Work log · Step 6b · Fixes before step 7 · 2026-11-12 (waiting for tan's review)
**Done:**
- Found by five tester sub-agents run end to end before submitting (own ports and database copies, repo read-only); tan asked for the fixes and reviews them afterwards (tasks T6.6–T6.10 in `plan.md`)
- T6.6: AT-9, a due date yesterday through the API → 400 `due-date-in-past` on `dueDate`, naming today, nothing saved
- T6.7: the reload after adding a task now goes through the grant load effect (`reloadCount`), so its "older answer" guard covers it (FE-17)
- T6.8: Add task is disabled while a task is on its way, usable again after any answer (FE-18)
- T6.9: D64 records tan's earlier choice: close vs add at the same moment, README only
- T6.10: SDK pin options tried in scratch clones (table below); tan chose README only (D65): `global.json` unchanged, the README (step 7) says to install the .NET 8 SDK

**Files touched:**
- `backend/tests/GrantTracker.Tests/Api/AddTaskTests.cs` (AT-9)
- `frontend/src/`: `GrantTasksScreen.tsx` + test (FE-17) · `AddTaskForm.tsx` + test (FE-18)
- `docs/`: `plan.md` (tasks T6.6–T6.10, D64, D65) · `ai-log.md` · `work-log/step-6b-fixes.md`

### Review
| # | Task | Result |
|---|------|--------|
| 1 | T6.6 | 🔧 AT-1 claimed to cover BR-2's "today", but no API test refused a past date (ai-log) · ✅ no BR-3 API test added: it would catch no bug AT-4 and the domain tests miss |
| 2 | T6.7 | 🔧 one load path instead of two, so the guard is written once (ai-log) |
| 3 | T6.8 | ✅ the button keeps its name "Add task", so existing tests and labels still find it |
| 4 | Break script | 🔧 restore kept the old file time, so `dotnet test` reused the broken build and `./test.sh` went red; script now touches the file (ai-log) |

**Missing:** README lines for D64 and D65 (step 7) · no browser hand check of FE-17/FE-18 yet

**Review it yourself:**
1. `GrantTasksScreen.tsx`: why does `reloadGrant` only change a number?
2. `AddTaskForm.tsx`: why `finally`, not `then`?
3. `plan.md` D64, D65: are they what you chose?

### Test
| # | Task | Red | Green | Break on purpose → result |
|---|------|-----|-------|---------------------------|
| 1 | T6.6 | Passes on today's code (it fills a gap) | ✅ 95 backend | ✅ today → `DateOnly.MinValue` → only AT-9 red |
| 2 | T6.7 | ✅ FE-17 red (GR-100001 shown under GR-100003) | ✅ 33 frontend | ✅ guard removed → FE-17 · reload not triggered → FE-11, FE-15, FE-17 |
| 3 | T6.8 | ✅ FE-18 red (2 requests) | ✅ 34 frontend; `./test.sh` exit 0 | ✅ never re-enabled → KEY-2, KEY-3, FE-18 · not disabled → FE-18 |

**T6.10 · SDK pin, tried in scratch clones**
| Machine | Today (`latestFeature`) | `latestMajor` |
|---------|-------------------------|---------------|
| .NET 8 SDK | ✅ | ✅ |
| .NET 10 SDK + .NET 8 runtime | ❌ "compatible SDK not found" | ✅ 94/94, format check passes |
| Only .NET 10 | ❌ | ❌ tests need the .NET 8 runtime; forcing .NET 10 → 35 fail (ASP.NET Core 8 test server) |

**Test it yourself:**

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| All | `./test.sh` | exit 0 · 95 backend · 34 frontend | — |
| BR-2 at the API | `dotnet test backend --filter AddTaskTests` | 9 passed | In `AddTaskAsync` pass `DateOnly.MinValue` as today → AT-9 fails |
| Reload after Add | `cd frontend && npx vitest run src/GrantTasksScreen.test.tsx` | 10 passed | Write `if (true)` for `if (isLatest)` before `setGrant` → FE-17 fails |
| Add while sending | `cd frontend && npx vitest run src/AddTaskForm.test.tsx` | 11 passed | `disabled={false}` → FE-18 fails |
| Browser | Add a task, double-click Add | One task; button greys out briefly | — |
