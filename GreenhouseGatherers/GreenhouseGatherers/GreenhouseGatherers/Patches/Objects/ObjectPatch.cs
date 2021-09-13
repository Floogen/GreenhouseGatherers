using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Object = StardewValley.Object;

namespace GreenhouseGatherers.GreenhouseGatherers.Patches.Objects
{
    internal class ObjectPatch : PatchTemplate
    {
        private readonly Type _object = typeof(Object);

        internal ObjectPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(Object.DayUpdate), new[] { typeof(GameLocation) }), postfix: new HarmonyMethod(GetType(), nameof(DayUpdatePostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(PlacementActionPrefix)));
        }

        internal static void DayUpdatePostfix(Object __instance, GameLocation location)
        {
            if (__instance.modData.ContainsKey(ModEntry.harvestStatueFlag) && __instance is Chest chest)
            {
                // Gather any crops nearby
                //statueObj.HarvestCrops(location);
            }
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

                if (location.objects.Values.Any(o => o.modData.ContainsKey(ModEntry.harvestStatueFlag)))
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
