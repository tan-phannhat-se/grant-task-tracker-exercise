# Work log · Step 2 · Test cases · 2026-11-11 (done; T2.4 moved into step 3 as one task per rule, D29)
**Done:**
- code style D19 (`.editorconfig`, `dotnet format`, Prettier, format checks in `test.sh`)
- T2.1 by tan: 29 domain test cases (BR-1 3, BR-2 5, BR-3 5, BR-4 4, BR-5 6, BR-6 6), decisions D20–D26 and the step 2 task table in `plan.md`
- T2.2–T2.3: domain skeleton (`Grant`, `GrantTask`, `Priority`, `GrantStatus`, `DomainErrorKind`, `DomainRuleException`, `ErrorCodes`, one file each) with D27–D28; rule methods throw `NotImplementedException` until step 3
- test helpers (`TestData`)

**Files touched:**
- `docs/plan.md`
- `CLAUDE.md`
- `test.sh`
- `.editorconfig`
- `backend/src/GrantTracker.Api/Domain/`
- `backend/tests/GrantTracker.Tests/`
- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/.prettierrc.json`
- `frontend/.prettierignore`
- `frontend/tsconfig.json` (reformatted)

### Review
| # | Check | Result |
|---|-------|--------|
| 0 | Code is clean and easy to read | 🔧 over-commented → short three-line summaries, no Dxx in code, one type per file, max 120 chars per line |
| 1 | Code follows one style (D19) | 🔧 none before → `.editorconfig` + formatters; `TestData.cs` brought back to .NET style |
| 2 | Only files in the approved plans | ✅ (`ai-log.md` "Accepted" was tan's earlier request) |
| 3 | Domain has no clock, EF Core or HTTP | ✅ "today" and the UTC time are parameters |
| 4 | Enums explicit and gapped, no 0 (D5, D17) | ✅ 100 / 200 / 300 |
| 5 | Tasks only created through `Grant.AddTask` | ✅ `GrantTask` constructor is `internal` |
| 6 | `Field` on the exception and one `ErrorCodes` list | ✅ tan kept `Field` (now part of D27) |

**Missing:** none recorded when this log was written (line added on 2026-11-12 so every log has every part).

**Review it yourself:**
- [ ] `Grant.cs`, `GrantTask.cs`: signatures match how you think about the rules
- [ ] `DomainRuleException.cs`: `Code`, `Kind`, `Field` (D27)
- [ ] `.editorconfig` and `.prettierrc.json`: the style you want

### Test
| # | Check | Result |
|---|-------|--------|
| 1 | `./test.sh` (format, build, tests, type-check) | ✅ |
| 2 | Brace moved onto the same line in a C# file → `./test.sh` fails | ✅ exit 2, restored |
| 3 | Bad spacing in `App.tsx` → `./test.sh` fails | ✅ exit 1, restored |
| 4 | 133-character line in a C# file → `./test.sh` fails | ✅ exit 1, restored |

**Missing:** no tests yet; step 3 adds them rule by rule (red commit, then green commit).

**Test it yourself:**
- [ ] `./test.sh` passes
- [ ] Put a `{` on the same line in any `.cs` file → `./test.sh` fails → undo
