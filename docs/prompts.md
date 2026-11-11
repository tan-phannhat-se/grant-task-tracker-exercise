# Key prompts

A few prompts that changed how the work is done.

| # | Prompt | Why it mattered |
|---|--------|-----------------|
| 1 | "Every part you finish must have a review phase and a test phase, written to your own log, which I will review. Commits and pushes are done by me." | Set the Build → Review → Test workflow and the AI work log in `docs/work-log/`. I stay the gate for every commit. |
| 2 | "The AI log needs one more column: how to make sure the same mistake doesn't happen again." | Each AI mistake now becomes a `CLAUDE.md` rule or a `test.sh` check. |
| 3 | "Review the project again: what is extra or missing compared with the interview document? This is the scaffold step." | Checking each step against the brief found two gaps: no GitHub repo, no prompt log. |
| 4 | "Everything must be short and clear, understood in a 30-second skim." | Logs and docs became tables and checklists I can review quickly. |
| 5 | "Don't change anything I didn't ask for; we're still discussing. Before doing anything, give me a short plan with the implementation steps, and only implement after I approve." | Stopped the AI acting on discussion. Every change now starts with a plan I approve. |
| 6 | "The six practices make sense, but batching decisions and approving one plan for several steps worries me: the AI may overstep while it works." | Added guardrails: touch only the files in the plan, stop at any undecided choice, stop after each step until I say `go`, and list the files touched in the log. |
