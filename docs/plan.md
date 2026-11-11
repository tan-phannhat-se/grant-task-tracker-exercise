# Plan

## Scope
One screen for working on a grant's tasks, backed by a small API.

- **Endpoints:** list grants (optional status filter, pending task count), get one grant with its tasks, add a task, complete a task, close a grant, list overdue tasks (most overdue first).
- **Business rules:**
  - BR-1: no task on a closed grant.
  - BR-2: due date not in the past and not before the application date.
  - BR-3: title required, at most 100 characters.
  - BR-4: completing records the date; completing again keeps that date.
  - BR-5: no closing while a High-priority task is pending.
  - BR-6: overdue = not completed and due date before today. "Today" must be testable.
- **Operations needs built:** OP-3 (safe retries, clear message when the server is unreachable) and OP-5 (health check). OP-1, OP-2, OP-4 and OP-6 are explained in the README.
- **Seed data:** the four grants from the brief, dates relative to start-up.

## Roles
| tan | AI |
|-----|----|
| Ideas, design, decisions | Implementation plan |
| Approves the plan | Implements the approved plan |
| Reviews code and logs, asks "why" | Self-review, tests, logs |
| Commits and pushes | — |
| Writes BR-5, `AI_USAGE.md` and the OP-5 `/health` check | Writes the BR-5 tests; reviews those three |

## Steps
| # | Step | tan decides | AI implements | tan reviews | Done when |
|---|------|-------------|---------------|-------------|-----------|
| 1 | Design and scaffold | Data model, rules for the AI, stack | Repo, `CLAUDE.md`, plan, `test.sh`, logs | Structure and rules against the brief | First commit |
| 2 | Test cases | Test case list for each BR | Test skeleton and test code | Each test catches a different bug | Case list approved |
| 3 | Business rules | Any new question found while building | BR-1..4, BR-6 and unit tests · tan writes BR-5 | Rule ↔ test mapping; break the code, watch tests fail | Tests green |
| 4 | API | Routes, response shapes, column lengths, indexes, cascade, seeding | DbContext, seed, endpoints, ProblemDetails, integration tests | Call each endpoint in Swagger | Swagger works |
| 5 | Frontend | Screen layout, how errors show | Screen and tests | Try it in the browser, ask "why" | Works end to end |
| 6 | Operations needs | Which OPs and how | OP-3 and tests · tan writes OP-5 `/health` | Each OP has a test | One test per OP |
| 7 | Docs | Key points, assumptions, the AI story | README and TESTING.md drafts · tan writes `AI_USAGE.md` | Everything true and accurate | All three written |
| 8 | Submit | — | — | Fresh clone, follow README, push, grant access, send link | Link sent |

## Tasks
Filled at the start of each step: one table per step, one commit per task.

### Step 2 · Test cases
| Task | Work | Builds | Files |
|------|------|--------|-------|
| T2.1 | Test case list for each BR | tan (AI reviews) | `docs/plan.md` |
| T2.2 | Domain skeleton: `Grant`, `GrantTask`, enums, errors; no logic yet | AI | `backend/src/GrantTracker.Api/Domain/` |
| T2.3 | Test helpers: fixed dates, sample grants and tasks | AI | `backend/tests/GrantTracker.Tests/Domain/` |
| ~~T2.4~~ | Moved into step 3: each rule now ships its tests and code together (D29) | — | — |

**T2.1 · Domain test cases** (38: 29 from T2.1, 8 added in T3.7 after the independent review, 1 added in T3.8; each catches a different bug)

#### BR-1 · No task on a closed grant (4)
| # | Case | Catches |
|---|------|---------|
| BR1-1 | Open grant + valid task → task added | Rule blocks open grants too |
| BR1-2 | Closed grant + valid task → `grant-closed` (409), task count unchanged | BR-1 missing, or task added despite the error |
| BR1-3 | Closed grant + empty title → `grant-closed`, not `title-required` | Wrong check order (D20) |
| BR1-4 | Closed grant + due yesterday → `grant-closed`, not `due-date-in-past` | Due-date check running before BR-1 |

#### BR-2 · Due date not in the past, not before the application date (6)
| # | Case | Catches |
|---|------|---------|
| BR2-1 | Grant applied in the past, due yesterday → `due-date-in-past`, Field `dueDate` | Condition 1 (not in the past) missing |
| BR2-2 | Grant applied in the past, due today → task added | `<=` instead of `<` (boundary) |
| BR2-3 | Grant applies on 20 Nov, due 15 Nov → `due-date-before-application` | Condition 2 (not before application) missing |
| BR2-4 | Grant applies on 20 Nov, due 20 Nov → task added | Boundary of condition 2 |
| BR2-5 | Grant applies on 20 Nov, due yesterday → `due-date-before-application`, message names 20 Nov | Error reports the wrong condition (D21) |
| BR2-6 | Grant applying today, due yesterday → `due-date-in-past` | Wrong code when today and the application date are the same day |

#### BR-3 · Title required, at most 100 characters (6)
| # | Case | Catches |
|---|------|---------|
| BR3-1 | Title `""` or spaces only → `title-required`, Field `title` (one `[Theory]`, 2 inputs) | Empty check missing · not trimmed |
| BR3-2 | Exactly 100 characters → task added | `>=` instead of `>` (boundary) |
| BR3-3 | 101 characters → `title-too-long` | Length check missing |
| BR3-4 | `"  Check docs  "` → task added, saved as `"Check docs"` | Checked trimmed but saved untrimmed |
| BR3-5 | 100 characters with 2 spaces on each side → task added | Length measured before trimming |
| BR3-6 | Empty title + due yesterday → `title-required`, not a due-date error | Due-date check running before BR-3 |

#### BR-4 · Completing records the date; completing again keeps it (5)
| # | Case | Catches |
|---|------|---------|
| BR4-1 | Pending task, completed on 11 Nov → `CompletedDate` = 11 Nov | Date not recorded |
| BR4-2 | Completed on 11 Nov, completed again on 12 Nov → still 11 Nov, no error | Date overwritten, or an error on repeat (D12) |
| BR4-3 | Task due 8 Nov, completed on 11 Nov → `CompletedDate` = 11 Nov, `DueDate` still 8 Nov | Due date "fixed" to look on time |
| BR4-4 | Task on a closed grant, completed → allowed | Nothing yet (a task does not know its grant); guards D24 if that link is added |
| BR4-5 | Completed on 11 Nov, completed again with 10 Nov → still 11 Nov | First date overwritten when the new date is earlier |

#### BR-5 · No closing while a High-priority task is pending (6)
| # | Case | Catches |
|---|------|---------|
| BR5-1 | One pending High task → `pending-high-priority-tasks` (409), grant stays Open, message names the task | BR-5 missing, or grant closed despite the error |
| BR5-2 | Two pending High tasks → refused, message names both | Only the first blocking task listed |
| BR5-3 | Only Low and Medium tasks pending → grant closed | Any pending task blocks (too strict) |
| BR5-4 | High task completed + Low task pending → grant closed | Priority checked without checking completion |
| BR5-5 | Grant with no tasks → grant closed | Crash on an empty list |
| BR5-6 | Already closed grant, closed again → no error, still Closed | Error on repeat (D13) |

#### BR-6 · Overdue = not completed and due before today (8)
| # | Case | Catches |
|---|------|---------|
| BR6-1 | Pending, due yesterday → overdue | BR-6 missing |
| BR6-2 | Pending, due today → not overdue | `<=` instead of `<` (boundary) |
| BR6-3 | Due 10 Nov, completed on 7 Nov or 11 Nov → not overdue (one `[Theory]`, 2 inputs) | Completion not checked · "completed late" mistaken for overdue |
| BR6-4 | Same task due 12 Nov: checked with today 11 Nov → no, 13 Nov → yes | Real clock or stored state used instead of `today` |
| BR6-5 | Due 10 Nov, completed 11 Nov → `IsCompletedLate` true, `IsOverdue` false | Late and overdue mixed up (D26) |
| BR6-6 | Due 10 Nov, completed 10 Nov → `IsCompletedLate` false | `>=` instead of `>` (on the due date is on time) |
| BR6-7 | Due 10 Nov, completed 7 Nov → `IsCompletedLate` false | "Late" true for any completion not on the due date |
| BR6-8 | Pending, due yesterday → `IsCompletedLate` false, `IsOverdue` true | A pending task flagged as completed late |

#### Stored fields · `AddTask` stores what it is given (2, added in T3.7)
| # | Case | Catches |
|---|------|---------|
| ST-1 | Idempotency key passed in → stored on the task | Key dropped, breaking safe retries (OP-3) |
| ST-2 | Creation time passed in → stored as `CreatedAt` | Real clock used instead of the given time |

#### Priority · only Low, Medium or High (1, added in T3.8)
| # | Case | Catches |
|---|------|---------|
| PR-1 | Priority 0, 150 or 999 → `priority-invalid` (400) on `priority`, no task added (`[Theory]`, 3 inputs) | Undefined priority accepted (BR-5 would never count it as High), or a range check that lets 150 through |

### Step 3 · Business rules
Each task is one rule, done test-first in two commits: `test(T3.x)` adds failing tests (red), `feat(T3.x)` makes them pass (green).

| Task | Rule | Code by | Files |
|------|------|---------|-------|
| T3.1 | BR-1 closed grant (3 cases) | AI | `Domain/Grant.cs` · `tests/Domain/Br1ClosedGrantTests.cs` |
| T3.2 | BR-3 title (5 cases) | AI | `Domain/Grant.cs` · `tests/Domain/Br3TitleTests.cs` |
| T3.3 | BR-2 due date (5 cases) | AI | `Domain/Grant.cs` · `tests/Domain/Br2DueDateTests.cs` |
| T3.4 | BR-4 complete (4 cases) | AI | `Domain/GrantTask.cs` · `tests/Domain/Br4CompleteTests.cs` |
| T3.5 | BR-5 close grant (6 cases) | **tan** (AI writes the tests) | `Domain/Grant.cs` · `tests/Domain/Br5CloseGrantTests.cs` |
| T3.6 | BR-6 overdue and completed late (6 cases) | AI | `Domain/GrantTask.cs` · `tests/Domain/Br6OverdueTests.cs` |
| T3.7 | Independent review of all six rules by three reviewer subagents (logic, mutations, consistency with brief and plan); gaps closed with 8 tests | AI | domain tests · `Domain/Grant.cs` · docs |
| T3.8 | Undefined priority refused in the domain (D30) | AI | `Domain/Grant.cs` · `Domain/ErrorCodes.cs` · `tests/Domain/PriorityTests.cs` |

T3.1 → T3.2 → T3.3 run in this order (D20) and one after another, because all three change `Grant.AddTask`. T3.4 and T3.6 only touch `GrantTask.cs` and their own test files.

### Step 4 · API
Each endpoint is one task, done test-first like step 3: a red commit with its integration tests, then a green commit. tan approves each endpoint's test case list first.

| Task | Work | Code by | Files |
|------|------|---------|-------|
| T4.1 | DbContext and EF configuration (D36–D38); database created on start (D18); "today" from the configured time zone (D15) | AI | `Data/` · `Services/` · `Program.cs` |
| T4.2 | Seed data (D33, D39, D40), with tests that it matches the brief's table | AI | `Data/` · `Program.cs` · `tests/Data/` |
| T4.3 | API foundation: domain errors → ProblemDetails (D27), integration test fixture (WebApplicationFactory, SQLite in-memory, FakeTimeProvider), start-up tests (seed only in Development, bad time zone) | AI | `Program.cs` · `Endpoints/` · `tests/Api/` |
| T4.4 | `GET /grants` (status filter, pending task count); first service; enums as strings (moved from T4.3: first response that shows them) | AI | `Endpoints/` · `Services/` · `tests/Api/` |
| T4.5 | `GET /grants/{id}` | AI | same |
| T4.6 | `POST /grants/{id}/tasks` (201; missing fields handled at the boundary, D23) | AI | same |
| T4.7 | `PUT /tasks/{id}/complete` | AI | same |
| T4.8 | `PUT /grants/{id}/close` (loads the grant with its tasks) | AI | same |
| T4.9 | `GET /tasks/overdue` (filter D14, most overdue first, `daysOverdue`) | AI | same |

The idempotency-key retry (200 with the existing task) is built in step 6 (OP-3).

**Test notes for later steps**
- Step 4 (API): `POST` a task to GR-100003 (closed in the seed) → 409 with `code: grant-closed`.
- Step 4 (API): `POST` a task with no `title` in the JSON → 400 `title-required` (null is handled at the API boundary, D23).
- Step 6 (OP-3): retry with the same idempotency key on a closed grant → still 409, no task created.
- Step 5 (screen): after Complete, show "Completed on {date}" from the response, so a repeat shows the original date.
- Step 4 (API): task responses include `completedLate` (D26).
- Step 4 (API): load a grant together with its tasks (`Include`) before `Close()`; otherwise BR-5 sees no tasks and lets the close through.
- Step 6 (OP-3): a key that already exists for a different grant or a different request → 422 `idempotency-key-reused`, never the other task.
- Step 6 (OP-3): accept only a 36-character UUID as the key (D36); a missing key is allowed (D7) and just means no duplicate protection.
- Step 7 (README, security / before going live): the idempotency key is for safe retries, not security; add authentication and scope keys per user before going live.
- Step 5 (screen): a task completed late shows a "Completed late" label (D26).

## Decisions
| # | Decision | Why |
|---|----------|-----|
| D1 | Two tables, `Grants` and `GrantTasks`; the C# class is `GrantTask` | A grant has many tasks. `Task` would clash with `System.Threading.Tasks.Task`. |
| D2 | `Grant.Status` is an enum (Open, Closed) | Room for more states later, e.g. Withdrawn. |
| D3 | A task's completion is one nullable `CompletedDate` | One source of truth: "completed" and "completed on" can never disagree. BR-4 becomes "if it already has a date, keep it". |
| D4 | Business dates are `DateOnly`; `CreatedAt` is a UTC `DateTime` | The rules compare days, not times. With `DateTime`, a task due today (00:00) would look past at 10:00 and BR-2 would wrongly refuse it. |
| D5 | `Priority` stored as int with explicit gapped values: Low = 100, Medium = 200, High = 300 | Sorting by priority works in SQL, and a value can be inserted later (e.g. 250) without shifting old data. No 0, so a missing priority is caught. Trade-off: the database shows 300, not "High", so the README lists the values for support. |
| D6 | `GrantTasks.CreatedAt` | Lets the team count tasks created per day (OP-6). |
| D7 | `GrantTasks.IdempotencyKey`: nullable, unique | A retried "add task" with the same key returns the existing task instead of a duplicate (OP-3). Stored on the task row so the task and the key are saved in one transaction; the unique index also stops two simultaneous retries. Redis was considered: it needs extra setup and two separate writes that can disagree after a crash. |
| D8 | Keep an int `Id` and a separate idempotency key (not a client-generated GUID id) | The id identifies the task and lives forever; the key identifies one request and can expire. The server stays in charge of ids. |
| D9 | .NET 8 | tan's choice. Note: .NET 8 support ends on 10 Nov 2026; mention in the README. |
| D10 | One API project with Domain / Services / Data / Endpoints folders | Small app; folders and dependency direction give the separation the brief asks for without extra projects. Split into projects if it grows. (Proposed by the AI, confirmed by tan.) |
| D11 | Swagger / OpenAPI (Swashbuckle), Development only | Listed as an optional extra in the brief. Added at scaffold time (tan's call) so every endpoint can be tried from the browser while building and in the demo, without curl. Not exposed outside Development. |
| D12 | Completing a completed task → `200`, date unchanged | Clients care whether it's done, not how many times they called. Retries stay safe (OP-3). |
| D13 | Closing a closed grant → `200`, unchanged | Same reason as D12. |
| D14 | `GET /tasks/overdue?grantStatus=All\|Open\|Closed`, default `Open` | The brief is silent on closed grants. A filter keeps every task reachable, and the default shows only work officers can still act on. |
| D15 | "Today" = one configurable time zone (default UTC; `Australia/Sydney` when live) | One API serves every office, so it can't be per office. Per-office time zone header noted as a next step. |
| D16 | Errors: 400 bad input · 404 not found · 409 rule refused; ProblemDetails with a `code` | 400 = fix your input. 409 = the current state forbids it. |
| D17 | `Grant.Status` stored as gapped ints: Open = 100, Closed = 200 | Same rule as `Priority` (D5): one convention for every enum. |
| D18 | `EnsureCreated`; Development recreates and reseeds on start; never deletes elsewhere | Seed dates stay relative to today. OP-1: no data loss on restart. Migrations before going live. |
| D19 | Code style: C# uses the standard .NET style (4 spaces, braces on their own line) via `.editorconfig` + `dotnet format`; the frontend uses Prettier. `./test.sh` fails on badly formatted code | tan chose the .NET default style: what C# reviewers expect. A checked format keeps AI-written and hand-written code identical. |
| D20 | `AddTask` checks BR-1 (closed grant) before BR-3 (title) and BR-2 (due date) | When the grant is closed, fixing the input cannot help, so that error comes first. |
| D21 | A due date that is too early is checked against the earliest allowed date = the later of today and the application date; the error names that date (`due-date-in-past` when it is today, `due-date-before-application` otherwise) | One message tells the officer the date that will work, so one fix is enough. Reporting "in the past" when the application date is later would send them to a date that still fails. |
| D22 | Titles are trimmed at both ends before BR-3 is checked, and the trimmed title is saved; whitespace inside the title is kept (trimming removes spaces, tabs and line breaks at both ends) | What is checked is what is saved, so a saved title is always 1–100 characters. Leading/trailing spaces are almost always accidental; inner spaces are the user's text. |
| D23 | Missing or `null` fields are handled at the API boundary (400); the domain trusts its non-nullable types and is not tested with `null` | Common .NET practice: validate at the boundary, trust inside. `null` only comes from HTTP input, so it is tested where it happens. |
| D24 | Tasks on a closed grant can still be completed | Completing records work that was really done; it adds nothing, so BR-1 does not apply. A closed grant has no pending High task (BR-5), so only Low/Medium leftovers can be completed. |
| D25 | A refused close names every pending High task in the message; no closed date is stored, so closing again returns 200 unchanged, as D13 | Tells the officer exactly what to finish. A closed date is not in the brief and would add a second source of truth next to `Status`. |
| D26 | `GrantTask.IsCompletedLate` (completed after the due date), computed like `IsOverdue`; returned by the API and shown on screen as a "Completed late" label | Not in the brief; tan asked for it so "overdue" (still to do and late) and "completed late" (done, but late) stay separate, tested concepts. Costs one line. |
| D27 | Domain rule violations throw `DomainRuleException` with a `Code`, a `Kind` (Validation → 400, RuleRefused → 409) and an optional `Field` (the input the error belongs to, so the screen shows it next to that input); one handler maps it to ProblemDetails | Simple and common in .NET. A new error needs no change in any endpoint (open/closed). |
| D28 | No data annotations (`[Required]`, `[MaxLength]`) on domain classes; rules are enforced in domain methods, database mapping uses EF Fluent API in `Data/` | An annotation only labels a rule; a direct call can still break it. Each rule stays in one place and is unit-testable. |
| D29 | Business rules are built one rule at a time (vertical slices): a red commit with that rule's tests, then a green commit with its code | Real TDD cycles (red → green → refactor) that show in `git log`; each rule can be reviewed with its own tests, and only the red commits fail `./test.sh`. |
| D30 | `AddTask` refuses a priority that is not Low, Medium or High: 400 `priority-invalid` on `priority`, checked after BR-2 | An undefined value would never count as High and could slip past BR-5. Keeps D5's promise that a missing priority is caught. |
| D31 | After trimming (D22), the title length is counted with `string.Length` (UTF-16 units), the same way an HTML `maxlength` counts | The screen and the API agree on the limit. An emoji counts as 2; titles are plain text, so this is accepted. |
| D32 | A property that carries a design decision may have a `/// <summary>` explaining it (e.g. `CompletedDate`, `IdempotencyKey`) | Those reasons are likely interview questions and show on hover. Other properties keep a short `//` only when needed. |
| D33 | Seed data replays history through the domain: each task is added with "today" = its grant's application date; tasks marked Completed are completed on their due date; GR-100003's tasks are added and completed before the grant is closed | Even sample data obeys every rule, and the domain needs no back door. The brief gives no completion dates, so "on the due date" is an assumption for the README. |
| D34 | Routes exactly as in the brief | Reviewers recognise them at once; nothing to defend. |
| D35 | Responses: a task has id, grantId, title, dueDate, priority, completedDate, isOverdue (computed by the server), completedLate · grant list adds pendingTaskCount, grant detail adds tasks · POST a task → 201 without Location (no GET /tasks/{id} in the brief); a retry with the same key → 200 · complete → 200 · close → 200 with the grant · overdue rows add referenceNumber, organisationName, daysOverdue · dates as yyyy-MM-dd, enums by name · errors as ProblemDetails with `code` and `field` | The screen gets what it shows without computing rules itself; the officer's clock cannot change what is overdue. |
| D36 | Column lengths: Title 100 (BR-3, same constant as the domain) · IdempotencyKey 36 (a UUID; the API accepts only UUIDs) · ReferenceNumber 9 (`GR-` + 6 digits as in the brief; widen with a migration if needed) · OrganisationName 200 (the Australian Business Register limit, ABR data dictionary 9.9.7) | Every length has a source; none is padding. |
| D37 | Indexes: ReferenceNumber unique · IdempotencyKey unique where not null · GrantId · DueDate | Unique reference; one task per retried request even under concurrency; fast task loading per grant; fast overdue list (OP-6). |
| D38 | Deleting a grant that still has tasks is restricted, not cascaded | The brief has no delete and ends a grant by closing it; support and reporting need history (OP-4, OP-6), so tasks are never lost silently. |
| D39 | Seed data is added only in Development, right after the database is recreated | The brief's data is fictional; a real office must never see sample grants. `dotnet run` uses Development, so reviewers still get the seed. |
| D40 | Seeded tasks get `CreatedAt` = their grant's application date at 00:00 UTC | Matches D33's replayed history (each task is added on the application date), and the seed needs only "today". |
