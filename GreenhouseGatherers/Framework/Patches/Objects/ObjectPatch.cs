using GreenhouseGatherers.Framework.Utilities;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Linq;
using Object = StardewValley.Object;

namespace GreenhouseGatherers.Framework.Patches.Objects
{
    internal class ObjectPatch : PatchTemplate
    {
        private readonly Type _object = typeof(Object);

        internal ObjectPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(PlacementActionPrefix)));
        }

        [HarmonyBefore(new string[] { "spacechase0.DynamicGameAssets", "furyx639.ExpandedStorage" })]
        internal static bool PlacementActionPrefix(Object __instance, GameLocation location, int x, int y, Farmer who = null)
        {
            if (__instance.DisplayName == "Harvest Statue")
            {
                if (location.IsOutdoors)
                {
                    _monitor.Log("Attempted to place Harvest Statue outdoors!", LogLevel.Trace);
                    Game1.showRedMessage("Harvest Statues can only be placed indoors!");

                    return false;
                }

                if (location.objects.Values.Any(o => o.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID)))
                {
                    _monitor.Log("Attempted to place another Harvest Statue where there already is one!", LogLevel.Trace);
                    Game1.showRedMessage("You can only place one Harvest Statue per building!");

                    return false;
                }
            }

            return true;
        }
    }
}
