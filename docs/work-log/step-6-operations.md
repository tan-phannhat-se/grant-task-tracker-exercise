# Work log · Step 6 · Operations needs · 2026-11-12 (done)
**Done:**
- Plan approved by tan: D57–D62 (key header, retry order, "same request", simultaneous retries, server unreachable, one key per form); tasks T6.1–T6.5 in `plan.md`
- T6.1 red: RT-1..RT-7 (8 runs); 6 fail for the right reason (201 instead of 200/400/422, 409 instead of 200); RT-2 and RT-6 already pass (today's code already does it), to be proved by breaking the code at green
- T6.2 red: HC-1, HC-2 fail with 404 (no `/health` route yet); tan writes the check
- T6.1 green: `Idempotency-Key` header checked at the endpoint (UUID only); `GrantService.AddTaskAsync` looks the key up first and returns `AddTaskResult` (task + `IsRetry`); `GrantTask.MatchesRequest`; new kind `RequestMismatch` → 422; two error codes
- T6.1 hand check on port 5099 with its own database file: 201 → 200 same id → 422 → 400 → 201 (no key); one task saved; only its own processes stopped
- T6.5 (part): README notes for OP-1, OP-2, OP-4, OP-6 listed below for step 7; hand check of the screen waits for T6.3, T6.4
- T6.2 green: tan wrote `DatabaseHealthCheck` (reads `Grants`; any exception → Unhealthy) and `/health` in `Program.cs`, in every environment; the AI reviewed it line by line, step by step
- Checked by hand before T6.3: with the API down, the Vite dev proxy answers 500 with an empty body; tan chose to make it answer 502 (D61), and `@types/node` was added for its types (D63, tan's choice)
- T6.3 red: NET-1..3, KEY-1 (6 runs fail: no key header, raw `TypeError`, "Something went wrong" for 502–504, no signal)
- T6.3 green: `send` adds a 15 s time limit and turns no answer or a 502/503/504 into `server-unreachable`; `addTask` takes the key; dev proxy answers 502 (checked by hand: 502); the form sends a new key per click for now (T6.4 keeps it across retries); FE-5 now checks only the method, as every request also carries a `signal`
- T6.4 (form) red: NET-4, KEY-2, KEY-3; KEY-2 fails (a new key per click); NET-4 and KEY-3 already pass, proved by breaking the code
- T6.4 (form) green: `AddTaskForm` keeps one key in state, makes a new one on any edit, sends it on every click; the screen part (NET-5) waits for step 5's `screenError` fix in the same files
- T6.4 (screen) red: NET-5 (grant list) and NET-6 (one grant, 503) fail with the general "Could not load" message; NET-6 added because it is a second place in the code
- T6.4 (screen) green: `messageFor(error, generalMessage)` shows the API client's message for loads and Complete
- Hand check in the browser, own API (5099) and own Vite (5198): API stopped → Add task shows "Cannot reach the server…" with values kept, POST 502 · grant change shows it at the top · API back → task added (201), error gone · logged keys: resend = same UUID, after an edit = new UUID; only own processes stopped

**Files touched:**
- `backend/src/GrantTracker.Api/`: `Domain/DomainErrorKind.cs` · `Domain/ErrorCodes.cs` · `Domain/GrantTask.cs` · `Services/AddTaskResult.cs` (new) · `Services/GrantService.cs` · `Endpoints/GrantEndpoints.cs` · `Endpoints/DomainRuleExceptionHandler.cs` (T6.1 green)
- `backend/tests/GrantTracker.Tests/Api/`: `AddTaskRetryTests.cs` (T6.1) · `HealthCheckTests.cs` (T6.2)
- `backend/src/GrantTracker.Api/`: `Data/DatabaseHealthCheck.cs` (new) · `Program.cs` (T6.2 green, written by tan)
- `docs/ai-log.md` (zsh hand-check entry; wrong commit advice entry; dead key reset entry)
- `frontend/`: `src/api/grantsApi.ts` + test · `vite.config.ts` · `package.json` + lock (`@types/node`) · `src/AddTaskForm.tsx` · `src/GrantTasksScreen.test.tsx` (FE-5) (T6.3)
- `frontend/src/`: `AddTaskForm.tsx` + test (T6.4 form) · `GrantTasksScreen.tsx` + test (T6.4 screen)
- `docs/plan.md` (Step 6 task table, D57–D63; T6.3, T6.4 rows widened; D62 wording) · `docs/work-log/step-6-operations.md`

### Review
| # | Task | Result |
|---|------|--------|
| 0c | T6.4 (form) green | 🔧 a new key after a success could not be caught by any break (the form is empty, so the next task is an edit anyway): line removed, D62 worded to match (ai-log) |
| 0 | T6.3 plan | 🔧 D61 assumed a proxy answers 502–504 when the API is down; running it showed Vite answers 500 → dev proxy now answers 502 (tan chose) |
| 0b | T6.3 green | ✅ diff re-read against D61: no automatic retry; message and code in one place (`serverUnreachable`); the form keeps its values on failure (unchanged). Note: `crypto.randomUUID` needs a secure page (localhost or https) |
| 1 | T6.1 hand check | 🔧 first run in zsh gave 400 for every call (argument not split); rerun in `bash`: all as expected (ai-log) |
| 2 | T6.1 red | ✅ each case maps to D57–D60 or a step 6 test note; no `field` expected (a header is not a form input) |
| 3 | T6.1 green | ✅ diff re-read against D57–D59: key looked up before the grant (D58); the 422 message never describes the other task; no new package; one type per file |
| 4 | T6.2 red | ✅ tests check only the status code, so tan is free to choose the body and the files |
| 5 | T6.2 green (tan's code) | 🔧 AI review: `<param>` blocks, a line over 120, `ex.Message` as the description (now a fixed text + the exception), empty `//` comments in `Program.cs` · ✅ no new package (health checks ship with ASP.NET Core 8) |

**Missing:** a failed reload after adding a task (`reloadGrant`) has no test of its own; it uses the same `messageFor`

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
| 5 | T6.4 (screen) | ✅ NET-5, NET-6 failed (general message) | ✅ 32 passed; `./test.sh` exit 0 (94 backend, 32 frontend) | ✅ 3 breaks, restore checked by hash: grant list general message → NET-5 · one grant general message → NET-6 · API message never used → NET-5, NET-6, FE-13 |
| 4 | T6.4 (form) | ✅ KEY-2 failed (new key per click); NET-4, KEY-3 passed | ✅ 10 passed; `tsc` ok | ✅ 4 breaks, restore checked by hash: new key per click → KEY-2 · key kept after an edit → KEY-3 · form cleared on failure → NET-4 · error without a field dropped → NET-4, FE-10 |
| 3 | T6.3 | ✅ 6 failed (see Done) | ✅ 9 passed; `./test.sh` exit 0 (94 backend, 24 frontend) | ✅ 4 breaks, restore checked by hash: no key header → KEY-1 · fetch error passed on → NET-1 · 502 left out → NET-2 (502) · no time limit → NET-3 |
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
| KEY-1 | `addTask` with a key → `Idempotency-Key` header | Key not sent |
| NET-1 | `fetch` fails → `ApiError` `server-unreachable`, status 0 | "Failed to fetch" shown, or swallowed |
| NET-2 | 502 / 503 / 504, empty body → same error | Proxy failure shown as "Something went wrong" |
| NET-3 | Every request carries an abort signal | No time limit: the screen waits forever |
| NET-4 | Form: server unreachable → message at the top of the form, values kept | Failure swallowed · form cleared |
| KEY-2 | Form: fail, click again → same key (a UUID); next task → new key | Retry adds a twin · one key forever |
| KEY-3 | Form: fail, edit the title, click again → new key | Corrected task refused as a reused key |
| NET-5 / NET-6 | Screen: grant list / one grant (503) cannot load → "Cannot reach the server…" at the top | General message hides that the server is down |

**Missing:** none (FE-12..16 belong to step 5, so step 6 uses NET- and KEY- ids)

**Test it yourself:**

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| Retries (T6.1) | `dotnet test backend --filter "FullyQualifiedName~AddTaskRetryTests"` | 8 passed | In `GrantTask.MatchesRequest` remove `GrantId == grantId &&` → RT-4 fails |
| Retries by hand | API running, then the same `curl -X POST …/grants/2/tasks -H "Idempotency-Key: <uuid>"` twice | 201, then 200 with the same `id` | Change the title in the second call → 422 `idempotency-key-reused` |
| API client (T6.3) | `(cd frontend && npx vitest run src/api/grantsApi.test.ts)` | 9 passed | In `grantsApi.ts` remove `502` from the list → NET-2 (502) fails |
| Form keys (T6.4) | `(cd frontend && npx vitest run src/AddTaskForm.test.tsx)` | 10 passed | In `AddTaskForm.tsx` send `crypto.randomUUID()` instead of `idempotencyKey` → KEY-2 fails |
| Screen messages (T6.4) | `(cd frontend && npx vitest run src/GrantTasksScreen.test.tsx)` | 9 passed | In `messageFor` return only `generalMessage` → NET-5, NET-6 fail |
| Whole OP-3 in the browser | API and `npm run dev` running; fill the form; stop the API; click Add task; start the API; click Add task again | "Cannot reach the server…", values kept; then one task added | — |
| Dev proxy, API down | Stop the API, keep `npm run dev`, then `curl -i http://localhost:5173/api/grants` | `502 Bad Gateway` | Remove `configure` in `vite.config.ts` → `500` |
| Health (T6.2) | `dotnet test backend --filter "FullyQualifiedName~HealthCheckTests"` | 2 passed | In `DatabaseHealthCheck` replace the `AnyAsync` line with `await _db.Database.CanConnectAsync(cancellationToken);` → HC-2 fails |
| Health by hand | API running, then `curl -i http://localhost:5080/health` | `200` · `Healthy` (also checked in Production on port 5099) | — |

### README notes for step 7 (OP-1, OP-2, OP-4, OP-6)
Facts come from the code, decisions or hand checks; "would" = not built.

| OP | Today | Before going live (would) |
|----|-------|---------------------------|
| Why OP-3, OP-5 | OP-3: a duplicate or a silent failure corrupts an officer's work every day in regional offices. OP-5: small, and the load balancer needs it to make OP-1 work | — |
| OP-1 copies, restarts | The API keeps no state in memory, so any copy can answer. A request cut by a restart is safe to send again (key, D7). `/health` lets the load balancer drop a copy. But SQLite is one file on one machine, and the database is created with `EnsureCreated` (D18) | One shared database server (SQL Server or PostgreSQL) for all copies; EF migrations instead of `EnsureCreated` |
| OP-2 concurrency | BR-5 can break: officer A closes a grant while officer B adds a High task. Two retries with the same key at once → 500, the next click → 200 (D60) | `Version` concurrency token on `Grant` (SQLite has no `rowversion`), changed by `AddTask` and `Close`; a conflict → 409 `concurrency-conflict`, the screen reloads (tan's choice: README only) |
| OP-4 support | Errors have a stable `code`; tasks keep `CreatedAt` (UTC). The error body has no request id (checked by hand: no `traceId`). No user identity, so no "who did what". .NET 8 logs handled 400/409 as Error (noisy, accepted) | Request id in every error body and log line, so an officer can quote it; logs in one central place; log in users and keep an audit trail |
| OP-6 reporting | Per-day counts are possible from the data: `CreatedAt` and `CompletedDate`. The overdue list and the pending count load every task of the filtered grants (D41, D55), so they slow down as data grows; a `DueDate` index exists (D37) | A small read-only stats endpoint or dashboard (created/completed per day); filter overdue tasks in SQL; record request duration per route (.NET metrics) and alert on the overdue list |
| Not in the list | No login: the key is for retries, not security. One time zone for "today" (D15). .NET 8 support ended 10 Nov 2026 (D9). SQLite file has no backup | Authentication with keys scoped per user; per-office time zone; move to .NET 10; backups |

