# Work log · Step 6 · Operations needs · 2026-11-12 (in progress)
**Done:**
- Plan approved by tan: D57–D62 (key header, retry order, "same request", simultaneous retries, server unreachable, one key per form); tasks T6.1–T6.5 in `plan.md`
- T6.1 red: RT-1..RT-7 (8 runs); 6 fail for the right reason (201 instead of 200/400/422, 409 instead of 200); RT-2 and RT-6 already pass (today's code already does it), to be proved by breaking the code at green
- T6.2 red: HC-1, HC-2 fail with 404 (no `/health` route yet); tan writes the check
- T6.1 green: `Idempotency-Key` header checked at the endpoint (UUID only); `GrantService.AddTaskAsync` looks the key up first and returns `AddTaskResult` (task + `IsRetry`); `GrantTask.MatchesRequest`; new kind `RequestMismatch` → 422; two error codes
- T6.1 hand check on port 5099 with its own database file: 201 → 200 same id → 422 → 400 → 201 (no key); one task saved; only its own processes stopped
- T6.2 green: tan wrote `DatabaseHealthCheck` (reads `Grants`; any exception → Unhealthy) and `/health` in `Program.cs`, in every environment; the AI reviewed it line by line, step by step

**Files touched:**
- `backend/src/GrantTracker.Api/`: `Domain/DomainErrorKind.cs` · `Domain/ErrorCodes.cs` · `Domain/GrantTask.cs` · `Services/AddTaskResult.cs` (new) · `Services/GrantService.cs` · `Endpoints/GrantEndpoints.cs` · `Endpoints/DomainRuleExceptionHandler.cs` (T6.1 green)
- `backend/tests/GrantTracker.Tests/Api/`: `AddTaskRetryTests.cs` (T6.1) · `HealthCheckTests.cs` (T6.2)
- `backend/src/GrantTracker.Api/`: `Data/DatabaseHealthCheck.cs` (new) · `Program.cs` (T6.2 green, written by tan)
- `docs/ai-log.md` (zsh hand-check entry; wrong commit advice entry)
- `docs/plan.md` (Step 6 task table, D57–D62) · `docs/work-log/step-6-operations.md`

### Review
| # | Task | Result |
|---|------|--------|
| 1 | T6.1 hand check | 🔧 first run in zsh gave 400 for every call (argument not split); rerun in `bash`: all as expected (ai-log) |
| 2 | T6.1 red | ✅ each case maps to D57–D60 or a step 6 test note; no `field` expected (a header is not a form input) |
| 3 | T6.1 green | ✅ diff re-read against D57–D59: key looked up before the grant (D58); the 422 message never describes the other task; no new package; one type per file |
| 4 | T6.2 red | ✅ tests check only the status code, so tan is free to choose the body and the files |
| 5 | T6.2 green (tan's code) | 🔧 AI review: `<param>` blocks, a line over 120, `ex.Message` as the description (now a fixed text + the exception), empty `//` comments in `Program.cs` · ✅ no new package (health checks ship with ASP.NET Core 8) |

**Missing:** T6.3–T6.5

**Review it yourself:**
1. `docs/plan.md` D57–D62: do they say what you approved?
2. `Services/GrantService.cs` `AddTaskAsync`: why is the key looked up before the grant is loaded?
3. `Domain/GrantTask.cs` `MatchesRequest`: why is the title trimmed there?
4. `AddTaskRetryTests.cs`: each "Catches" line names a different bug.
5. `Data/DatabaseHealthCheck.cs`: why read a row instead of `CanConnectAsync`? (break H2 below answers it)

### Test
| # | Task | Red | Green | Break on purpose → result |
|---|------|-----|-------|---------------------------|
| 1 | T6.1 | ✅ 6 of 8 failed (wrong status); RT-2, RT-6 pass | ✅ 8 passed; `./test.sh` stops only at HC-1, HC-2 (T6.2 red, tan's) | ✅ 8 breaks in a `bash` script, restore checked by hash: untrimmed title → RT-1 · grant not compared → RT-4 · title not compared → RT-5 · retry as 201 → RT-1, RT-7 · BR-1 before key → RT-7 · key mandatory → RT-2 (+7 AT) · length-only check → RT-3 · BR-1 skipped with a key → RT-6 |
| 2 | T6.2 | ✅ 2 failed (404) | ✅ 2 passed; `./test.sh` exit 0 (94 backend, 15 frontend) | ✅ 2 breaks, restore checked by hash: data never read → HC-2 · only `CanConnectAsync` → HC-2 (opening still works with the tables gone) |

**Test cases** (T6.1, T6.2; each catches a different bug)
| # | Scenario → expected | Catches |
|---|---------------------|---------|
| RT-1 | Same request and key twice (title with spaces) → 201, then 200 same id; one task | Duplicate · 201 on retry · title compared untrimmed |
| RT-2 | Same request twice, no key → 201, 201; two tasks | Key made mandatory · "no key" as one shared key |
| RT-3 | Key `not-a-uuid` or 36 `z`s with dashes → 400 `idempotency-key-invalid`; no task | Any text accepted · only the length checked |
| RT-4 | Key used on GR-100002, same body to GR-100001 → 422 `idempotency-key-reused`; no task there | Other grant's task returned · second task created |
| RT-5 | Key used, same grant, different title → 422 | Only the key compared |
| RT-6 | Same key twice to closed GR-100003 → 409 both; no task | Key stored although the add was refused |
| RT-7 | Add with key, close the grant, retry → 200 same task | BR-1 checked before the key |
| HC-1 | `GET /health`, database fine → 200 | Route missing · working copy reported unhealthy |
| HC-2 | Tables dropped after start → 503 | Check never touches the data |

**Missing:** T6.3, T6.4 test cases (NET-1..3, KEY-1, FE-12, FE-13) are written down at their red commit

**Test it yourself:**

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| Retries (T6.1) | `dotnet test backend --filter "FullyQualifiedName~AddTaskRetryTests"` | 8 passed | In `GrantTask.MatchesRequest` remove `GrantId == grantId &&` → RT-4 fails |
| Retries by hand | API running, then the same `curl -X POST …/grants/2/tasks -H "Idempotency-Key: <uuid>"` twice | 201, then 200 with the same `id` | Change the title in the second call → 422 `idempotency-key-reused` |
| Health (T6.2) | `dotnet test backend --filter "FullyQualifiedName~HealthCheckTests"` | 2 passed | In `DatabaseHealthCheck` replace the `AnyAsync` line with `await _db.Database.CanConnectAsync(cancellationToken);` → HC-2 fails |
| Health by hand | API running, then `curl -i http://localhost:5080/health` | `200` · `Healthy` (also checked in Production on port 5099) | — |
