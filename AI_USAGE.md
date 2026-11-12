# AI usage
Written by Tan Nhat Phan. Claude helped organize and edit the text.

## Tools and models
I used Claude Code (desktop app):
- Sonnet 5.5 to read and understand the brief and other documents.
- Opus 5.5 to plan, set up the project, and write code. All sub-agents and sessions also used Opus 5.5.
- Three reviewers independently checked the six business rules: one for logic, one by deliberately breaking the code, and one against the brief and plan. They found gaps, and eight tests were added.
- A reviewer checked the grant list and found mouse-only rows and a filter that reset after Back. Both were fixed.
- Five testers checked the app end to end, each with a separate port and database copy and read-only access to the repo. Their findings drove the final fixes.
- I used a separate session for each step, then started fresh after step 4. Two screens were built in two sessions, each limited to its own files. Before submission, a review session helped me check the remaining risks, including a close-vs-add race and the .NET SDK pin.

## Agent setup
The reviewers value judgement over volume. I kept the AI's instructions clear, its scope limited, and its work checked.

| File | Purpose                                                                                                                            |
|---|------------------------------------------------------------------------------------------------------------------------------------|
| `CLAUDE.md` | The brief, tech stack, commands, folder structure, coding and testing rules, and how to work with me. Every session loads it.      |
| `docs/plan.md` | Scope, roles, steps, tasks, and decisions D1–D67 with reasons. The AI follows the plan, and I can explain each decision.           |
| `docs/work-log/` | One Build, Review, and Test log per step, using `TEMPLATE.md`, so I can see what changed, what was checked, and what was left out. |
| `docs/ai-log.md` | AI mistakes, how I caught them, and how to prevent repeats. Each lesson becomes a rule or check.                                   |
| `.claude/settings.json` | Allows build, test, run, and read-only Git. Package changes require my approval; commits, pushes, and `.env` access are denied.    |
| `test.sh` | Runs formatting, line-length and code checks, backend tests, type-checking, and front-end tests.                                   |

I approved the plan before work began. The AI touched only planned files and paused when it reached an unplanned decision. It stopped after each step until I said `go`; `discuss` meant talk only, and `approve` accepted the plan. After step 4, I started a fresh session and carried the work forward through `CLAUDE.md`, the plan, and the logs.

## Workflow
I split the work into steps and tasks (for example, T3.1 and T3.2), with one commit per task.

| Step | Work |
|---|---|
| 1 | Design and scaffold: data model, `CLAUDE.md`, plan, and `test.sh` |
| 2 | Business-rule test cases: I wrote them; AI reviewed them |
| 3 | Rules BR-1 to BR-6: I wrote BR-5; AI wrote its tests first |
| 4 | API, database, seed data, endpoints, and errors |
| 5 / 5b | Front end; added the grant list, Close grant, and overdue view |
| 6 / 6b | Operations needs OP-3 and OP-5; I wrote `/health`, then fixed tester findings |
| 7 | Documentation |
| 8 | Fresh clone, follow the README, and submit |

Each task followed the same loop:
1. **Plan:** AI lists the changes and files; I approve.
2. **Red:** Write and commit failing tests (`test(T3.3): …`).
3. **Green:** Implement until the tests pass (`feat(T3.3): …`).
4. **Review:** AI checks the diff against the plan and `CLAUDE.md`.
5. **Test:** Run `./test.sh`, then deliberately break the code and confirm a test fails.
6. **Log:** Update the step log; AI asks me one or two questions about why.
7. **Me:** I review, commit, and say `go` to continue.

The red and green commits are visible in `git log`.

## Key prompts
Translated from Vietnamese and shortened. More are in `docs/prompts.md`; the 4th and 5th are my comments in `docs/ai-log.md`.

| Prompt | Effect |
|---|---|
| “Review and test every part, and record both in your log. I review the logs; I commit and push.” | Set the Build → Review → Test loop and kept commits under my control. |
| “We’re still discussing. Before making changes, give me a short plan and wait for approval.” | Kept discussion separate from implementation. |
| “Add a column explaining how to prevent each mistake from happening again.” | Turned mistakes into rules in `CLAUDE.md` or checks in `test.sh`. |
| “If something falls outside the rules or plan, ask me.” | Made the AI stop at conflicts instead of deciding alone. |
| “Use a template. Keep it simple; don’t add this check to `test.sh`.” | I removed an unnecessary check the AI had added. |
| “Keep everything short and clear enough to review in 30 seconds.” | Made the logs and docs quicker to scan. |

## Where the AI went wrong
`docs/ai-log.md` records all 34 cases. These four were caught in different ways:

| Mistake                                                                                                                                                                              | How I caught it | Fix |
|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---|---|
| A clock test used a fixed time that happened to match the real date, so it could not catch use of the real clock.                                                                    | The AI replaced the injected clock with the real one while breaking the code on purpose; all tests still passed. | Used a past date and added a rule to `CLAUDE.md`. |
| No API test rejected a past due date; replacing “today” with the minimum date still passed all 94 tests.                                                                             | A tester deliberately broke it. | Added AT-9: yesterday’s due date returns 400. |
| Comments made up about 40% of the code and often repeated it.                                                                                                                        | I found the code cluttered. | Kept only short summaries; `test.sh` now checks long lines and decision numbers in code. |
| The front-end plan covered only four of six endpoints and left Close grant and the overdue list without a screen. It was checked against its own decisions, never against the brief. | I asked how to close a grant. | I chose to add the brief's optional grant list and Close grant, and an overdue view, in step 5b. Plans are now checked against the brief. |

The process grew heavy; with two hours I’d keep the loop but record only the key decisions.
I learned that written rules alone were easy to forget. Checks in `test.sh` and the log template helped prevent repeats.

## What I wrote or rewrote
- BR-5, which prevents closing a grant while a High-priority task is pending. AI wrote the tests first; I wrote the code, then AI reviewed it.
- The `/health` check (`DatabaseHealthCheck` and its route), using the same process.
- The business-rule test case list in step 2.
- The design and every decision in `docs/plan.md`. AI suggested options; I chose.
- My comments in `docs/ai-log.md`, and review changes I asked for, such as one `switch` for title checks and the name `ConfiguredTimeZoneCalendar` (the AI made the code change).
- This file.
