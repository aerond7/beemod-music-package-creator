# Release Process

Use this checklist for tagged BMPC releases.

## Before Tagging

1. Pick the next SemVer version, for example `1.3.1`.
2. Update project version metadata:
   - `src/BMPC/BMPC.csproj`
   - `src/BMPC.Core/BMPC.Core.csproj`, if core changes should share the release version
3. Update the matching version changelog in `docs/changelog/X.Y.Z.md`.
4. Run:

```powershell
dotnet restore src\BMPC.sln --source https://api.nuget.org/v3/index.json
dotnet build src\BMPC.sln --configuration Release --no-restore
dotnet test src\BMPC.sln --configuration Release --no-build
dotnet publish src\BMPC\BMPC.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

5. Smoke test the self-contained executable on a clean Windows machine if one is available. At minimum, launch the app and verify import/open-package flows still work beside `BEE2.exe`.

## Tagging

Create an annotated tag from the release commit:

```powershell
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z
```

The `Release` GitHub Actions workflow publishes `src/BMPC/BMPC.csproj` as a self-contained `win-x64` single-file executable, archives the output, uploads a workflow artifact, and creates a GitHub Release for the tag. Use `docs/changelog/X.Y.Z.md` as the release-note source.

## After Tagging

1. Verify the GitHub Release exists.
2. Download the `BMPC-vX.Y.Z-win-x64-self-contained.zip` asset.
3. Extract and smoke test the app on Windows.
4. If the release is broken, delete or mark the release as a pre-release, fix forward, and publish a new patch tag.

## Known Versioning Cleanup

Current project versions are not centralized:

- `src/BMPC/BMPC.csproj` uses `1.3.0`.
- `src/BMPC.Core/BMPC.Core.csproj` uses `1.3.0`.
- `Utils.GetAppVersion()` reads the executing assembly version.

Keep these values intentional during each release. A later cleanup can centralize product versioning in one MSBuild property.
