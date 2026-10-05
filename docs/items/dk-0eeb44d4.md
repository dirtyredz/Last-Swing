---
id: dk-0eeb44d4
type: task
created: 2026-10-05
status: todo
since: 2026-10-05
area: healthbar
priority: P2
rank: n
parent:
fixes: []
blocked_by: []
relates: []
---
# De-leak Target.DestructibleSource

From docs/BACKLOG.md (P2)

- **De-leak `Target.DestructibleSource`.** Replace the exposed `DestructibleView` with a semantic
  property (e.g. `HasBecomeDestructed`) and have the view take a `forceEmpty` render flag instead of
  `HealthBar` synthesizing a mutable `Target`. **Now unblocked** — the kill-frame WIP was verified
  in game 2026-08-22 (owner: "all good"). Reviewers were split on whether it's a real leak, so it's
  optional polish, not a fix; if taken, re-run the killing-blow checklist afterward.
