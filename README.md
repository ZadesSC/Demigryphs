# pphhyy's Demigryphs Continued

RimWorld mod adding demigryph animals, equipment, and related compatibility content.

## Supported Versions

RimWorld `1.6` and `1.7` are actively supported in this repository. Both versions load the same content from `Common`, including the optional compatibility folders. No separate `1.6` or `1.7` folder is needed.

Older version folders are kept for historical reference and should not be modified unless there is a specific reason to do so.

## Repository Layout

- `Common` contains the active defs, patches, and shared assets loaded for both supported versions.
- `Common/Mods/VEF` contains compatibility content for Vanilla Expanded Framework features such as banners and shields.
- `Common/Mods/Animal Armor Framework` contains the Animal Apparel: Framework compatibility path.
- `Common/Mods/Animal Gear` contains the Animal Gear compatibility path.

`LoadFolders.xml` controls which optional compatibility folders are loaded.

## Development Builds

The [Build dev package workflow](https://github.com/ZadesSC/Demigryphs/actions/workflows/dev-build.yml) creates a downloadable artifact for every push to `dev` and pull request targeting `dev`. Artifacts are retained for 30 days and contain an installable ZIP, `SHA256SUMS`, and `BUILD_INFO.json` recording the commit and package/DLL checksums.

Open a completed workflow run and download its `Demigryphs-dev-...` artifact. Extract the artifact, then extract its mod ZIP into RimWorld's `Mods` folder so that `Mods/Demigryphs/About/About.xml` exists. Enable the local mod in the game.

The package includes `About`, `Common`, `LoadFolders.xml`, the license, and this README. Historical versions and Workshop editing files are excluded. The workflow validates XML and shared load mappings and packages the committed VEF patch DLL; it does not recompile that DLL or run RimWorld. In-game testing remains separate.

To create the same package locally with Python 3, run `python3 scripts/package_dev.py`. The output is written to `dist/`.

## Notes

- Animal Apparel: Framework and Animal Gear are supported as alternative compatibility paths, not as simultaneous active frameworks.
- Combat Extended currently provides its own built-in compatibility patch for this mod.
