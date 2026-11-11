# AI work log

AI's record of each finished step, for tan to review. Mistakes also go to `ai-log.md`.

**Format:** Done · Files touched · Review (checklist, missing, how to review) · Test (checklist, missing, how to test).
Marks: ✅ pass · 🔧 found and fixed · ⏳ not yet. Guides go broad → narrow.
**Limits:** one entry per step (update it while the step runs) · about 40 lines per entry · tables up to 8 rows, 🔧 and ⏳ first, small ✅ grouped · files by folder · up to 4 self-check items.

---

## Step 1 · Design and scaffold · 2026-11-11
**Done:** git repo + private GitHub remote · .NET 8 API and xUnit tests · React/Vite frontend · `test.sh` · agent setup (`CLAUDE.md`, `.claude/settings.json`, `plan.md` D1–D18, logs).
**Files touched:** root config and `test.sh` · `.claude/` · `backend/src`, `backend/tests` · `frontend/` · `docs/`.

### Review
| # | Check | Result |
|---|-------|--------|
| 1 | Structure matches `CLAUDE.md` | 🔧 layer folders were missing → added |
| 2 | Builds with any .NET 8 SDK | 🔧 `global.json` 8.0.133 → 8.0.100 |
| 3 | Frontend type-checks | 🔧 removed `process.env` |
| 4 | `.md` files consistent | 🔧 duplicates removed, stale "open questions" fixed, Docs map added |
| 5 | Scope: no business code, demo code removed, only agreed packages (+ Swagger D11) | ✅ |
| 6 | Safety: commit/push denied for the AI · no build output or secrets · Swagger dev-only | ✅ |
| 7 | Decisions D1–D18 recorded; lengths, indexes, cascade, seeding left for step 4 | ✅ |

**Missing:** README prerequisites (.NET 8 SDK, Node 20.19+) · port 5080 set in 2 files · remove `.gitkeep` once folders have code · `tan's comment` in `ai-log.md` (tan).

**Review it yourself:**
- [ ] `plan.md`: Roles, Steps, D1–D18 match what you decided
- [ ] `CLAUDE.md`: the rules say what you want
- [ ] `.claude/settings.json`: commit and push are denied
- [ ] You can explain every line of `Program.cs`, `vite.config.ts`, `test.sh`

### Test
| # | Check | Result |
|---|-------|--------|
| 1 | `./test.sh`, lint, build | ✅ (no tests yet) |
| 2 | Planted type error → `./test.sh` fails | ✅ exit 2 |
| 3 | API `/swagger` and frontend dev page | ✅ 200, 200 |
| 4 | Fresh clone via README | ⏳ step 8 |

**Missing:** no real tests yet · remove `--passWithNoTests` at the first frontend test · tested only on this Mac.

**Test it yourself:**
- [ ] `./test.sh` passes
- [ ] `dotnet run --project backend/src/GrantTracker.Api` → http://localhost:5080/swagger opens
- [ ] `cd frontend && npm run dev` → http://localhost:5173 shows the title
- [ ] Add `const x: number = "a"` to `App.tsx` → `./test.sh` fails → remove it

---

## Step 2 · Test cases · 2026-11-11 (in progress: T2.1, T2.2, T2.3, code style done; T2.4 next)
**Done:** code style D19 (`.editorconfig`, `dotnet format`, Prettier, format checks in `test.sh`) · T2.1 by tan: 29 domain test cases (BR-1 3, BR-2 5, BR-3 5, BR-4 4, BR-5 6, BR-6 6), decisions D20–D26 and the step 2 task table in `plan.md` · T2.2–T2.3: domain skeleton (`Grant`, `GrantTask`, `Priority`, `GrantStatus`, `DomainErrorKind`, `DomainRuleException`, `ErrorCodes`, one file each) with D27–D28; rule methods throw `NotImplementedException` until step 3 · test helpers (`TestData`).
**Files touched:** `docs/plan.md`, `CLAUDE.md`, `test.sh`, `.editorconfig` · `backend/src/GrantTracker.Api/Domain/` · `backend/tests/GrantTracker.Tests/` · `frontend/` (`package.json`, lock file, `.prettierrc.json`, `.prettierignore`, `tsconfig.json` reformatted).

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

**Missing:** no tests yet; T2.4 adds them (red until step 3).

**Test it yourself:**
- [ ] `./test.sh` passes
- [ ] Put a `{` on the same line in any `.cs` file → `./test.sh` fails → undo
