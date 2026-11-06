# ADR 0048: Documentation in the present tense, published with the SDK that it describes

## Status

Accepted (2026-11-05). Amends [ADR 0022](0022-release-coordination.md): the site still deploys on merge, but it waits
while `main` holds SDK changes nuget.org does not serve. Procedure: [Releasing, Documentation and releases](../releasing.md#documentation-and-releases).

## Context

Merged documentation reached production at once, while the SDK changes it described reached nuget.org only with the
next SDK release. Each page that mentioned such a change had to label it ("not published yet", "in the next SDK"),
and each release had to find and rewrite those labels across the documentation and the landing page. Before
0.5.0-alpha.1 they were spread over dozens of pages. Two alternatives were considered:

- **Write in the present tense and keep deploying on merge.** No labels, but the site would show APIs that the
  installable SDK lacks: a reader copies an example, installs the package and the code does not compile.
- **Release the SDK automatically on every SDK change.** Always in step, but nuget.org versions are permanent and
  immutable: every merge would mint a version (with its approval), and Alpha churn would fill the feed.

## Decision

- **Documentation is written in the present tense**, as if the SDK that contains it were published. No page carries
  "not published yet" labels.
- **Pending SDK changes are listed in one place**: `## Next release` in the release notes (and in the migration notes
  for breaking changes). A pull request that changes the SDK adds its entry there.
- **Pending changes hold production.** While that section has content (`node sdk/version.mjs status`: `pending`),
  production keeps its deployment and demo releases are refused; previews deploy. The SDK release that ships them
  deploys the site once nuget.org serves the packages, so SDK and documentation are published together.
- **The release command closes the section**: `node sdk/version.mjs release <version>` renames each "Next release"
  section to the version (and relinks it) and refuses a version with nothing under it, so an SDK release is never empty.
- **Site-only changes need no SDK version.** With nothing pending they deploy on merge. With changes pending they wait
  for that release, the price of never showing an uninstallable API; a separate release branch for the site was
  rejected as more process than the rare urgent fix needs.

## Consequences

- No release rewrites tense across the site; `version.mjs release` does the only rename.
- Between an SDK change and its release, production does not move, even for unrelated site fixes. Releasing the SDK
  (or reverting the pending change) is how a fix goes out sooner.
- `check` refuses an empty "Next release" heading and migration notes without release notes, so the gate cannot be
  left on or off by mistake.
