# Work log · Step 4 · API · 2026-11-12 (in progress)
**Done:**
- T4.1 red: 6 tests (CL-1, CL-2, DB-1..DB-4); stubs so they compile
- T4.1 green: length constants, `GrantTrackerDbContext` mapping, `ConfiguredTimeZoneCalendar.Today()`, `Program.cs` (DbContext, clock, calendar, database created on start)
- T4.2 red: 2 tests (SD-1, SD-2); `SeedData.Seed` stub throws; D39 and D40 recorded in `plan.md`
- T4.2 green: `SeedData.Seed` replays the brief through the domain; `Program.cs` seeds in Development only
- T4.3 red: PD-1..3 (handler stub throws); test API factory; AF-1..3 pass at once (code exists since T4.1/T4.2), proved by breaking the code; "services" and "enums as strings" moved to T4.4 (tan approved)
- T4.3 green: `DomainRuleExceptionHandler` (400 / 409 with `code`, `field` only when set); `Program.cs` registers it with ProblemDetails
- T4.4 red: GL-1..GL-5 (6 runs) fail with 404 because the route does not exist yet; no stubs needed; D41–D43 recorded
- T4.4 green: `Grant.PendingTaskCount`, `GrantService.ListGrantsAsync`, `GET /grants` with the status filter, enums as strings; D44 (bad input throws `DomainRuleException`, tan's choice)
- T4.5 red: GD-1..GD-3 fail (no route: 404 without a body); D46, D47 recorded
- T4.6 red: AT-1..AT-8 fail (no route yet); D48, D49 recorded
- T4.6 green: `AddTaskRequest`, `GrantService.AddTaskAsync` (today from the calendar, `CreatedAt` from `TimeProvider`), `POST /grants/{id:int}/tasks` → 201; `ThrowOnBadRequest = false`
- T4.7 red: CT-1..CT-3 fail (no route yet); test factory exposes its fake `Clock` so a test can move to the next day
- T4.7 green: `GrantTaskService.CompleteTaskAsync`, `TaskEndpoints` (handlers + `.Routes`), `PUT /tasks/{id:int}/complete` → 200; `task-not-found`
- T4.5 green: `NotFound` kind → 404, `GrantService.GetGrantAsync`, `GET /grants/{id:int}`, `GrantDetailResponse`, `TaskResponse` (overdue from the calendar's today)
- New test folders `tests/Services/` and `tests/Data/` (tan approved), added to the Structure block in `CLAUDE.md`
- Log template `docs/work-log/TEMPLATE.md`; step 2 and 3 logs completed; a `./test.sh` check for log parts built, then removed (tan: over-engineering)

**Files touched:**
- `backend/src/GrantTracker.Api/`: `Data/GrantTrackerDbContext.cs` · `Services/ConfiguredTimeZoneCalendar.cs` (`.gitkeep`s removed) · `Domain/Grant.cs` · `Domain/GrantTask.cs` (constants) · `Program.cs` · `appsettings.json`
- `backend/src/GrantTracker.Api/Data/SeedData.cs` · `Program.cs` (T4.2) · `Endpoints/DomainRuleExceptionHandler.cs` (T4.3; `.gitkeep` removed) · `Services/GrantService.cs` · `Endpoints/GrantEndpoints.cs` (handlers) + `.Routes.cs` + `.StatusFilter.cs` (one partial class, split at tan's request) · `Endpoints/Responses/GrantListItemResponse.cs` · `Domain/Grant.cs` · `Domain/ErrorCodes.cs` (T4.4) · `Domain/DomainErrorKind.cs` · `Endpoints/Responses/GrantDetailResponse.cs` · `TaskResponse.cs` (T4.5) · `Endpoints/Requests/AddTaskRequest.cs` · `Program.cs` · `ErrorCodes.cs` (T4.6) · `Services/GrantTaskService.cs` · `Endpoints/TaskEndpoints.cs` + `.Routes.cs` (T4.7)
- `backend/tests/GrantTracker.Tests/Api/`: `GrantTrackerApiFactory.cs` · `DomainRuleExceptionHandlerTests.cs` · `StartupTests.cs` (T4.3; `.gitkeep` removed) · `GrantListTests.cs` (T4.4) · `GrantDetailTests.cs` (T4.5) · `AddTaskTests.cs` (T4.6) · `CompleteTaskTests.cs` · `GrantTrackerApiFactory.cs` (`Clock`) (T4.7)
- `backend/tests/GrantTracker.Tests/`: `Data/GrantTrackerDbContextTests.cs` · `Data/SeedDataTests.cs` · `Services/ConfiguredTimeZoneCalendarTests.cs`
- `docs/plan.md` (D39–D49, task rows T4.2–T4.6, README notes) · `CLAUDE.md` (Structure: `Endpoints/Requests/`, `Endpoints/Responses/`)
- `CLAUDE.md` (Structure block; Docs map; log rule points to the template) · `docs/ai-log.md` (T4.1 and T4.5 entries)
- `docs/work-log/TEMPLATE.md` · `step-2-test-cases.md` · `step-3-business-rules.md` (Missing, Review it yourself)

### Test cases (T4.1–T4.7)
| # | Case | Catches |
|---|------|---------|
| CL-1 | UTC configured, 2020-01-15 23:30Z → today 15 Jan | Real clock used, or a fixed time zone |
| CL-2 | `Australia/Sydney`, same instant → today 16 Jan | Configured time zone ignored |
| DB-1 | Closed grant + completed and pending task, read back in a new context → every field equal, `CreatedAt` still UTC | A field not mapped; `CreatedAt` read back as Unspecified |
| DB-2 | Two grants with the same reference number → refused | Unique index missing |
| DB-3 | Two tasks with no key → saved; two with the same key → refused | Unique index missing; no key saved as `""` |
| DB-4 | Delete a grant that has a task → refused, task still there | Cascade delete |
| SD-1 | Seed with today = 2020-01-15 → the brief's 4 grants (reference, organisation, applied = today − N, status) | Wrong offset or sign, typo, missing grant, GR-100003 left open, not saved, real clock |
| CT-1 | Complete overdue "Verify bank details" → 200, completed today, not overdue, completed late, saved | Not saved; real clock; wrong status |
| CT-2 | Complete, clock +1 day, complete again → 200, first date kept | Repeat overwrites the date; repeat refused with 409 |
| CT-3 | Unknown task → 404 `task-not-found` | 500 |
| AT-1 / AT-2 | Valid task due today on GR-100002 → 201, no Location, title trimmed, listed by GET · `CreatedAt` = fake clock | Not saved; 200; real clock (today or `CreatedAt`) |
| AT-3 | Closed GR-100003 → 409 `grant-closed` | Domain error not reaching HTTP |
| AT-4..AT-6 | No `title` → 400 `title-required` · no `dueDate` → `due-date-required` · no `priority` → `priority-invalid` (each on its field) | `null` crashing (500) or slipping through |
| AT-7 / AT-8 | Unknown grant → 404 `grant-not-found` · broken JSON → 400 | 500 instead of 4xx |
| GD-1 | GR-100001 → fields + 3 tasks by due date: completed · overdue · not yet due; priority by name | Tasks not loaded; `isOverdue` wrong or real clock; wrong order |
| GD-2 | A task completed after its due date → `completedLate: true` | `completedLate` not mapped or always false |
| GD-3 | Unknown id → 404 `grant-not-found` | 500, or 200 with an empty body |
| GL-1 | `GET /grants` → 4 grants by reference, dates `yyyy-MM-dd`, status by name, pending 2, 2, 0, 0 | Completed counted as pending; status as a number; unstable order |
| GL-2 / GL-3 | `?status=Open` → 3 open grants · `?status=Closed` → GR-100003 | Filter ignored, inverted or fixed to Open |
| GL-4 | `?status=Foo` and `?status=100` → 400 `status-invalid` on `status` | Bad filter returns everything; a number bound to an enum |
| GL-5 | `?status=all` → every grant | `All` refused, or case-sensitive matching |
| PD-1 | `Validation` error with a field → 400, `code`, `field`, message as `detail` | Wrong status; code or field missing |
| PD-2 | `RuleRefused` error → 409, `code`, no `field` | Every error mapped to 400 |
| PD-3 | Any other exception → not handled (default 500) | A crash reported as a 400 that blames the user |
| AF-1 | Test API in Development with the fake clock → GR-100004 applied 10 Jan 2020 | Fixture not replacing the clock |
| AF-2 | API in Production → no grants | Sample data seeded outside Development |
| AF-3 | Unknown `App:TimeZone` in Production → start fails | Calendar created only on the first request |
| SD-2 | Each grant's tasks match the brief; Completed → completed on the due date, Pending → none | Task on the wrong grant, wrong priority or status, completed "today" (looks late) |

### Review
| # | Task | Result |
|---|------|--------|
| 1 | T4.7 green | ✅ Same shape as the grant endpoints (partial class, service, shared `TaskResponse`) · a task is loaded alone: completing needs nothing from its grant |
| 2 | T4.6 green | 🔧 AT-8 found broken JSON → 500 in Development (`ThrowOnBadRequest` makes the exception handler see it) → turned off, 400 everywhere · 🔧 D49 said "ProblemDetails"; the real 400 has an empty body → D49 corrected · ✅ tasks not loaded to add one |
| 3 | T4.5 green | 🔧 The first break-check script ran in zsh: backups failed silently and five mutations stayed in the code; found by the final file check, undone by hand, checks rerun with a `bash` script that stops on a failed backup · ✅ task order and overdue live in the response mapping; the domain decides overdue |
| 4 | T4.4 green | ✅ Endpoint only parses and maps; service only queries; "pending" defined once in `Grant` · `PendingTaskCount` is not a column (schema checked) · `?status=` (empty) counts as missing |
| 5 | T4.3 green | ✅ One place maps domain errors; other exceptions fall through to the default 500 without details · the HTTP pipeline wiring (`UseExceptionHandler`) has no endpoint to call yet: proved in T4.6 |
| 6 | T4.3 red | 🔧 AF-3 first ran in Development, where the seed creates the calendar anyway, so it could not catch a late calendar → moved to Production |
| 7 | T4.2 green | ✅ Only domain methods, no back door · GR-100003 closed after its High task is completed · seed only in Development (D39) · note: the in-memory database setup is repeated in the two `tests/Data/` files (left as is: the T4.1 file is not in this plan) |
| 8 | T4.1 green · red steps · schema | 🔧 "Real clock used" not caught: the test instant was today's real date → moved to 2020-01-15 (tan approved) · 🔧 `GrantTrackerDbContext` constructor had no summary · ✅ Fixed dates only; each test gets its own in-memory database; each summary has a "Catches" line · schema printed from the model: unique `ReferenceNumber` · unique `IdempotencyKey` where not null · `GrantId` · `DueDate` · `ON DELETE RESTRICT` · enums as `INTEGER` |

**Missing:** column lengths are not tested (SQLite does not enforce them; declared only) · the "where not null" filter on the key index cannot be proved (SQLite already allows many NULLs in a unique index).

**Review it yourself:**
1. `git diff`: only the files above changed.
2. `Program.cs`: the calendar is created at start-up, so a bad `App:TimeZone` stops the app at once.
3. `GrantTrackerDbContext.cs`: each line maps to one length, index, delete rule or converter you decided.
4. `ConfiguredTimeZoneCalendar.Today()`: reads only the injected clock, never `DateTime.Now`.

### Test
| # | Task | Red | Green | Break on purpose → result (all restored) |
|---|------|-----|-------|------------------------------------------|
| 1 | T4.7 | 3/3 failed (no route: 404; CT-3 has no body) | 76/76 | Not saved → CT-1, CT-2 · date from the real clock → CT-1, CT-2 · repeat guard removed in `GrantTask.Complete` → CT-2 · missing task → 500 → CT-3 |
| 2 | T4.6 | 8/8 failed (no route: 404; AT-2 finds no task, AT-7 has no body) | 73/73 | Not saved → AT-1, AT-2 · Location sent → AT-1 · `CreatedAt` real clock → AT-2 · today real clock → AT-1, AT-2 · missing date → default → AT-5 · `ThrowOnBadRequest` on → AT-8 · missing priority → 0: nothing red (the domain refuses 0 with the same code and field) → the endpoint check was redundant and is removed (tan's choice) |
| 3 | T4.5 | 3/3 failed (no route: 404, GD-3 has no body to read `code` from) | 65/65 | No `Include` → GD-1, GD-2 · ordered by title → GD-1 · overdue from the real clock → GD-1 · `completedLate` always false → GD-2 · `NotFound` not mapped → GD-3 |
| 4 | T4.4 | 6/6 runs failed (404: no route yet) | 62/62 | Count every task → GL-1 · filter ignored → GL-2, GL-3 · order by organisation → GL-1, GL-2, GL-5 · enums as numbers → GL-1 · enum parse accepts "100" → GL-4 · case-sensitive → GL-2, GL-3 · `UseExceptionHandler` removed → GL-4 |
| 5 | T4.3 | PD-1..3 failed (`NotImplementedException`); AF-1..3 passed (code already there) | 56/56 | Validation → 409 → PD-1 red · RuleRefused → 400 → PD-2 red · field dropped → PD-1 red · field always written → PD-2 red · every exception handled → PD-3 red · fixture keeps the real clock → AF-1 red · seed in every environment → AF-2 red · calendar created lazily → AF-3 red |
| 6 | T4.2 | 2/2 failed (`NotImplementedException`) | 50/50 | Wrong priority → SD-2 red · completed a day late → SD-2 red · GR-100003 not closed → SD-1 red · real clock → both red · not saved → both red · offset −5 → −6 → SD-1 red |
| 7 | T4.1 | 6/6 failed (2 `NotImplementedException`, 4 on asserts) | 48/48 | Zone ignored → CL-2 red · no UTC converter → DB-1 red · reference not unique → DB-2 red · key not unique → DB-3 red · cascade → DB-4 red · real clock → CL-1, CL-2 red · Sydney hard-coded → CL-1 red |
| 8 | Start-up and API (by hand) | — | ✅ | `PUT /tasks/1/complete` twice → 200, same date both times · `/tasks/9999/complete` → 404 `task-not-found` · `POST /grants/2/tasks` → 201, no Location · `/grants/3/tasks` → 409 `grant-closed` · bad date, broken JSON → 400, empty body · `curl /grants/1` → 3 tasks, Verify bank details overdue · `/grants/9999` → 404 `grant-not-found` · `/grants/abc` → 404 with no body (route constraint, accepted) · `curl /grants?status=closed` → GR-100003 · `?status=Foo` → 400 ProblemDetails with `code`, `field` · Swagger shows `status` as `Open`/`Closed` · Development: 4 grants, 6 tasks, dates from today, `CreatedAt` = application date 00:00 UTC · Production: no seed · Production restart keeps a row; Development restart empties it · `App__TimeZone=Mars/Olympus` → `TimeZoneNotFoundException`, app stops |

**Missing:** seeded `CreatedAt` (D40) has no automated test; checked by hand. The handler is now reached through HTTP (GL-4). The bad time zone and "seed only in Development" are now AF-3 and AF-2.

**Test it yourself:** `./test.sh` → all green; `dotnet test backend` → 76 passed, then each group:

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| Calendar | `dotnet test backend --filter ConfiguredTimeZoneCalendarTests` | 2 passed | `_timeProvider.GetUtcNow()` → `DateTimeOffset.UtcNow` → CL-1 and CL-2 fail |
| DbContext | `dotnet test backend --filter GrantTrackerDbContextTests` | 4 passed | `DeleteBehavior.Restrict` → `Cascade` → DB-4 fails |
| Complete task | `dotnet test backend --filter CompleteTaskTests` | 3 passed | Remove `if (IsCompleted) { return; }` in `GrantTask.Complete` → CT-2 fails |
| Add task | `dotnet test backend --filter AddTaskTests` | 8 passed | Remove `await _db.SaveChangesAsync();` in `AddTaskAsync` → AT-1, AT-2 fail |
| Grant detail | `dotnet test backend --filter GrantDetailTests` | 3 passed | `.OrderBy(t => t.DueDate)` → `t.Title` in `GrantDetailResponse` → GD-1 fails |
| Grant list | `dotnet test backend --filter GrantListTests` | 6 passed | `_tasks.Count(t => !t.IsCompleted)` → `_tasks.Count` in `Grant` → GL-1 fails |
| Errors | `dotnet test backend --filter DomainRuleExceptionHandlerTests` | 3 passed | `Validation => …400BadRequest` → `409Conflict` → PD-1 fails |
| Start-up | `dotnet test backend --filter StartupTests` | 3 passed | Add `SeedData.Seed(db, calendar.Today());` in the `else` of `Program.cs` → AF-2 fails |
| Seed | `dotnet test backend --filter SeedDataTests` | 2 passed | `harbour.Close();` removed in `SeedData.Seed` → SD-1 fails |
| Start-up by hand | `App__TimeZone=Mars/Olympus dotnet run --project backend/src/GrantTracker.Api` | App stops with `TimeZoneNotFoundException` | — |
