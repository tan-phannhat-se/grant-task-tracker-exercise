# Work log · Step 3 · Business rules · 2026-11-11 (in progress)
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

**Files touched:**
- `backend/tests/GrantTracker.Tests/Domain/Br1ClosedGrantTests.cs`
- `backend/src/GrantTracker.Api/Domain/Grant.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br3TitleTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br2DueDateTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br4CompleteTests.cs`
- `backend/src/GrantTracker.Api/Domain/GrantTask.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br5CloseGrantTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br6OverdueTests.cs`

### Review
| # | Check | Result |
|---|-------|--------|
| 1 | One test per approved case (BR1-1 … BR1-3) | ✅ |
| 2 | Fixed dates only, through `TestData` | ✅ |
| 3 | Every test has a summary: case id, scenario → expected result, and the bug it catches | ✅ tan asked for it |

### Test
| # | Check | Result |
|---|-------|--------|
| 1 | T3.1 red: BR-1 tests fail for the right reason | ✅ 3 failed, all `NotImplementedException` (no compile or setup error) |
| 2 | Format and line length | ✅ |
| 3 | T3.1 green: BR-1 tests pass | ✅ 3/3 |
| 4 | BR-1 check removed on purpose → tests go red | ✅ BR1-2 and BR1-3 failed, restored |
| 5 | Task added before the check on purpose → `Assert.Empty` catches it | ✅ BR1-2 failed, restored |
| 6 | T3.2 red: BR-3 tests fail on their asserts | ✅ 5 of 6 runs failed; BR3-2 (exactly 100) passes because there is no limit yet, and guards `>` vs `>=` once there is |
| 7 | T3.2 green, then `.Trim()` removed and `>` changed to `>=` on purpose | ✅ 9/9 passed · no trim → 3 red · `>=` → 2 red · restored (rerun after the `switch` rewrite) |
| 8 | T3.3 red: BR-2 tests fail on their asserts | ✅ 3 of 5 failed (`Assert.Throws`); BR2-2 and BR2-4 (the two boundaries) pass because there is no date limit yet |
| 9 | T3.3 green, then broken three ways on purpose | ✅ 14/14 passed · `<=` → 2 red (both boundaries) · application date ignored → 2 red · codes swapped → 3 red · restored |
| 10 | T3.4 red | ✅ 4/4 failed on `NotImplementedException` from `Complete`; the green step's break checks will prove the asserts |
| 11 | T3.4 green, then broken twice on purpose | ✅ 18/18 passed · repeat guard removed → BR4-2 red · `CompletedDate = DueDate` → 4 red · restored |
| 12 | T3.5 red: BR-5 tests | ✅ 2 of 6 failed (`Assert.Throws`): BR5-1, BR5-2. The other 4 pass as guards against blocking too much |
| 13 | T3.5 green (tan's code), then broken three ways on purpose | ✅ 24/24 passed · close before the check → BR5-1 red · High filter removed → 3 red · completion filter removed → BR5-4 red · restored |
| 14 | T3.6 red | ✅ 7/7 runs failed on `NotImplementedException`; the other 24 tests still pass. The green step's break checks will prove the asserts |
| 15 | T3.6 green, then broken three ways on purpose | ✅ 31/31 passed · `<=` → BR6-2 red · completion check removed → 3 red · late uses `>=` → BR6-6 red · restored |

**Test it yourself:** run all with `dotnet test backend` → 31 passed, then each rule:

| Rule | Run | Expect | Break on purpose → expect (then undo) |
|------|-----|--------|----------------------------------------|
| BR-1 | `dotnet test backend --filter Br1` | 3 passed | Comment out the `if (Status == GrantStatus.Closed)` check in `Grant.AddTask` → 2 fail |
| BR-3 | `dotnet test backend --filter Br3` | 6 passed | `case > GrantTask.TitleMaxLength` → `>=` → BR3-2 fails |
| BR-2 | `dotnet test backend --filter Br2` | 5 passed | `dueDate < earliestDueDate` → `<=` → BR2-2 and BR2-4 fail |
| BR-4 | `dotnet test backend --filter Br4` | 4 passed | Remove `if (IsCompleted) return;` in `GrantTask.Complete` → BR4-2 fails |
| BR-5 | `dotnet test backend --filter Br5` | 6 passed | Move `Status = GrantStatus.Closed;` above the check in `Grant.Close` → BR5-1 fails |
| BR-6 | `dotnet test backend --filter Br6` | 7 passed | `DueDate < today` → `<=` in `GrantTask.IsOverdue` → BR6-2 fails |
