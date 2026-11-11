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
