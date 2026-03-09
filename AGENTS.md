# Repository Guidelines

## Project Structure & Module Organization

- `src/ConfigVault.Api/`: ASP.NET Core API (controllers, middleware, SSE endpoints). Entry point: `Program.cs`.
- `src/ConfigVault.Core/`: Core client/services and DI extensions used by the API and consumers.
- `src/ConfigVault.Tests/`: xUnit tests for API + core.
- `sdks/`: Client SDKs for `csharp/`, `python/`, and `typescript/` (each has its own `src/` + `tests/`).
- `scripts/`: Maintenance scripts (Docker smoke checks, multi-arch checks).
- `docs/`, `artifacts/`: Design notes/plans and build artifacts. Ignore generated `bin/` and `obj/`.

## Build, Test, and Development Commands

- `dotnet --info`: Verify the SDK pinned by `global.json` (service targets `net10.0`; C# SDK targets `net8.0`).
- `dotnet restore ConfigVault.sln`: Restore packages.
- `dotnet build ConfigVault.sln -c Release`: Build the API, core, and tests.
- `dotnet test ConfigVault.sln`: Run all .NET tests (coverlet collector is enabled in test projects). If you hit `MSB3491` writing `.msCoverageSourceRootsMapping_*` under `bin/Release`, re-run with `-c Debug` or clean `bin/Release` for the failing test project.
- `dotnet run --project src/ConfigVault.Api`: Run API locally.
- `docker compose up --build`: Run API container on `http://localhost:8083` (see `docker-compose.yml`).

SDKs:
- Python: `cd sdks/python && pip install -e ".[dev]" && pytest`
- TypeScript: `cd sdks/typescript && npm ci && npm test && npm run build`

## Coding Style & Naming Conventions

- C#: 4-space indentation, `Nullable` enabled, `ImplicitUsings` enabled.
- Naming: `PascalCase` for types/methods, `camelCase` for locals/parameters, interfaces prefixed with `I`.
- Async APIs should be `Task`-based and end with `Async`.

## Testing Guidelines

- .NET: xUnit + FluentAssertions + Moq; name files `*Tests.cs` and keep tests close to the unit under test.
- TypeScript: Vitest; name files `*.test.ts`.
- Python: Pytest (with `pytest-asyncio`); name files `test_*.py`.

## Commit & Pull Request Guidelines

- Prefer Conventional Commits as used in history: `feat(scope): ...`, `fix: ...`, `docs: ...`, `chore: ...`, `test(scope): ...`.
- PRs should include: a short "why", what changed, how to test (commands or curl examples), and any config changes (no secrets).

## Security & Configuration Tips

- Never commit API keys or Vault credentials. Use `appsettings.Development.json`, environment variables (e.g. `ConfigVault__ApiKeys__0`), or secret managers.
- Key format is hierarchical (`namespace/path/to/key`) and maps to Vaultwarden folders/items; validate any changes with an end-to-end test or docker compose run.
