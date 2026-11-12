# Testing strategy

## Run the checks

```bash
./test.sh
```

One command checks formatting and line length, runs 95 xUnit backend tests, type-checks the front end, and runs 52 Vitest tests. Tests use fixed dates and a fake clock. Each API test has its own in-memory database, so results do not depend on the real date or test order.

## What the tests cover

| Level | Tool | Coverage | Count |
|---|---|---|---|
| Domain | xUnit, no database | BR-1 to BR-6 and their boundaries: due today or yesterday, 100 or 101 characters, repeat completion, pending or completed High task, overdue or completed late. “Today” is passed as `DateOnly`. | 42 |
| Services | xUnit | “Today” in the configured time zone, using an injected clock | 2 |
| Data | xUnit + SQLite in-memory | Unique indexes, restricted deletes, and seed data against the brief | 6 |
| API | `WebApplicationFactory` + SQLite in-memory + `FakeTimeProvider` | Endpoints, status and error details, rules over HTTP, clock use, safe retries (OP-3), health check (OP-5), and startup | 45 |
| Front end | Vitest + Testing Library, fake API | API client and views: overdue highlights, field errors, completion, closing grants, filters, connection errors, retry keys, and ignoring older responses | 52 |

Domain tests cover each rule boundary once. API tests check that requests are wired correctly: status and error body, clock, and database writes. Front-end tests check what officers see without starting a server.

Each test lists its case ID and the bug it catches. The intended coverage is one distinct bug per test.

## Operations risks

| Risk | Test coverage |
|---|---|
| **OP-3: duplicate request or lost connection** | API RT-1 to RT-7 cover retries, reused keys (422), closed grants (409), and requests without keys. Front-end NET-1 to NET-6 and KEY-1 to KEY-3 cover 502–504 errors, timeouts, kept form values, reuse on retry, and a new key after edits. |
| **OP-5: API cannot reach its data** | HC-1 checks healthy → 200; HC-2 drops tables and checks for 503. |
| **OP-2: two officers update one grant at once** | Not automated. A tester reproduced the race: simultaneous close and High-task addition can leave a closed grant with a pending High task. To test it, repeatedly send both requests to a file-based database and assert that state never occurs. After the `Version` fix, one request should return 409. |

## How I checked the AI-written tests

1. **List cases first.** I approved each scenario, expected result, and bug before implementation.
2. **Red, then green.** Each task has a failing `test(...)` commit and a passing `feat(...)` commit in `git log`.
3. **Break the code on purpose.** After tests passed, the AI introduced the bugs listed in each “Catches:” line and recorded which test failed in `docs/work-log/step-*.md`. A script backs up and restores the file on any exit, then checks its hash.
4. **Get independent reviews.** Reviewer agents and five tester agents tried to break the app.

This caught:
- A clock test that passed even when real time was used, because its fixed date matched that day. The AI moved it to a past date, and I approved.
- A missing API test for past due dates. Replacing “today” with the minimum date left all 94 tests passing, so the AI added AT-9.
- A form line that no test could catch and that was not needed; the AI removed it.

## What I did not test

| Not tested | Risk or current check |
|---|---|
| OP-2 concurrency and simultaneous retries with one key (second retry can return 500) | Two officers can break BR-5 at the same time. |
| Multiple API instances on one database; restart during a request | Checked by hand only: data survives restart and retry returns 200. |
| Midnight and offices in other time zones | “Today” may differ from an officer’s local date near midnight. |
| Malformed input (bad JSON, `"priority": "Urgent"`) | Checked by hand; returns a 400 with an empty body. |
| Full flow in a real browser | Components use a fake API; the full flow was tried by hand. |
| Accessibility and very long titles | Only keyboard use of lists was checked. |
| Overdue-list performance with large data | Currently loads every task for the selected grants. |

Known gaps in the tests:
- BR4-4 (completing a task on a closed grant) catches nothing now because tasks do not know their grant. It protects the rule if that link is added.
- `due-date-before-application` is tested in the domain, not through the API.
- All grant-detail Close tests use grant 1, so a bug that always closes grant 1 could pass.

## Before going live

1. Add Playwright tests for main flows against a real API.
2. Add the OP-2 concurrency test with the `Version` fix.
3. Load-test the overdue list (for example, 100,000 tasks) with a time limit.
4. Run API tests against the production database server and migrations.
5. Test time zones around midnight in Sydney.
6. Check accessibility with a keyboard and screen reader.

