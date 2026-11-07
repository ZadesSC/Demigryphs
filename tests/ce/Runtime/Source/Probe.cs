using System;
using System.IO;
using System.Linq;
using System.Xml;
using CombatExtended;
using RimWorld;
using UnityEngine;
using Verse;

[StaticConstructorOnStartup]
public static class CeProbe
{
    public static string Report => Environment.GetEnvironmentVariable("DEMI_CE_REPORT");
    public static bool Enabled => !string.IsNullOrEmpty(Report);
    public static void Check(bool ok, string message)
    {
        File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + message + "\n");
    }
    public static void Say(string text) => File.AppendAllText(Report, text + "\n");
    static CeProbe()
    {
        if (!Enabled) return;
        try
        {
            Say("RimWorld " + VersionControl.CurrentVersionString);
            var mod = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "pphhyy.demigryphsmod");
            CheckXmlReplay(mod);
            Check(mod.foldersToLoadDescendingOrder.Any(f => f.EndsWith("Combat Extended")), "CE folder active");
            var defs = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.defName.StartsWith("pphhyy_")).ToList();
            foreach (var d in defs)
            {
                foreach (var list in new[] { d.statBases, d.equippedStatOffsets })
                    if (list != null) Check(!list.GroupBy(s => s.stat).Any(g => g.Count() > 1), d.defName + " unique stats");
                if (d.race != null)
                {
                    Check(d.tools.Count == 4 && d.tools.All(t => t is ToolCE), d.defName + " four CE tools");
                    Check(d.tools.Cast<ToolCE>().All(t => t.armorPenetrationBlunt > 0), d.defName + " penetration");
                    foreach (var p in d.race.body.AllParts.Where(p => p.depth == BodyPartDepth.Outside))
                        Check(p.groups.Count(g => g.defName == "CoveredByNaturalArmor") == 1, d.defName + " armor coverage " + p.Label);
                }
                if (d.IsApparel)
                {
                    Check(d.GetStatValueAbstract(StatDef.Named("Bulk")) > 0, d.defName + " bulk");
                    Check(d.GetStatValueAbstract(StatDef.Named("WornBulk")) > 0, d.defName + " worn bulk");
                }
                if (d.defName.EndsWith("Shield") || d.defName.EndsWith("Banner"))
                {
                    Check(d.thingClass == typeof(CombatExtended.Apparel_Shield), d.defName + " CE shield class");
                    Check(d.modExtensions.Count(e => e is ShieldDefExtension) == 1, d.defName + " one CE extension");
                    Check(d.GetModExtension<ShieldDefExtension>().shieldCoverage.Count > 0, d.defName + " coverage");
                    Check(d.apparel.RenderNodeProperties.Count == 1, d.defName + " render node");
                    Check(!d.comps.Any(c => c.GetType().FullName.Contains("CompProperties_Shield")), d.defName + " VEF shield comp removed");
                    Check(d.tools.NullOrEmpty(), d.defName + " no melee weapon tools");
                }
            }
            Check(defs.Count(d => d.race != null) == 9, "all nine breeds");
            var mods = LoadedModManager.RunningModsListForReading.Select(m => m.PackageId).ToList();
            Check(defs.Count(d => d.IsApparel && d.defName.StartsWith("pphhyy_Demigryph_")) == (mods.Contains("dylan.animalgear") || mods.Contains("ingendum.animalapparelframework") ? 8 : 0), "expected animal armor integration");
            Check(defs.Count(d => d.defName.EndsWith("Shield") || d.defName.EndsWith("Banner")) == (mods.Contains("oskarpotocki.vanillafactionsexpanded.core") ? 10 : 0), "expected VEF integration");
        }
        catch (Exception e) { Say("FAIL startup " + e); }
    }
    static void CheckXmlReplay(ModContentPack mod)
    {
        var doc = new XmlDocument();
        doc.LoadXml("<Defs/>");
        foreach (var folder in mod.foldersToLoadDescendingOrder)
        {
            string defs = Path.Combine(folder, "Defs");
            if (!Directory.Exists(defs)) continue;
            foreach (var file in Directory.GetFiles(defs, "*.xml", SearchOption.AllDirectories))
            {
                var source = new XmlDocument(); source.Load(file);
                foreach (XmlNode child in source.DocumentElement.ChildNodes)
                    if (child is XmlElement) doc.DocumentElement.AppendChild(doc.ImportNode(child, true));
            }
        }
        var patches = mod.Patches.Where(p => p.sourceFile.Contains("Combat Extended")).ToList();
        Check(patches.Count > 0, "XML replay found local CE patches");
        string raw = doc.OuterXml;
        foreach (var patch in patches) Check(patch.Apply(doc), "standalone XML " + patch.sourceFile);
        string once = doc.OuterXml;
        foreach (var patch in patches) Check(patch.Apply(doc), "repeated XML " + patch.sourceFile);
        Check(once == doc.OuterXml, "standalone patch is idempotent");
        // Reconstruct the raw input, then replay the actual installed CE operations.
        var builtIn = LoadedModManager.RunningModsListForReading.Single(m => m.PackageId == "ceteam.combatextended")
            .Patches.Where(p => p.sourceFile.IndexOf("pphhyy Demigryph", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        doc.LoadXml(raw);
        foreach (var patch in builtIn) patch.Apply(doc);
        foreach (var patch in patches) Check(patch.Apply(doc), "coexistence XML " + patch.sourceFile);
        string combined = doc.OuterXml;
        foreach (var patch in patches) Check(patch.Apply(doc), "coexistence replay " + patch.sourceFile);
        Check(combined == doc.OuterXml, "combined patch is idempotent");
        foreach (XmlElement stats in doc.SelectNodes("Defs/ThingDef/statBases | Defs/ThingDef/equippedStatOffsets"))
            Check(!stats.ChildNodes.OfType<XmlElement>().GroupBy(n => n.Name).Any(g => g.Count() > 1), "XML unique stats " + stats.ParentNode.SelectSingleNode("defName")?.InnerText);
    }

}

public sealed class CeScene : GameComponent
{
    int frames;
    bool done, sceneReady;
    public CeScene(Game game) { }
    public override void GameComponentUpdate()
    {
        if (!CeProbe.Enabled || done || Current.ProgramState != ProgramState.Playing || Find.CurrentMap == null || LongEventHandler.ShouldWaitForEvent || ++frames < 30) return;
        if (sceneReady)
        {
            CeProbe.Say("COMPLETE");
            done = true;
            Application.Quit();
            return;
        }
        try
        {
            Find.TickManager.Pause();
            var map = Find.CurrentMap;
            int i = 0;
            foreach (var kind in DefDatabase<PawnKindDef>.AllDefsListForReading.Where(k => k.race.defName.StartsWith("pphhyy_") && k.race.race != null))
            {
                var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, Faction.OfPlayer, fixedBiologicalAge: 5f));
                GenSpawn.Spawn(pawn, map.Center + new IntVec3(i++ % 9, 0, 4), map);
                CeProbe.Check(pawn.GetStatValue(StatDef.Named("MeleeCritChance")) > 0, pawn.def.defName + " spawned melee stats");
                var target = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
                GenSpawn.Spawn(target, pawn.Position + IntVec3.South, map);
                var verb = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(false).Select(v => v.verb).FirstOrDefault(v => v is Verb_MeleeAttackCE);
                CeProbe.Check(verb != null && verb.GetType().Name.Contains("CE"), pawn.def.defName + " CE melee verb");
                float before = target.health.summaryHealth.SummaryHealthPercent;
                var melee = (Verb_MeleeAttackCE)verb;
                for (int hit = 0; hit < 100 && !target.Dead; hit++)
                {
                    melee.ApplyMeleeDamageToTarget(target);
                }
                CeProbe.Check(target.Dead || target.health.summaryHealth.SummaryHealthPercent < before, pawn.def.defName + " actual melee damage");
            }
            foreach (var d in DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.defName.StartsWith("pphhyy_") && d.IsApparel))
            {
                var stuff = d.stuffCategories.Any(c => c.defName.StartsWith("Metallic")) ? ThingDefOf.Steel : ThingDef.Named("pphhyy_Leather_Demigryph");
                CeProbe.Check(stuff.stuffProps.categories.Any(c => d.stuffCategories.Contains(c)), d.defName + " allowed material");
                CeProbe.Say("MATERIAL " + d.defName + " allowed=" + string.Join(",", d.stuffCategories.Select(c => c.defName)) + " selected=" + stuff.defName);
                var apparel = (Apparel)ThingMaker.MakeThing(d, stuff);
                var kind = d.defName.StartsWith("pphhyy_Demigryph_") ? PawnKindDef.Named("pphhyy_Demigryph") : PawnKindDefOf.Colonist;
                var wearer = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, Faction.OfPlayer, fixedBiologicalAge: kind == PawnKindDefOf.Colonist ? 25f : 5f));
                GenSpawn.Spawn(wearer, map.Center + new IntVec3(i++ % 12, 0, -4 - i / 12), map);
                wearer.apparel.Wear(apparel, false, false);
                CeProbe.Check(apparel.Wearer == wearer, d.defName + " equip");
                float sharp = apparel.GetStatValue(StatDefOf.ArmorRating_Sharp);
                float blunt = apparel.GetStatValue(StatDefOf.ArmorRating_Blunt);
                float expectedSharp = d.defName.EndsWith("Helm") ? (d.defName.Contains("Hunter") ? 3.75f : d.defName == "pphhyy_Human_DemigyryphHelm" ? 3f : 3.5f)
                    : d.defName.EndsWith("Shield") ? (d.defName == "pphhyy_Human_DemigyryphShield" ? 7f : 8f)
                    : d.defName.EndsWith("Banner") ? .05f : d.defName.Contains("leatherArmor") ? .4f : d.defName.Contains("_PlateArmor") ? 4.5f : 5f;
                float expectedBlunt = expectedSharp * (stuff == ThingDefOf.Steel ? 1.5f : .8f);
                CeProbe.Check(Math.Abs(sharp - expectedSharp) < .0001f && Math.Abs(blunt - expectedBlunt) < .0001f, d.defName + " armor sharp=" + sharp + " blunt=" + blunt);
                if (apparel is CombatExtended.Apparel_Shield)
                {
                    wearer.drafter.Drafted = true;
                    var hand = wearer.RaceProps.body.AllParts.First(p => p.def == BodyPartDefOf.Hand);
                    var hit = new DamageInfo(DamageDefOf.Bullet, 2f, 0.001f, -1f, wearer, hand, ThingDef.Named("Bullet_22Short_FMJ"));
                    var after = ArmorUtilityCE.GetAfterArmorDamage(hit, wearer, hand, out bool deflected, out bool reduced, out bool absorbed);
                    CeProbe.Check(after.Def == DamageDefOf.Blunt, d.defName + " CE shield deflects low sharp penetration into blunt impact");
                    CeProbe.Say("SHIELD " + d.defName + " absorbed=" + absorbed + " damage=" + after.Amount + " def=" + after.Def);
                    if (!d.defName.EndsWith("Banner")) CeProbe.Check(absorbed && after.Amount == 0, d.defName + " metal absorbs blunt impact");
                    CeProbe.Check(d.GetModExtension<ShieldDefExtension>().PartIsCoveredByShield(wearer.RaceProps.body.AllParts.First(p => p.def == BodyPartDefOf.Hand), false), d.defName + " hand protected");
                    CeProbe.Check(apparel.SpecialDisplayStats().Any(), d.defName + " info card");
                }
            }
            Find.CameraDriver.SetRootPosAndSize(map.Center.ToVector3Shifted(), 16f);
            sceneReady = true;
            frames = 0;
        }
        catch (Exception e) { CeProbe.Say("FAIL scene " + e); done = true; Application.Quit(); }
    }
}
