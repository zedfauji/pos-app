# Cleanup Rules

**Status**: ACTIVE  
**Owner**: Technical Program Manager  
**Last Reviewed**: 2025-12-22  

---

## Core Principle

> **Cleanup ≠ Refactor**

Cleanup operations must NEVER change runtime behavior.

---

## Allowed Operations

✅ **YES**:
- Move files between folders
- Archive obsolete content
- Add README files
- Add comments (status markers)
- Create documentation
- Update `.csproj` include paths (if moving files)
- Add `.gitkeep` to empty folders

❌ **NO**:
- Rename public classes
- Rename public methods
- Change namespaces
- Modify logic
- "Simplify" code
- Remove unused code
- Add new dependencies
- Change UI behavior

---

## File Classification

Every file must belong to ONE category:

| Category | Meaning | Action |
|:---------|:--------|:-------|
| ACTIVE | Runtime code | DO NOT TOUCH |
| LEGACY | Old but referenced | READ-ONLY |
| PLANNING | Future intent | Add headers |
| HISTORICAL | Past decisions | Archive |
| ABANDONED | Dead code | Archive |
| UNKNOWN | Unclear | Flag for review |

---

## Archive Protocol

1. Create `/archive/` folder in relevant location
2. Move file with git (preserves history)
3. Add `ARCHIVED_README.md` explaining why
4. Update any project references

```bash
git mv old-file.cs archive/old-file.cs
```

---

## Documentation Headers

All planning docs MUST have:

```markdown
---
Status: ACTIVE | SUPERSEDED | ABANDONED
Owner: [Name or Role]
Last Reviewed: YYYY-MM-DD
Supersedes: [Document name or N/A]
Notes: [Brief explanation]
---
```

---

## Build Safety Check

After ANY cleanup operation:

1. `dotnet build solution/MagiDesk.sln`
2. Launch client manually
3. Verify core flows work:
   - Table map loads
   - Can start session
   - Payment page accessible

If ANYTHING breaks → **REVERT IMMEDIATELY**

---

## Escalation Triggers

STOP and escalate if:

- Tempted to "fix" something while moving
- File purpose is unclear
- Moving file breaks compilation
- Reference is used but file seems dead
- Not sure if file is runtime-critical

---

## Cleanup vs Other Tasks

| Task Type | Who Approves | May Change Behavior |
|:----------|:-------------|:-------------------|
| Cleanup | TPM | ❌ No |
| Refactor | Architect | ⚠️ Maybe |
| Feature | Product | ✅ Yes |
| Bugfix | QA | ✅ Yes |

---

**When in doubt, DON'T.**
