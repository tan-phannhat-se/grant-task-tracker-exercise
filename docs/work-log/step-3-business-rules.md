# Work log · Step 3 · Business rules · 2026-11-11 (in progress)
**Done:** T3.1 red: 3 BR-1 tests · T3.1 green: BR-1 in `Grant.AddTask`, minimal `Grant.Close()` (sets Closed; BR-5 left for tan as a TODO).
**Files touched:**
- `backend/tests/GrantTracker.Tests/Domain/Br1ClosedGrantTests.cs`
- `backend/src/GrantTracker.Api/Domain/Grant.cs`

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

**Test it yourself:**
- [ ] `dotnet test backend` → 3 passed
- [ ] Comment out the `if (Status == GrantStatus.Closed)` check in `Grant.AddTask` → 2 tests fail → undo
