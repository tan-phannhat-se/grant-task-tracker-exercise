# Work log · Step 6 · Operations needs · 2026-11-12 (in progress)
**Done:**
- Plan approved by tan: D57–D62 (key header, retry order, "same request", simultaneous retries, server unreachable, one key per form); tasks T6.1–T6.5 in `plan.md`
- T6.1 red: RT-1..RT-7 (8 runs); 6 fail for the right reason (201 instead of 200/400/422, 409 instead of 200); RT-2 and RT-6 already pass (today's code already does it), to be proved by breaking the code at green
- T6.2 red: HC-1, HC-2 fail with 404 (no `/health` route yet); tan writes the check

**Files touched:**
- `backend/tests/GrantTracker.Tests/Api/`: `AddTaskRetryTests.cs` (T6.1) · `HealthCheckTests.cs` (T6.2)
- `docs/plan.md` (Step 6 task table, D57–D62) · `docs/work-log/step-6-operations.md`

### Review
| # | Task | Result |
|---|------|--------|
| 1 | T6.1 red | ✅ each case maps to D57–D60 or a step 6 test note; no `field` expected (a header is not a form input) |
| 2 | T6.2 red | ✅ tests check only the status code, so tan is free to choose the body and the files |

**Missing:** T6.1 green · T6.2 green (tan) · T6.3–T6.5

**Review it yourself:**
1. `docs/plan.md` D57–D62: do they say what you approved?
2. `AddTaskRetryTests.cs`: each "Catches" line names a different bug.
3. `HealthCheckTests.cs` HC-2: dropping the tables is how the test makes the data unreachable; your check must notice it.

### Test
| # | Task | Red | Green | Break on purpose → result |
|---|------|-----|-------|---------------------------|
| 1 | T6.1 | ✅ 6 of 8 failed (wrong status); RT-2, RT-6 pass | ⏳ | ⏳ |
| 2 | T6.2 | ✅ 2 failed (404) | ⏳ tan | ⏳ |

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
| Retries (T6.1) | `dotnet test backend --filter "FullyQualifiedName~AddTaskRetryTests"` | Now (red): 6 failed, 2 passed · after green: 8 passed | ⏳ written at green |
| Health (T6.2) | `dotnet test backend --filter "FullyQualifiedName~HealthCheckTests"` | Now (red): 2 failed · after tan's check: 2 passed | ⏳ written at green |
