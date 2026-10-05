---
id: dk-fddb0416
type: task
created: 2026-10-05
status: todo
since: 2026-10-05
area: gamefonts
priority: P2
rank: t
parent:
fixes: []
blocked_by: []
relates: []
---
# Fold GameFonts.Search name-search loops into FindByName<T>

From docs/BACKLOG.md (P2)

- **Fold `GameFonts.Search`'s 3 name-search loops** into a `FindByName<T>` helper. Blocked on the
  cross-mod sync: `GameFonts.cs` is a verbatim copy shared with Chest Labels / Plant Peek, so the
  fix belongs in the workspace canonical + all copies, not here alone.
