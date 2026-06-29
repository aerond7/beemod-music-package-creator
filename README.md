# BEEmod Music Package Creator (BMPC)

BEEmod Music Package Creator is a Windows desktop app for creating and updating custom music packages for BEEmod/BEE2. It converts selected source audio, adds package metadata and icon assets, and imports the generated package into a local BEEmod installation.

BMPC is open source under the MIT License. It is intended for BEEmod/BEE2 users who want a guided way to add custom chamber music, funnel music, gel sound effects, and package artwork without hand-editing BEE package files.

Website: [bmpc.aerond.dev](https://bmpc.aerond.dev)

## BEEmod / BEE2 References

BMPC is an independent companion tool for BEEmod/BEE2 package creation. For BEEmod/BEE2 downloads, source code, items, and project information, see:

- [BEEmod GitHub organization](https://github.com/BEEmod)
- [BEE2.4 application repository](https://github.com/BEEmod/BEE2.4)
- [BEE2.4 application releases](https://github.com/BEEmod/BEE2.4/releases)
- [BEE2 item packages repository](https://github.com/BEEmod/BEE2-items)
- [BEE2 item package releases](https://github.com/BEEmod/BEE2-items/releases)

## Who This Is For

- BEEmod/BEE2 players who want to create custom music packages.
- Music-pack authors who want to edit and re-import existing BMPC packages.
- Contributors who want to improve the app, package format handling, tests, or documentation.

## Requirements

- Windows
- BEEmod/BEE2 installation
- .NET SDK 8.0.100 or newer, only if building from source

Release builds are published as self-contained Windows executables, so a separate .NET Desktop Runtime install is not required for normal use.

BMPC must run from the same folder as `BEE2.exe`. If it starts from another folder, it will show an install-location warning and stop.

## Supported Source Files

BMPC file pickers currently accept:

- Audio: `.wav`, `.mp3`
- Images: `.png`, `.jpg`, `.jpeg`

## Install And Start

1. Download a self-contained BMPC release ZIP from this repository's Releases page, or build BMPC from source.
2. Extract or copy BMPC into the folder that contains `BEE2.exe`.
3. Close BEEmod/BEE2 before creating, editing, or deleting packages.
4. Start BMPC.
5. Reopen BEEmod/BEE2 after BMPC finishes importing a package.

## Create A Music Package

1. Click **New package**.
2. Enter package name and description.
3. Optional: enter a group name. This is the group shown in the BEEmod music selection menu. If left blank, BMPC uses the package name.
4. Click **Add song**.
5. Enter song name, description, and authors.
6. Choose an icon image, or keep the default BMPC icon.
7. Choose a required base track.
8. Optional: choose funnel music, use the default funnel music, or sync funnel music to the base track.
9. Optional: choose speed-gel and bounce-gel sound effects.
10. Add more songs if needed.
11. Review the summary, then click **Create**.

BMPC writes the generated `.bee_pack` file into BEEmod's `packages` folder and stores BMPC edit data under `bmpc/packages`. The edit data lets BMPC reopen packages later for changes.

## Edit Or Remove Packages

The main BMPC window lists packages that BMPC knows how to edit.

- Use **Edit** to change package details or song files, then click **Update**.
- Use **Remove** to delete a BMPC-created package.
- Close BEEmod/BEE2 before editing or removing packages so BMPC can safely replace files.

## Safety Notes

- Keep your original music and image files somewhere safe. BMPC creates package output, but it is not a media library or backup tool.
- Use music you own, created, or have permission to use. BMPC shows this reminder in the app because music copyright still applies to custom packages.
- Avoid creating two packages with the same package name. BMPC turns names into package IDs, so duplicate names can conflict.

## Build From Source

Install the .NET SDK 8.0.100 or newer, then run:

```powershell
dotnet restore src\BMPC.sln
dotnet build src\BMPC.sln --configuration Release --no-restore
```

The WPF app project is `src/BMPC/BMPC.csproj`.

Run tests with:

```powershell
dotnet test src\BMPC.sln --configuration Release --no-build
```

Create the same self-contained executable used for releases with:

```powershell
dotnet publish src\BMPC\BMPC.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup, pull request expectations, and issue guidance.

Maintainer release steps are documented in [docs/release-process.md](docs/release-process.md).

## License

BMPC is licensed under the [MIT License](LICENSE.txt).
