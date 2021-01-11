using Harmony;
using StardewValley;
using System.Reflection;
using StardewModdingAPI;
using GreenhouseGatherers.GreenhouseGatherers.Objects;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using System.Linq;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using Netcode;
using StardewValley.Objects;

namespace GreenhouseGatherers.GreenhouseGatherers.Patches
{
    [HarmonyPatch]
    public class CropPatch
    {
        private static IMonitor monitor = Resources.GetMonitor();

        internal static MethodInfo TargetMethod()
        {
            return AccessTools.Method(typeof(StardewValley.Crop), nameof(StardewValley.Crop.harvest));
        }

        internal static bool Prefix(Crop __instance, int xTile, int yTile, HoeDirt soil, JunimoHarvester junimoHarvester = null)
        {
            if (soil.currentLocation.numberOfObjectsWithName("Harvest Statue") == 0)
            {
                return true;
            }

            // Get the nearby HarvestStatue, which will be placing the harvested crop into
            HarvestStatue statueObj = soil.currentLocation.objects.Pairs.First(p => p.Value.Name == "Harvest Statue").Value as HarvestStatue;

			bool success = false;
			if ((bool)__instance.forageCrop)
			{
				Object o = null;
                System.Random r2 = new System.Random((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2 + xTile * 1000 + yTile * 2000);
				switch ((int)__instance.whichForageCrop)
				{
					case 1:
						o = new Object(399, 1);
						break;
					case 2:
						soil.shake((float)System.Math.PI / 48f, (float)System.Math.PI / 40f, (float)(xTile * 64) < Game1.player.Position.X);
						return false;
				}
				if (Game1.player.professions.Contains(16))
				{
					o.Quality = 4;
				}
				else if (r2.NextDouble() < (double)((float)Game1.player.ForagingLevel / 30f))
				{
					o.Quality = 2;
				}
				else if (r2.NextDouble() < (double)((float)Game1.player.ForagingLevel / 15f))
				{
					o.Quality = 1;
				}
				Game1.stats.ItemsForaged += (uint)o.Stack;

				// Add the forage crop to the HarvestStatue's inventory
				if (statueObj.addItem(o) != null)
                {
					Game1.showRedMessage($"The Harvest Statue at {soil.currentLocation.Name} is full, so the Junimos ate the extra crops!");
				}
			}
			else if (__instance.fullyGrown)
			{
				int numToHarvest = 1;
				int cropQuality = 0;
				int fertilizerQualityLevel = 0;
				if ((int)__instance.indexOfHarvest == 0)
				{
					return true;
				}
				System.Random r = new System.Random(xTile * 7 + yTile * 11 + (int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame);
				switch ((int)soil.fertilizer)
				{
					case 368:
						fertilizerQualityLevel = 1;
						break;
					case 369:
						fertilizerQualityLevel = 2;
						break;
					case 919:
						fertilizerQualityLevel = 3;
						break;
				}
				double chanceForGoldQuality = 0.2 * ((double)Game1.player.FarmingLevel / 10.0) + 0.2 * (double)fertilizerQualityLevel * (((double)Game1.player.FarmingLevel + 2.0) / 12.0) + 0.01;
				double chanceForSilverQuality = System.Math.Min(0.75, chanceForGoldQuality * 2.0);
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
				if ((int)__instance.minHarvest > 1 || (int)__instance.maxHarvest > 1)
				{
					int max_harvest_increase = 0;
					if (__instance.maxHarvestIncreasePerFarmingLevel.Value > 0)
					{
						max_harvest_increase = Game1.player.FarmingLevel / (int)__instance.maxHarvestIncreasePerFarmingLevel;
					}
					numToHarvest = r.Next(__instance.minHarvest, System.Math.Max((int)__instance.minHarvest + 1, (int)__instance.maxHarvest + 1 + max_harvest_increase));
				}
				if ((double)__instance.chanceForExtraCrops > 0.0)
				{
					while (r.NextDouble() < System.Math.Min(0.9, __instance.chanceForExtraCrops))
					{
						numToHarvest++;
					}
				}
				if ((int)__instance.indexOfHarvest == 771 || (int)__instance.indexOfHarvest == 889)
				{
					cropQuality = 0;
				}
				Object harvestedItem = (__instance.programColored ? new ColoredObject(__instance.indexOfHarvest, 1, __instance.tintColor)
				{
					Quality = cropQuality
				} : new Object(__instance.indexOfHarvest, 1, isRecipe: false, -1, cropQuality));

				// Add the crop to the HarvestStatue's inventory
				if (statueObj.addItem(harvestedItem) != null)
				{
					Game1.showRedMessage($"The Harvest Statue at {soil.currentLocation.Name} is full, so the Junimos ate the extra crops!");
				}
				success = true;
				
				if (success)
				{
					if ((int)__instance.indexOfHarvest == 421)
					{
						__instance.indexOfHarvest.Value = 431;
						numToHarvest = r.Next(1, 4);
					}
					int price = System.Convert.ToInt32(Game1.objectInformation[__instance.indexOfHarvest].Split('/')[1]);
					harvestedItem = (__instance.programColored ? new ColoredObject(__instance.indexOfHarvest, 1, __instance.tintColor) : new Object(__instance.indexOfHarvest, 1));
					for (int i = 0; i < numToHarvest - 1; i++)
					{
						Game1.createItemDebris(harvestedItem.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
					}
					if ((int)__instance.indexOfHarvest == 262 && r.NextDouble() < 0.4)
					{
						Object hay_item = new Object(178, 1);
						Game1.createItemDebris(hay_item.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
					}
					else if ((int)__instance.indexOfHarvest == 771)
					{
						Game1.player.currentLocation.playSound("cut");
						if (r.NextDouble() < 0.1)
						{
							Object mixedSeeds_item = new Object(770, 1);
							Game1.createItemDebris(mixedSeeds_item.getOne(), new Vector2(xTile * 64 + 32, yTile * 64 + 32), -1);
						}
					}
					if ((int)__instance.regrowAfterHarvest == -1)
					{
						return false;
					}

					__instance.fullyGrown.Value = true;
					if (__instance.dayOfCurrentPhase.Value == (int)__instance.regrowAfterHarvest)
					{
						__instance.updateDrawMath(soil.currentTileLocation);
					}
					__instance.dayOfCurrentPhase.Value = __instance.regrowAfterHarvest;
				}
			}

			return false;
        }
    }
}
