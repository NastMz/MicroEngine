# ADR 0010: Content-addressed demo packages and a prebuilt Vercel deployment pipeline

## Status

Accepted. Amends [ADR 0009](0009-public-site.md) (demo packages as a versioned build input).

## Context

ADR 0009 made demo packages a versioned input of the site: a person built them, packed them, created a
`demos-v<version>` release and committed its SHA-256 in `site/demos.lock.json`. Every engine or game change needed
those manual steps, and Vercel built the site itself from the repository with a stored GitHub token. The goal now is
that merging an approved pull request is enough to publish, without losing reproducibility, integrity checks or the
privacy of the repository.

## Decision

- **Keys instead of versions.** Each registered game's package is identified by a key: a SHA-256 over the Git blob
  ids of its inputs (engine, web host and toolchain files, the game folder, its registry entry, the third-party
  notices). Blob ids ignore line-ending conversion, so Windows and Linux runners agree. The lock file and the tag
  releases are removed; the commit itself determines which packages the site ships.
- **One store, never overwritten.** Packages and their manifests (archive SHA-256, per-file SHA-256, commit,
  toolchain) are assets of one private prerelease, `demo-artifacts`. A key is built once, by the first run that needs
  it, and reused by every later commit with the same inputs. `demos.mjs fetch` verifies archive and files before
  the site build continues.
- **One workflow with stages.** Plan (keys, change detection), engine tests, a matrix of package builds generated
  from the registry, the site checks, and deployment. Pull requests get protected previews; pushes to `main` deploy
  production; forks get no credentials.
- **`vercel build` + `vercel deploy --prebuilt` from GitHub Actions.** The run that checked a commit also builds and
  uploads its output, so a deployment cannot exist without its packages, Vercel never needs repository access or a
  GitHub token, and no source is uploaded. Git deployments are disabled in `vercel.json`. Rollback switches
  production to an earlier immutable deployment.

## Alternatives considered

- **Vercel's Git integration building the site.** Simpler, but Vercel would need repository access and a token for
  the private store, could deploy before the packages of a commit exist, and could not run the browser checks first.
- **GitHub Actions artifacts or cache for packages.** Artifacts expire (at most 90 days) and caches are evicted, which
  would break rebuilding or redeploying older commits; release assets are durable and private.
- **Versioned releases with a committed lock (ADR 0009).** Reproducible, but every change needed a person to build,
  tag and update the hash.
- **One key for all games.** Simpler, but a change to one game would rebuild all four (several minutes each).

## Consequences

- Documentation and site changes deploy in minutes without touching the WebAssembly toolchain; an engine change
  rebuilds every game in parallel; a game change rebuilds that game.
- The `demo-artifacts` release grows with every key; old assets can be deleted when no deployment to keep needs them.
- Keys only see committed (staged) inputs; local builds with uncommitted changes use `DEMOS_DIR`.
- The pipeline needs three settings (a Vercel token, team ID and project ID); see `docs/deployment.md`.
