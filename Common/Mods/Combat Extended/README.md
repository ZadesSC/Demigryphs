# Combat Extended compatibility

Loaded only with `CETeam.CombatExtended`, after CE, from the shared `Common` content for RimWorld 1.6 and 1.7. No extra patch mod is needed. Leave CE's built-in Demigryph patch enabled: these patches check each target and replace existing values or add missing values.

Coverage includes all nine breeds and their melee tools, natural armor body groups, all three leathers, all five human helmets, all eight animal armor pieces through either Animal Apparel: Framework or Animal Gear, and all five shields and five banners through Vanilla Expanded Framework. Eggs, trophies, scenarios and factions do not add combat mechanics requiring CE conversion.

## Values

The starting balance follows CE 16.7.4.0's Demigryph compatibility: breed-specific melee damage, cooldown, penetration and combat chances; leather sharp/blunt stuff power 0.05/0.04; helmet armor multipliers 3–3.75; leather barding multiplier 8; plate barding 4.5–5. Helmets all receive bulk/worn bulk 6/1. Animal armor uses 3/1 for leather and 17.5/3.5 for plate, consistently across both frameworks.

Shields use CE's apparel class, Shield layer, coverage extension and drafted render nodes. Their armor comes from CE material scaling (7–8), with bulk/worn bulk 12/8. Banners use multiplier 1 and bulk/worn bulk 10/4, protect the hands, and retain their pain shock threshold bonus. Every visual variant receives its own render node. The VEF shield comp and weapon tools are removed to avoid running both shield systems. VEF's melee pushback maneuver is unused while the banner is CE apparel, as in CE's built-in conversion.

The obsolete vanilla helmet shooting offsets are removed from all five variants. Optional integrations are guarded by their actual defs. Existing unrelated stats, body groups and mod extensions are retained. Duplicate singleton blocks produced by the upstream shield patch are normalized.

## Attribution and license

The animal tool definitions and compatibility balance are adapted from the CE Team's [Demigryph patches at commit 979aed6f0dfe8d73775f0ef687445c6350bb120b](https://github.com/CombatExtended-Continued/CombatExtended/tree/979aed6f0dfe8d73775f0ef687445c6350bb120b/ModPatches/pphhyy%20Demigryph), distributed in CE 16.7.4.0. These adaptations add conditional replacement, complete optional integration coverage and fix incomplete conversions.

Files in this compatibility folder are licensed under [Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International](https://creativecommons.org/licenses/by-nc-sa/4.0/), following [CE's license](https://github.com/CombatExtended-Continued/CombatExtended/blob/979aed6f0dfe8d73775f0ef687445c6350bb120b/README.md#license). The repository's MIT license continues to apply to its other original content.

## Validation

See `tests/ce` for the disposable-profile game harness and the recorded test matrix. CE currently declares support for 1.6 only; the 1.7 folder mapping is ready, but in-game CE testing on 1.7 requires a compatible CE build.
