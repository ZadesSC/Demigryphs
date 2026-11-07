# Combat Extended game checks

`run_game.py` builds a temporary test mod and launches the real macOS RimWorld 1.6 executable with `-quicktest`, a disposable save profile and only the required mods. It does not load player saves or change the Workshop mods. A .NET SDK and the locally installed game, Harmony, CE and selected optional mods are required. Generated test DLLs are ignored and are not shipped in the mod package.

```sh
python3 tests/ce/run_game.py --framework apparel
python3 tests/ce/run_game.py --framework gear
python3 tests/ce/run_game.py --framework apparel --without-built-in
python3 tests/ce/run_game.py --framework none --without-vef
```

Use `--game` and `--workshop` to override the Steam paths. `--without-built-in` creates a private CE load map omitting only CE's built-in Demigryph patches; all other CE content remains unchanged. Temporary symlinks in the game's mod folder are removed on exit. Do not run multiple instances simultaneously.

Each run writes `report.txt`, `Player.log` and `stdout.log` into a printed temporary directory, and fails if checks fail, the game logs exceptions/def errors, or the scene does not finish. The test mod checks:

- Actual selected load folders and every breed's resolved CE tools, penetration, stats and body armor groups.
- Real Verse XML patch replay with and without upstream operations, including a second application to check idempotence and unique stat entries.
- Spawned adult demigryphs using CE's melee damage calculation against live pawns.
- Equipping every available armor piece, resolving material-derived sharp/blunt armor and bulk.
- Every shield/banner's CE class, coverage, extension, render node, info card and removal of VEF's shield comp/weapon tools.
- CE sharp deflection on a covered hand, with metal shields absorbing the resulting impact and fabric banners allowing blunt impact through, followed by game frames with the test pawns in view.

These are deterministic compatibility/smoke checks, not a statistical balance study. The melee test invokes CE's damage method directly, and the shield test invokes CE's armor calculation; projectile flight, player menu interactions, recipe work and long colony play are not automated here.

CE 16.7.4.0 declares only RimWorld 1.6 support. The identical 1.7 load mapping is checked during packaging, but running CE against 1.7 is deferred until a compatible CE build is available.
