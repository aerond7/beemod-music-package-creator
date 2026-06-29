# Contributing

Thanks for helping improve BEEmod Music Package Creator (BMPC).

## Development Setup

Requirements:

- Windows
- .NET SDK 8.0.100 or newer
- BEEmod/BEE2 installation for manual app testing

Restore and build:

```powershell
dotnet restore src\BMPC.sln
dotnet build src\BMPC.sln --configuration Release --no-restore
```

Run automated tests:

```powershell
dotnet test src\BMPC.sln --configuration Release --no-build
```

For user-facing workflow changes, also manually verify affected BMPC behavior with a local BEEmod/BEE2 install.

## Coding Expectations

- Keep changes focused and small enough to review.
- Avoid unrelated formatting churn.
- Preserve current install behavior: BMPC runs beside `BEE2.exe`.
- Prefer clear names and simple code over broad rewrites.
- Add or update tests when test projects exist and behavior changes.

## Issues

Open an issue before larger behavior changes, UI changes, package format changes, or compatibility changes. Include enough context for maintainers to reproduce or evaluate the request.

## Pull Requests

Pull requests should include:

- Summary of the change
- Linked issue when applicable
- Build and test results
- Screenshots or short recordings for UI changes
- Documentation updates when user or contributor behavior changes

Before opening a PR, run:

```powershell
dotnet restore src\BMPC.sln
dotnet build src\BMPC.sln --configuration Release --no-restore
dotnet test src\BMPC.sln --configuration Release --no-build
dotnet publish src\BMPC\BMPC.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```
