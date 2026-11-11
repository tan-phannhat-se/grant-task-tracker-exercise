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

**Files touched:**
- `backend/tests/GrantTracker.Tests/Domain/Br1ClosedGrantTests.cs`
- `backend/src/GrantTracker.Api/Domain/Grant.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br3TitleTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br2DueDateTests.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br4CompleteTests.cs`
- `backend/src/GrantTracker.Api/Domain/GrantTask.cs`
- `backend/tests/GrantTracker.Tests/Domain/Br5CloseGrantTests.cs`

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

**Test it yourself:**
- [ ] `dotnet test backend` → 18 passed
- [ ] Comment out the `if (Status == GrantStatus.Closed)` check in `Grant.AddTask` → 2 tests fail → undo
- [ ] `dotnet test backend --filter Br3` → 6 passed
- [ ] Change `>` to `>=` in the BR-3 length check → BR3-2 fails → undo
- [ ] `dotnet test backend --filter Br2` → 5 passed
