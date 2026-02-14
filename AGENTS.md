# Repository Guidelines

## Project Structure & Module Organization

- `client/`: Angular 18 app; code in `src/`, assets `public/`, dev proxy `proxy.conf.json`; builds land in `dist/` (keep untracked).
- `server/src/JulliusApi.sln`: `Jullius.ServiceApi` (ASP.NET Core + Serilog + EF Core SQL Server), `Jullius.Data` (DbContext/migrations), `Jullius.Domain`, `Jullius.Tests` (xUnit).
- `infra/`: Azure templates and secret-setup scripts for ops; run via CI/maintainers only.
- `automation/`: helper scripts such as `import-card-transactions.ps1`; keep idempotent/parameterized.
- `docs/`: product notes/feature drafts; not deployed.

## Build, Test, and Development Commands

- Install front-end deps: `cd client && npm install`.
- Dev server: `npm start` (<http://localhost:4200> with API proxy).
- Build front-end: `npm run build` → `client/dist`.
- Restore/build backend: `dotnet restore server/src/JulliusApi.sln` then `dotnet build ...` (.NET 10 SDK).
- Run API: `dotnet run --project server/src/Jullius.ServiceApi/Jullius.ServiceApi.csproj` (uses `appsettings.Development.json`; keep secrets local).
- Tests: `npm test` (Karma/Jasmine); `dotnet test server/src/JulliusApi.sln /p:CollectCoverage=true` (xUnit/coverlet).

## Coding Style & Naming Conventions

- TypeScript/HTML/SCSS: 2-space indent, UTF-8, trailing newline, single quotes (`client/.editorconfig`); kebab-case folders, PascalCase classes/components.
- C#: nullable + implicit usings on; PascalCase public members, camelCase locals/parameters; async methods end with `Async`; keep controllers thin, push rules to services/domain.
- Logging/config: use Serilog; avoid secrets in logs; env-specific settings stay in `appsettings.*`.

## Testing Guidelines

- Angular: co-locate specs (`*.spec.ts`); mock HTTP; keep async tests deterministic with TestBed; cover template logic/pipes.
- .NET: xUnit with FluentAssertions/Moq; name tests `Method_ShouldExpectation_WhenCondition`; keep suites under `Jullius.Tests/<Area>` with `*Tests.cs`; favor domain/service behavior tests.

## Commit & Pull Request Guidelines

- Commits follow history: `feat: ...`, `fix: ...`, `refactor: ...` with concise Portuguese descriptions; keep changes scoped and tests passing.
- PRs: include summary, linked issue/board card, testing notes (`npm test`, `dotnet test`, manual steps), and screenshots/GIFs for UI changes.
- Flag migrations, API contract shifts, or new env vars; add rollout/rollback notes for data changes.

## Security & Configuration Tips

- Keep secrets out of Git; use `appsettings.Development.json` or user secrets locally; production values live in Azure/GitHub secrets via `infra/`.
- For Firebase keys follow `client/FIREBASE_CONFIG.md`; prefer env-driven config over constants.
- Rotate credentials referenced in `github-secrets.txt` via portals, not commits.
