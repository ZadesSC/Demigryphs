using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using VEF.Apparels;
using Verse;

namespace Demigryphs.VEFBannerMenuPatch
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            new Harmony("pphhyy.demigryphs.vef.banner-menu").PatchAll();
        }
    }

    [HarmonyPatch(typeof(FloatMenuOptionProvider_Equip), "GetSingleOptionFor")]
    public static class FloatMenuOptionProviderEquipPatch
    {
        public static void Postfix(ref FloatMenuOption __result, Thing clickedThing)
        {
            if (__result != null && DemigryphShieldUtility.IsDemigryphShieldItem(clickedThing))
            {
                __result = null;
            }
        }
    }

    [HarmonyPatch(typeof(Building_OutfitStand), "GetFloatMenuOptions")]
    public static class OutfitStandMenuPatch
    {
        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Building_OutfitStand __instance)
        {
            List<FloatMenuOption> options = __result.ToList();
            foreach (Thing item in __instance.HeldItems)
            {
                if (!DemigryphShieldUtility.IsDemigryphShieldItem(item))
                {
                    continue;
                }

                TaggedString equipLabel = "Equip".Translate(item.LabelShort);
                options.RemoveAll(option => option.Label.Contains(equipLabel));
            }

            return options;
        }
    }

    internal static class DemigryphShieldUtility
    {
        internal static bool IsDemigryphShieldItem(Thing thing)
        {
            if (thing is not Apparel_Shield shield)
            {
                return false;
            }

            string defName = shield.def?.defName;
            return defName != null
                && defName.StartsWith("pphhyy_Human_Demigyryph")
                && (defName.EndsWith("Banner") || defName.EndsWith("Shield"));
        }
    }
}
