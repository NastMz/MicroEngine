# Security policy

## Reporting a vulnerability

Please do not open a public issue for a security problem. Report it privately through GitHub:
**Security → Report a vulnerability** on this repository (GitHub private vulnerability reporting). Include what is
affected (engine, a demo, the browser packages, the website or the CI workflows), how to reproduce it, and its
impact as you understand it.

You can expect an acknowledgement within 7 days and an assessment within 30 days. Fixes are released as a new
version; the report is credited unless you ask otherwise. There is no bug bounty.

## Scope

| In scope | Out of scope |
| --- | --- |
| The engine (`src/`), the demo games (`games/`), the browser host (`web/`) | Vulnerabilities in raylib, .NET or other dependencies (report them upstream; tell us if MicroEngine is affected) |
| The published website and browser demos | Social engineering, denial of service against GitHub or Vercel |
| The GitHub Actions workflows (for example a way for a pull request to reach secrets or deploy) | Findings that need a compromised developer machine |

## Supported versions

MicroEngine is in Alpha. Security fixes go into the latest SDK version on nuget.org (`sdk-v*` releases), the latest
demo release (`v*`) and the published website; older versions are not patched, so update to the latest one.
