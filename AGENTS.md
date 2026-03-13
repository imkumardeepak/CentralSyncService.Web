# Repository Guidelines

## Project Structure & Module Organization
`Program.cs` wires the ASP.NET Core MVC app, DI container, logging, and the hosted sync service. `Controllers/` contains MVC endpoints, `Services/` contains orchestration such as `SyncService`, `Core/` holds entities, DTOs, and repository interfaces, and `Infrastructure/` contains SQL Server repository and logging implementations. UI lives in `Views/` and static assets in `wwwroot/`. SQL setup and reporting procedures are in `SQL/`, and supporting documentation lives in `docs/`.

## Build, Test, and Development Commands
- `dotnet restore` restores NuGet packages.
- `dotnet build` compiles the solution and surfaces nullable/package warnings.
- `dotnet run` starts the web app locally using the current environment settings.
- `dotnet watch run` is the fastest loop for Razor/UI changes.

Run commands from the repository root: `D:\PROJECT\HALDIRAM\SORTER_SCAN_FROM_TO\WebService_Report`.

## Coding Style & Naming Conventions
Use 4-space indentation and keep files ASCII unless an existing file already uses Unicode. Follow C# conventions already used here: `PascalCase` for types, methods, controller actions, DTOs, and view models; `_camelCase` for private fields; descriptive action names such as `DailyTransfer` or `ProductionOrderMaterialReport`. Keep controllers thin, put SQL access in `Infrastructure/Repositories`, and prefer async ADO.NET calls with `ConfigureAwait(false)` to match the existing codebase.

## Testing Guidelines
There is currently no test project in this repository. For new work, add tests before refactoring business-critical paths, especially sync orchestration, repository mappings, and reporting calculations. Use a separate `*.Tests` project when tests are introduced, and name tests after behavior, for example `SyncService_StartsLoop_WhenHostedServiceRuns`.

## Commit & Pull Request Guidelines
Recent history follows a conventional style such as `feat(report): ...`, `feat(sync): ...`, and `refactor(box-tracking): ...`. Keep commit messages scoped and action-oriented. Pull requests should include a short summary, affected areas, any SQL/procedure changes, manual verification steps, and screenshots for `Views/` changes.

## Security & Configuration Tips
Do not commit real connection strings, database credentials, or plant IPs. Keep local secrets out of `appsettings*.json` when possible, and verify any schema or stored-procedure change against both the plant-side and central databases before merging.
