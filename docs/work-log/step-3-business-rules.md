# Work log · Step 3 · Business rules · 2026-11-11–12 (done)
**Done:**
- T3.1 red: 3 BR-1 tests
- T3.1 green: BR-1 in `Grant.AddTask`, minimal `Grant.Close()` (sets Closed; BR-5 left for tan as a TODO)
- T3.2 red: 5 BR-3 tests (6 runs)
- T3.2 green: BR-3 in `Grant.AddTask` (trim, then one `switch` on the length; the trimmed title is saved)
- T3.3 red: 5 BR-2 tests
- T3.3 green: BR-2 in `Grant.AddTask` (earliest allowed date = later of today and the application date; the message names it as `yyyy-MM-dd`)
- T3.4 red: 4 BR-4 tests
- T3.4 green: BR-4 in `GrantTask.Complete` (records today; a repeat keeps the first date)
- T3.5 red: 6 BR-5 tests (written by the AI; the code is tan's)
- T3.5 green: BR-5 in `Grant.Close()`, **written by tan**; AI review found 2 bugs (closed before checking, Medium blocked) and 4 style points, all fixed by tan
- T3.6 red: 6 BR-6 tests (7 runs) and an `IsCompletedLate` stub so they compile
- T3.6 green: `IsOverdue(today)` (BR-6) and `IsCompletedLate` (D26) in `GrantTask`; no `NotImplementedException` left in the domain
- T3.7: independent review by three subagents (logic · 29 mutations · consistency with brief and plan); no logic bug; 8 surviving mutations closed with 8 new tests and 3 extra asserts; `Tasks` made read-only; D22 and D25 reworded
- T3.8: undefined priority refused in the domain (D30, test PR-1); decisions D31 (how 100 characters are counted) and D32 (summaries on decision-carrying properties)

**Files touched:**
- `backend/src/GrantTracker.Api/Domain/Grant.cs`
- `backend/src/GrantTracker.Api/Domain/GrantTask.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br1ClosedGrantTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br2DueDateTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br3TitleTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br4CompleteTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br5CloseGrantTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br6OverdueTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/StoredFieldsTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/PriorityTests.cs`
- `backend/src/GrantTracker.Api/Domain/ErrorCodes.cs`
- `docs/plan.md`
- `CLAUDE.md` (switch rule, self-check rule, rule-conflict rule)
- `docs/ai-log.md` (entries 9 and 10)

### Review
| # | Task | Result |
|---|------|--------|
| 1 | T3.1 BR-1 | ✅ One test per case, fixed dates, every test has a summary with "Catches" |
| 2 | T3.2 BR-3 | 🔧 Two `if`s on one value → one `switch` (tan noticed) |
| 3 | T3.3 BR-2 | ✅ Date in the message uses the invariant culture, so it is the same on every machine |
| 4 | T3.4 BR-4 | ✅ A repeat returns early and keeps the first date |
| 5 | T3.5 BR-5 (tan's code) | 🔧 First version closed before checking and blocked Medium; fixed by tan after review |
| 6 | T3.6 BR-6 | ✅ Overdue and completed late computed, never stored |
| 7 | T3.7 independent review | 🔧 `Tasks` returned the real list → `AsReadOnly()`; `Grant` summary named every rule → fixed; B1–B3 decided by tan (D30–D32) |

### Test
| # | Task | Red | Green | Break on purpose → result (all restored) |
|---|------|-----|-------|------------------------------------------|
| 1 | T3.1 BR-1 | 3/3 failed (`NotImplementedException`) | 3/3 | No check → 2 red · task added before the check → 1 red |
| 2 | T3.2 BR-3 | 5/6 failed on asserts | 9/9 | No trim → 3 red · `>=` → 2 red |
| 3 | T3.3 BR-2 | 3/5 failed on asserts | 14/14 | `<=` → 2 red · application date ignored → 2 red · codes swapped → 3 red |
| 4 | T3.4 BR-4 | 4/4 failed (`NotImplementedException`) | 18/18 | No repeat guard → 1 red · `CompletedDate = DueDate` → 4 red |
| 5 | T3.5 BR-5 | 2/6 failed on asserts | 24/24 | Close before the check → 1 red · no High filter → 3 red · no completion filter → 1 red |
| 6 | T3.6 BR-6 | 7/7 failed (`NotImplementedException`) | 31/31 | `<=` → 1 red · no completion check → 3 red · late uses `>=` → 1 red |
| 7 | T3.7 review | 29 mutations: 20 killed, 9 survived | 39/39 | The 8 survivors that matter now fail at least one new test; the 9th only changes message wording |
| 8 | T3.8 priority | 2/2 runs failed on asserts | 42/42 | Priority check removed → 3 red · range check instead of `Enum.IsDefined` → 1 red (150) |

**Missing:** loading a grant with its tasks before `Close()` is a step 4 test (see `plan.md`).

**Test it yourself:** run all with `dotnet test backend` → 42 passed, then each group:

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| BR-1 | `dotnet test backend --filter Br1` | 4 passed | Comment out the `if (Status == GrantStatus.Closed)` check in `Grant.AddTask` → 3 fail |
| BR-3 | `dotnet test backend --filter Br3` | 7 passed | `case > GrantTask.TitleMaxLength` → `>=` → BR3-2 and BR3-5 fail |
| BR-2 | `dotnet test backend --filter Br2` | 6 passed | `dueDate < earliestDueDate` → `<=` → BR2-2 and BR2-4 fail |
| BR-4 | `dotnet test backend --filter Br4` | 5 passed | Remove `if (IsCompleted) return;` in `GrantTask.Complete` → BR4-2 and BR4-5 fail |
| BR-5 | `dotnet test backend --filter Br5` | 6 passed | Move `Status = GrantStatus.Closed;` above the check in `Grant.Close` → BR5-1 fails |
| BR-6 | `dotnet test backend --filter Br6` | 9 passed | `DueDate < today` → `<=` in `GrantTask.IsOverdue` → BR6-2 fails |
| Stored fields | `dotnet test backend --filter StoredFields` | 2 passed | `IdempotencyKey = null;` in the `GrantTask` constructor → ST-1 fails |
| Priority | `dotnet test backend --filter PriorityTests` | 3 passed | `if (!Enum.IsDefined(priority))` → `if (false)` → 3 fail |
