# In-game validation — 2026-10-26

Tested the final compatibility XML in macOS RimWorld 1.6.4871 rev597 with Combat Extended 16.7.4.0 (`979aed6f0d-rel`). Each run used a fresh disposable profile and the real game executable. All runs completed with zero failed assertions and no detected exceptions, XML/def resolution errors or patch operation failures.

- PASS: Animal Apparel: Framework + VEF; upstream patch enabled (1077 assertions).
- PASS: Animal Gear + VEF; upstream patch enabled (1077 assertions).
- PASS: Animal Apparel: Framework + VEF; upstream patch disabled (1077 assertions).
- PASS: CE alone with Demigryphs; no VEF or animal apparel framework (828 assertions).

The checks include standalone and upstream-first XML replay, idempotence, unique stats and natural armor groups, all nine spawned breeds using CE melee damage, all available apparel equipped with valid materials, exact sharp/blunt armor values, shield coverage/deflection and render frames.

At normal quality, steel helmets resolved to sharp armor 3–3.75 and blunt armor 4.5–5.625. Steel shields resolved to 7–8 sharp and 10.5–12 blunt. Steel animal plate resolved to 4.5–5 sharp and 6.75–7.5 blunt. Demigryph-leather barding resolved to 0.4 sharp / 0.32 blunt, and banners to 0.05 sharp / 0.04 blunt. These are CE armor units, not vanilla percentage values.

The development packager passed XML and both supported-version load-map validation. Its ZIP contains all six CE patch files and excludes the test harness.

RimWorld 1.7 was not tested in-game: installed/current CE declares support only for 1.6. Both versions select the same compatibility folder. These smoke checks do not establish long-term gameplay balance, projectile flight behavior or player menu/recipe interactions.
