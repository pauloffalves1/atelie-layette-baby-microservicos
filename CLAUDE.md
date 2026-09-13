# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

This is the production system for Ateliê Layette Baby (layettebaby.com.br) — a microservices
architecture (Identity, Catalog, Orders, Backoffice, Notifications, Gateway/YARP + an Angular
Native Federation frontend). It was extracted on 2026-09-10 via `git subtree split
--prefix=microservices` from the monorepo `pauloffalves1/atelie-bebe`, which now holds only the
older, frozen `server/`/`client/` monolith (kept as a rollback, no longer deployed, receives no new
work). All commit history that touched this tree before the split was preserved here.

`README.md` is the canonical, detailed reference — architecture diagrams (Mermaid), domain
storytelling, event storming, the full RF/RNF requirement catalog, the auth/permission model, and
the testing strategy. Read it for anything beyond the condensed notes below rather than re-deriving
it from source.

`spec/` holds the same requirement catalog reframed as formal Spec-Driven Development docs (same
pattern as the frozen monolith's own `spec/`, but scoped to what's actually built here):
`requirements.md` (user story + EARS acceptance criteria per requirement, cross-referenced to the
README's RF/RNF numbers), `design.md` (architecture decisions and cross-service contracts per
requirement, complementing rather than repeating README's diagrams), and `tasks.md` (a
requirement-traceable implementation checklist). Append new `[ ]` tasks there, under a new
`requirements.md` entry first, before starting a new feature — then update README's RF/RNF table and
Status list to match once it's done (see the commit that added RF22-RF27 for the shape of that
update).

## Commands

```bash
dotnet build AtelieBebe.Microservices.slnx                          # build everything
dotnet test AtelieBebe.Microservices.slnx --nologo                  # 142 unit tests, 5 projects
cp .env.example .env && cd keys && openssl genrsa -out jwt-private.pem 2048 && \
  openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem       # one-time local setup
docker compose build && docker compose up -d                        # run everything locally
cd frontend/shell && npm run test:e2e                                # 7 Playwright e2e specs
k6 run load-tests/gateway-smoke.js                                   # load smoke test
```

`.github/workflows/ci.yml` runs the unit tests and, in a second job, the e2e suite plus the k6 load
test against a throwaway `docker compose` stack, on every push/PR.

**Windows gotcha:** if this repo's real on-disk path casing doesn't match how you `cd` into it
(e.g. it physically lives at `C:\IA\...` but you open a shell at `C:\ia\...`), Playwright's test
loader can resolve the same spec file under two different casings and corrupt its internal suite
registry — `"Playwright Test did not expect test() to be called here"`, 0 tests collected, even via
`npm run test:e2e`. Fix: open the shell from the path's true on-disk casing (PowerShell:
`(Get-Item "<path>").FullName`) before touching Playwright. See the README's e2e section.

## Architecture (condensed — see README.md for the full picture)

Four bounded-context services (Identity, Catalog, Orders, Backoffice) each own their own SQL Server
database, one Notifications worker, one Gateway (YARP) as the single HTTP entry point. Each service
follows the same internal shape: `AtelieBebe.<Service>.Core` (domain + application, EF Core), a thin
`AtelieBebe.<Service>.Api` (Minimal API endpoints only), and `AtelieBebe.<Service>.Core.Tests`
(xUnit, domain invariants only, no EF/DB). `shared/AtelieBebe.SharedKernel` holds `Entity`/
`ValueObject`/exceptions, the transactional outbox, the RabbitMQ client, and shared JWT auth
(RS256) — including `AdminPermission`, a `[Flags]` enum gating every admin endpoint group per
feature area (see README's "Permissões granulares por administradora").

Frontend is a microfrontend (`frontend/shell`, Angular + Native Federation): `shell` hosts
`storefront` (public store) and `admin` (admin panel) as runtime remotes, each independently
buildable/testable.

## Working here

- New admin functionality needs its own `AdminPermission` flag and a corresponding policy
  (`JwtAuthenticationExtensions.PermissionPolicyName`) — don't gate a new admin endpoint group with
  the generic `"AdminOnly"` policy unless it's genuinely self-service (like 2FA/password change).
- A new domain event that should trigger a notification needs a case in
  `OutboxProcessor.DispatchAsync` (or the per-service outbox publisher) and a method on
  `INotificationSender`.
- Keep `dotnet test AtelieBebe.Microservices.slnx` and `npm run test:e2e` green before considering
  a change done — both are fast enough to run locally, and CI runs the exact same commands.
