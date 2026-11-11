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

## Open questions
Decide in step 4: column lengths, indexes, cascade delete, seeding approach.
