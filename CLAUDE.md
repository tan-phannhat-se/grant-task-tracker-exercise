# Grant Task Tracker: rules for the AI agent

## Context
A take-home exercise: a small tool for grants officers to see grants, add tasks, complete tasks and spot overdue tasks. The reviewers assess **judgement more than volume**: how the work is broken down, how the AI is guided and checked, and whether the developer (tan) can explain every line. A small, correct, well-tested solution beats a large one.

- Requirements and decisions: `docs/plan.md`. Read it before any change. Do not build anything that is not in it.
- AI mistakes are logged in `docs/ai-log.md` (see "Working with tan").

## Docs map
| File | Holds |
|------|-------|
| `docs/plan.md` | Scope, roles, steps, decisions (D1, D2…) |
| `docs/work-log/step-N-*.md` | The AI's Build / Review / Test log, one file per step |
| `docs/ai-log.md` | AI mistakes and how we prevent them |
| `docs/prompts.md` | A few key prompts from tan |

## Stack and commands
- Backend: .NET 8, ASP.NET Core minimal API, EF Core 8 + SQLite, xUnit. SDK pinned in `global.json`.
- Frontend: React 19 + TypeScript, Vite, Vitest + Testing Library.
- Run every check (format, tests, type-check): `./test.sh` (must pass before every commit).
- API: `dotnet run --project backend/src/GrantTracker.Api` → http://localhost:5080 (Swagger UI at `/swagger`, Development only)
- Frontend: `cd frontend && npm run dev` → http://localhost:5173 (`/api/*` is proxied to the API).

## Structure (one API project, separated by folder)
```
backend/src/GrantTracker.Api/
  Domain/     entities and business rules BR-1..BR-6. No EF Core, no HTTP, no clock.
  Services/   use cases: load from DbContext, call the domain, save. Gets "today" from TimeProvider.
  Data/       DbContext, EF configuration, seed data.
  Endpoints/  HTTP mapping only: request/response types, status codes, ProblemDetails.
backend/tests/GrantTracker.Tests/
  Domain/     unit tests of the rules (no database)
  Api/        integration tests: WebApplicationFactory + SQLite in-memory + FakeTimeProvider
frontend/src/  one screen; API client in one typed module
```
Dependencies point inwards: Endpoints → Services → Domain. Domain depends on nothing.

## Hard rules
- **Never call `DateTime.Now`, `DateTime.UtcNow` or `DateOnly.FromDateTime(DateTime.Now)` in Domain or Services.** Inject `TimeProvider`; domain methods take `DateOnly today` as a parameter.
- Business dates (application date, due date, completed date) are `DateOnly`. Timestamps (`CreatedAt`) are UTC `DateTime`.
- Enums use explicit gapped values, never 0: see D5 (`Priority`) and D17 (`Status`) in `docs/plan.md`.
- Errors: ProblemDetails with a stable `code`, status codes as in D16.
- Each business rule lives in exactly one place in Domain.
- No new packages, frameworks or abstractions (repositories, MediatR, AutoMapper…) without asking tan first.
- Never use or paste code or data from tan's employer.

## Code style
- **Format:** code must pass the format check in `./test.sh` (D19). Run `dotnet format backend/GrantTracker.sln` and `cd frontend && npm run format` before handing work over.
- **Simple and clear first.** A newcomer must understand the code on first read: plain logic, small methods, clear names, no clever tricks.
- Several checks on the same value use one `switch` (e.g. `case 0:` · `case > Max:`), not a chain of `if`s.
- **Apply SOLID, but keep it simple.** Use it where it makes the code clearer, not as a checklist: no interface, layer or abstraction without a concrete reason (the brief says "don't over-engineer").
  - **S**ingle responsibility: one job per class. Domain = rules, Services = use cases, Data = storage, Endpoints = HTTP.
  - **O**pen/closed: add behaviour by adding code, not by editing unrelated code (e.g. one place maps domain errors to ProblemDetails, so a new error does not touch every endpoint).
  - **L**iskov: prefer composition; any subtype must work wherever its base type is used.
  - **I**nterface segregation: small, focused interfaces; no "do everything" service.
  - **D**ependency inversion: depend on abstractions injected through the constructor for what changes or must be faked in tests (`TimeProvider`, `DbContext`).
- **Comments: short and only where they help.**
  - Every class and public method: a `/// <summary>` on three lines (open tag, one short sentence, close tag), as Rider and Visual Studio generate it; `/** */` in TypeScript.
  - Properties and lines: a short `//` comment only when the reason is not obvious.
  - A line that enforces a business rule names it (e.g. `// BR-2`). No decision numbers (Dxx) or OP numbers in code; traceability lives in `docs/`.
  - Explain why, never repeat what the code says. No `<param>` blocks.
  - One type per file.
- **Line length:** at most 120 characters in C# (checked by `./test.sh`), 100 in the frontend (Prettier).

## Tests
- xUnit, Arrange / Act / Assert, names like `AddTask_DueDateYesterday_IsRejected`.
- Each test has a summary: case id, scenario → expected result, and a "Catches:" line (the bug it catches).
- Fixed dates only (e.g. `new DateOnly(2026, 11, 11)` or `FakeTimeProvider`). Tests must not depend on the real date or on run order.
- Each test must catch a different bug. Before adding a test, say which bug it catches.
- Prove a new test can fail: break the code on purpose, watch it go red, restore.

## Working with tan

### Roles and limits
- **Roles:** tan designs, decides, approves plans, reviews and commits; the AI plans and implements (see Roles in `docs/plan.md`). Exception: tan writes BR-5, `AI_USAGE.md` and the OP-5 health check; the AI only reviews them (for BR-5 the AI writes the tests first and gives hints when asked).
- **Change only what tan asks to change.** A question or a discussion is not a request: answer or propose, then wait. No edits to files tan did not ask about.
- **Commit and push are tan's. The AI never runs `git commit` or `git push`.** It may suggest a conventional commit message (`feat:`, `test:`, `docs:`, `chore:`).

### Workflow for every piece of work
1. **Plan first, then wait.** Show tan a short plan: each step and every file it touches. One plan may cover several steps; tan approves it once. At the start of each step, break it into tasks (T3.1, T3.2…), one commit each (e.g. `feat(T3.3): BR-2 due date rules`); the task list is part of the plan tan approves and goes into the Tasks section of `docs/plan.md`.
2. **Tests first.** tan approves the test case list; then the AI writes code until the tests pass.
3. **Build** one step at a time. Touch only the files in the plan; anything else → stop and ask.
4. **Undecided choice** (not in `docs/plan.md`) → stop and ask. Never decide alone.
5. **Review**: re-read the whole diff against `docs/plan.md` and this file; look for bugs, missed edge cases, over-building and rule breaks. Fix what is found.
6. **Test**: run `./test.sh` plus any check that proves the change works (e.g. start the API and call it). New tests: see "Tests".
7. **Log**: record the step in its own file `docs/work-log/step-N-*.md`, including the files touched. tan reviews it. Format:
   - Done · Files touched (one per line) · Review (checklist, missing, how to review) · Test (checklist, missing, how to test).
   - Marks: ✅ pass · 🔧 found and fixed · ⏳ not yet. Guides go broad → narrow.
   - Limits: one entry per step (update it while the step runs) · about 40 lines · tables up to 8 rows, 🔧 and ⏳ first · files by folder · up to 4 self-check items.
8. **Ask**: one or two "why" questions to tan about the change.
9. **Stop** and wait for tan's `go` before the next step. tan reviews and commits.

- Keywords from tan: `go` = do it · `discuss` = talk only, no edits · `approve` = plan accepted.
- Step 3 (business rules): a separate reviewer subagent reads the diff and looks for bugs before tan reviews.
- After step 4: start a fresh session; `CLAUDE.md`, `docs/plan.md` and the logs carry the state.

### Logs and writing
- When the AI is wrong, incomplete or over-engineered, add an entry to `docs/ai-log.md` straight away, including how to stop it happening again. If the prevention is a rule, add it to this file; if a check can enforce it, add it to `test.sh`.
- When tan gives a prompt that changes how the work is done, propose adding it to `docs/prompts.md` (English translation + why it mattered). Keep that file to a few key prompts; corrections go to `ai-log.md`, design choices to `plan.md`.
- New decisions go into the Decisions table in `docs/plan.md` with the reason.
- Language: **everything in the repository is English** (code, comments, docs, logs, commit messages). In chat, reply in the language tan writes in.
- **Write everything short and clear: readable in 30 seconds.** Tables and checklists over paragraphs. This applies to chat, logs and docs. Replies are short, one step at a time.
