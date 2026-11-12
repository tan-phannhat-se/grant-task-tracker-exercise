# Grant Task Tracker

A small tool for grants officers to view grants, manage tasks, and find overdue work.
Built with ASP.NET Core 8 minimal API, EF Core (SQLite), React 19, TypeScript, and Vite.

- Testing: [TESTING.md](TESTING.md)
- AI use: [AI_USAGE.md](AI_USAGE.md)
- Plan, decisions (D1–D67), and step logs: [docs/](docs/)

## Prerequisites

| Tool | Version | Check |
|---|---|---|
| .NET SDK | **8.0.1xx** ([download](https://dotnet.microsoft.com/download/dotnet/8.0)) | `dotnet --list-sdks` shows `8.0.x` |
| Node.js | 20.19+ or 22.12+ (Vite 7) | `node -v` |

Install .NET 8 first. `global.json` pins 8.0.1xx, so .NET 9 or 10 alone is not enough to build or test. With Homebrew, add keg-only `dotnet@8` to the front of your PATH:

```bash
export PATH="/opt/homebrew/opt/dotnet@8/bin:$PATH"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet@8/libexec"
```

## Run

Run commands from the repository root.

**API** → http://localhost:5080 (Swagger: `/swagger`; health: `/health`)

```bash
dotnet run --project backend/src/GrantTracker.Api
```

In Development, the app recreates `grant-tracker.db` and seeds the brief's four grants on each start, with dates relative to today. In Production (`ASPNETCORE_ENVIRONMENT=Production`), it keeps data across restarts, skips seeding, and disables Swagger. “Today” uses `App__TimeZone` (default `UTC`; set `Australia/Sydney` when live).

**Front end** → http://localhost:5173 (proxies `/api/*` to the API)

```bash
cd frontend
npm ci
npm run dev
```

**Tests:** run all checks with one command:

```bash
./test.sh
```

It checks formatting and line length, runs 95 xUnit backend tests, type-checks the front end, and runs 52 Vitest tests. Exit code 0 means all checks passed.

## What it does

| View | Features |
|---|---|
| Grant list | Filter Open, Closed, or All; see pending task counts; open a grant |
| Grant detail | View tasks, highlight overdue work and late completions, add or complete tasks, close the grant |
| Overdue tasks | View overdue tasks across grants, sorted by lateness, with priority and days overdue |

The brief asks for one grant screen. I also chose to build the grant list, Close grant, and overdue view.

## API

| Endpoint | Notes |
|---|---|
| `GET /grants?status=Open\|Closed\|All` | Defaults to all; includes `pendingTaskCount` |
| `GET /grants/{id}` | Grant and tasks, sorted by due date |
| `POST /grants/{id}/tasks` | Returns 201; optional UUID `Idempotency-Key` makes a retry return the same task with 200 |
| `PUT /tasks/{id}/complete` | Returns 200; repeat completion keeps the first date (BR-4) |
| `PUT /grants/{id}/close` | Returns 200; repeat close changes nothing; returns 409 if a High-priority task is pending (BR-5) |
| `GET /tasks/overdue?grantStatus=Open\|Closed\|All` | Defaults to Open; sorted by lateness; includes `daysOverdue` |
| `GET /health` | Returns 200 Healthy or 503 Unhealthy after checking the database |

Errors use ProblemDetails with a stable `code` and, when relevant, a `field`:

| Status | Meaning | Codes |
|---|---|---|
| 400 | Invalid input | `title-required`, `title-too-long`, `due-date-in-past`, `due-date-before-application`, `due-date-required`, `priority-invalid`, `status-invalid`, `idempotency-key-invalid` |
| 404 | Not found | `grant-not-found`, `task-not-found` |
| 409 | Current state prevents the action | `grant-closed` (BR-1), `pending-high-priority-tasks` (BR-5) |
| 422 | Key reused for a different request | `idempotency-key-reused` |

## Design decisions and trade-offs

| Decision | Reason | Trade-off |
|---|---|---|
| One API project with `Domain/`, `Services/`, `Data/`, and `Endpoints/`; dependencies point inward | Keeps a small app organized | Split into projects if it grows |
| Business rules live in the domain and receive `DateOnly today`; services get today from `TimeProvider` | One place per rule; dates are easy to test | — |
| Business dates use `DateOnly`; `CreatedAt` uses UTC `DateTime` | Rules compare dates, not times | — |
| One nullable `CompletedDate` per task | Completion state and date cannot conflict | — |
| Repeated completion or close returns 200 with no change | Safe for clients to retry | Repeats are not errors |
| Enums use spaced integers (Low 100, Medium 200, High 300; Open 100, Closed 200) | SQL sorting, room for values, and 0 means missing | Database shows numbers, not labels |
| Idempotency key is stored on the task with a unique index | Task and key save in one transaction | Keys do not expire |
| Organisation names are limited to 200 characters | Australian Business Register data dictionary 9.9.7 | — |
| Three views switch in `App`, without a router | Avoids another package for three views | Browser Back and direct grant links do not work |

See [docs/plan.md](docs/plan.md), “Decisions,” for the full list and reasons.

## Assumptions

- Seed tasks marked Completed were completed on their due date; the brief gives no completion dates.
- The overdue list defaults to Open grants; `grantStatus=All` includes every grant.
- All offices use one time zone for “today.”
- An early due date returns one error with the earliest valid date: today or the application date.
- Titles are trimmed before the 100-character check. Length matches HTML `maxlength` (an emoji counts as 2).
- Tasks on closed grants can still be completed; BR-1 prevents adding tasks, not recording completed work.
- A rejected close lists every pending High-priority task.

## Operations and expected problems

I implemented **OP-3** (safe retries and a clear connection error) and **OP-5** (health check). OP-3 protects work on unreliable regional connections. OP-5 is part of what a load balancer needs for OP-1.

| Problem | Current approach | Before going live |
|---|---|---|
| **OP-3** Duplicate request or lost connection | `Idempotency-Key` returns the same task on retry. The screen keeps one key per filled form; after a failure, 502–504, or 15-second timeout, it shows “Cannot reach the server…” and keeps the form values. | — |
| **OP-5** An API instance is unhealthy | `GET /health` checks the database and returns 200 or 503. | Also verify the database file or server is the expected one. |
| **OP-1** Multiple instances and restarts | API keeps no in-memory state; interrupted requests can be retried safely. | Use a shared SQL Server or PostgreSQL database and EF migrations instead of `EnsureCreated`. |
| **OP-2** Officers update one grant at once | Rules hold within each request. **Known gap:** a simultaneous close and High-priority task addition can both succeed, leaving a closed grant with a pending High task (BR-5). Simultaneous retries with one key can return 500 for the second request; the next click returns 200. | Add a `Version` concurrency token to `Grant`, updated on each add and close. A conflicting save returns 409 `concurrency-conflict`, and the screen reloads. |
| **OP-4** Support investigates issues later | Stable error codes; tasks record `CreatedAt`. | Add request IDs to errors and logs, central logging, user login, and an audit trail. .NET 8 logs handled 400/409 responses as “unhandled exception” errors, which is noisy. |
| **OP-6** Workload grows and overdue list slows | Data records `CreatedAt` and `CompletedDate`; `DueDate` is indexed. | Add a read-only stats endpoint, filter overdue tasks in SQL (currently tasks for matching grants are loaded first), and monitor route timings. |
| No login | Idempotency keys support retries, **not security**. | Add authentication and scope keys to each user. |
| Offices use different time zones | One configurable time zone. | Set a time zone per office. |
| .NET 8 support ended on 10 Nov 2026 | Kept the version used to build and test the app. | Move to .NET 10. |
| Data loss | — | Back up the database. |

## Known limitations

- Some requests read “today” twice (in the service and endpoint); `daysOverdue` is calculated in the response type, not the domain. The reads can differ only around midnight. Fix: read today once in the service and move `DaysOverdue(today)` to the domain.
- Malformed values (for example, `"priority": "Urgent"`, an invalid date, or broken JSON) return an empty 400. A non-numeric ID such as `/grants/abc` returns an empty 404. The screen shows a general message.
- On a closed grant, a missing due date returns 400 `due-date-required`, not 409 `grant-closed`.
- Uppercase and lowercase UUID keys are treated as different; the screen always sends lowercase.
- `"priority": 300` is accepted as High because numbers bind to the enum.
- `/health` does not detect a SQLite file deleted or replaced while the API is running.
- The screen needs `localhost` or HTTPS for `crypto.randomUUID`.

## Next steps

1. Add the OP-2 `Version` token and test simultaneous close and task addition.
2. Move to a shared database and EF migrations (OP-1).
3. Add login and scope idempotency keys per user.
4. Add request IDs and central logs; reduce noise for expected 400/409 errors (OP-4).
5. Filter overdue tasks and pending counts in SQL; add a stats endpoint (OP-6).
6. Add Playwright tests for main flows in a real browser.

## Time spent
About 15 hours.

More than the suggested 2 hours: I chose a strict loop for the AI (plan, tests first, break the code on purpose, review every diff), 
because how AI is guided and checked is a large part of this exercise. 
With 2 hours I’d keep that loop for the business rules and cut the per-step logs and most recorded decisions

## Repository map

```text
backend/src/GrantTracker.Api/   Domain/ · Services/ · Data/ · Endpoints/ · Program.cs
backend/tests/GrantTracker.Tests/   Domain/ · Services/ · Data/ · Api/
frontend/src/   App, GrantList, GrantDetail, TaskList, AddTaskForm, OverdueTaskList; api/grantsApi.ts
docs/   plan.md · work-log/ · ai-log.md · prompts.md
CLAUDE.md · .claude/settings.json   AI agent setup
test.sh   All checks in one command
```
