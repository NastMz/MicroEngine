# Prototype history and greenfield transition

Status: repository transition explicitly approved by the owner on 2026-10-12.

## Preservation

- Prototype commit: `294eb5581f5cc6d3e8a7d57cfa8eb5961a068beb`.
- Annotated tag `legacy-final`, verified to target that commit.
- Documentary preservation commit: `b05dd9701c7e35f32bf1cb57920ba711fe9c4448`.
- Retirement commit: the commit introducing this file. Resolve with
  `git log --diff-filter=A --format=%H -- docs/engineering/history.md`.

The tag contains committed prototype files, not working-tree changes. Documentary work
was committed separately before retirement. Pre-existing untracked `.atl/` was preserved
locally and excluded. No remote publication or Git history rewrite was performed.

## Scope

278 tracked files retired: source/demos, tests, assets, benchmarks, demo save,
asset generator, solution, build/version settings, workflows, changelog and old technical docs.
General files and greenfield documentation remain. No new engine implementation was created.

## Historical inspection

~~~sh
git show legacy-final:README.md
git ls-tree -r --name-only legacy-final
git show legacy-final:.github/workflows/build.yml
~~~

For full exploration use a separate checkout/worktree at the tag in a suitable project
location. Do not restore prototype files over active work or migrate them automatically.
Tags do not preserve ignored/untracked data or external assets. Remote backup requires
separate publication authorization.

## Validation

Exact tag target and retirement inventory are verified before deletion. Active links,
tracked change scope and historical file availability are checked before closing the
transition. Prototype builds are not evidence of greenfield functionality.
