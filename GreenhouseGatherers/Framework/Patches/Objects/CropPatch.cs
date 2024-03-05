using GreenhouseGatherers.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Crops;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Linq;
using Object = StardewValley.Object;

namespace GreenhouseGatherers.Framework.Patches.Objects
{
    internal class CropPatch : PatchTemplate
    {
        private readonly Type _object = typeof(Crop);

        internal CropPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(Crop.harvest), new[] { typeof(int), typeof(int), typeof(HoeDirt), typeof(JunimoHarvester) }), prefix: new HarmonyMethod(GetType(), nameof(HarvestPrefix)));
        }

        [HarmonyPriority(Priority.Low)]
        internal static bool HarvestPrefix(Crop __instance, Vector2 ___tilePosition, int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null)
        {
            Object cropObj = new Object(__instance.indexOfHarvest, 1);
            string cropName = "Unknown";
            if (cropObj != null)
            {
                cropName = cropObj.DisplayName;
            }

            if (soil is null)
            {
                _monitor.Log($"Crop ({cropName}) at {xTile}, {yTile} is missing HoeDirt, unable to process!", LogLevel.Trace);
                return true;
            }
            if (soil.Location is null)
            {
                _monitor.Log($"Crop ({cropName}) at {xTile}, {yTile} is missing currentLocation (bad GameLocation?), unable to process!", LogLevel.Trace);
                return true;
            }

            if (!soil.Location.objects.Values.Any(o => o.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID)))
            {
                return true;
            }

            // If any farmer is in a location with a Harvest Statue and the farmer is not in bed, skip logic
            if (soil.Location.farmers.Any(f => !f.isInBed))
            {
                return true;
            }

            // Get the nearby HarvestStatue, which will be placing the harvested crop into
            Chest statueObj = soil.Location.objects.Values.First(o => o.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID)) as Chest;

            if (__instance.dead.Value)
            {
                return false;
            }

            bool success = false;
            if (__instance.forageCrop.Value)
            {
                Object o = null;
                Random r2 = Utility.CreateDaySaveRandom(xTile * 1000, yTile * 2000);
                if (__instance.whichForageCrop.Value == "1")
                {
                    o = ItemRegistry.Create<Object>("(O)399");
                }
                else if (__instance.whichForageCrop.Value == "2")
                {
                    soil.shake((float)Math.PI / 48f, (float)Math.PI / 40f, xTile * 64 < Game1.player.Position.X);
                    return false;
                }
                if (Game1.player.professions.Contains(16))
                {
                    o.Quality = 4;
                }
                else if (r2.NextDouble() < (double)(Game1.player.ForagingLevel / 30f))
                {
                    o.Quality = 2;
                }
                else if (r2.NextDouble() < (double)(Game1.player.ForagingLevel / 15f))
                {
                    o.Quality = 1;
                }
                Game1.stats.ItemsForaged += (uint)o.Stack;

                // Try to add the forage crop to the HarvestStatue's inventory
                if (statueObj.addItem(o) != null)
                {
                    // Statue is full, flag it as being eaten
                    statueObj.modData[ModDataKeys.HAS_EATEN_CROPS] = true.ToString();
                }

                return false;
            }
            else if (__instance.currentPhase.Value >= __instance.phaseDays.Count - 1 && (!__instance.fullyGrown || __instance.dayOfCurrentPhase.Value <= 0))
            {
                int fertilizerQualityLevel = soil.GetFertilizerQualityBoostLevel();
                if (string.IsNullOrEmpty(__instance.indexOfHarvest.Value))
                {
                    return false;
                }

                Random r = new Random(xTile * 7 + yTile * 11 + (int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame);
                CropData data = __instance.GetData();
                double chanceForGoldQuality = 0.2 * (Game1.player.FarmingLevel / 10.0) + 0.2 * fertilizerQualityLevel * ((Game1.player.FarmingLevel + 2.0) / 12.0) + 0.01;
                double chanceForSilverQuality = Math.Min(0.75, chanceForGoldQuality * 2.0);

                int cropQuality = 0;
                if (fertilizerQualityLevel >= 3 && r.NextDouble() < chanceForGoldQuality / 2.0)
                {
                    cropQuality = 4;
                }
                else if (r.NextDouble() < chanceForGoldQuality)
                {
                    cropQuality = 2;
                }
                else if (r.NextDouble() < chanceForSilverQuality || fertilizerQualityLevel >= 3)
                {
                    cropQuality = 1;
                }

                cropQuality = MathHelper.Clamp(cropQuality, data?.HarvestMinQuality ?? 0, data?.HarvestMaxQuality ?? cropQuality);
                int numToHarvest = 1;
                if (data != null)
                {
                    int minStack = data.HarvestMinStack;
                    int maxStack = Math.Max(minStack, data.HarvestMaxStack);
                    if (data.HarvestMaxIncreasePerFarmingLevel > 0f)
                    {
                        maxStack += (int)(Game1.player.FarmingLevel * data.HarvestMaxIncreasePerFarmingLevel);
                    }
                    if (minStack > 1 || maxStack > 1)
                    {
                        numToHarvest = r.Next(minStack, maxStack + 1);
                    }
                }

                if (data != null && data.ExtraHarvestChance > 0.0)
                {
                    while (r.NextDouble() < Math.Min(0.9, data.ExtraHarvestChance))
                    {
                        numToHarvest++;
                    }
                }

                Item harvestedItem = __instance.programColored.Value ? new ColoredObject(__instance.indexOfHarvest, 1, __instance.tintColor.Value)
                {
                    Quality = cropQuality
                } : ItemRegistry.Create(__instance.indexOfHarvest.Value, 1, cropQuality);

                if (harvestedItem is not null)
                {
                    if (r.NextDouble() < Game1.player.team.AverageLuckLevel() / 1500.0 + Game1.player.team.AverageDailyLuck() / 1200.0 + 9.9999997473787516E-05)
                    {
                        numToHarvest *= 2;
                    }

                    success = true;
                }

                if (success)
                {
                    if (__instance.indexOfHarvest.Value == "421")
                    {
                        __instance.indexOfHarvest.Value = "431";
                        numToHarvest = r.Next(1, 4);
                    }
                    harvestedItem = __instance.programColored.Value ? new ColoredObject(__instance.indexOfHarvest.Value, 1, __instance.tintColor.Value) : ItemRegistry.Create(__instance.indexOfHarvest.Value);
                    int price = 0;
                    Object obj = harvestedItem as Object;
                    if (obj != null)
                    {
                        price = obj.Price;
                    }
                    float experience = (float)(16.0 * Math.Log(0.018 * price + 1.0, Math.E));
                    if (junimoHarvester == null)
                    {
                        Game1.player.gainExperience(0, (int)Math.Round(experience));
                    }
                    for (int i = 0; i < numToHarvest; i++)
                    {
                        if (statueObj.addItem(harvestedItem.getOne()) != null)
                        {
                            // Statue is full, flag it as being eaten
                            statueObj.modData[ModDataKeys.HAS_EATEN_CROPS] = true.ToString();
                        }
                    }

                    int regrowDays = data?.RegrowDays ?? -1;
                    if (regrowDays > 0)
                    {
                        __instance.fullyGrown.Value = true;
                        if (__instance.dayOfCurrentPhase.Value == regrowDays)
                        {
                            __instance.updateDrawMath(___tilePosition);
                        }
                        __instance.dayOfCurrentPhase.Value = regrowDays;
                    }
                }
            }

            return false;
        }
    }
}
