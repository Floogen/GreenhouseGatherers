using GreenhouseGatherers.Framework.API;
using GreenhouseGatherers.Framework.Models;
using GreenhouseGatherers.Framework.Objects;
using GreenhouseGatherers.Framework.Patches.Characters;
using GreenhouseGatherers.Framework.Patches.Objects;
using GreenhouseGatherers.Framework.Utilities;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Objects;
using System;
using System.IO;
using System.Linq;

namespace GreenhouseGatherers.GreenhouseGatherers
{
    public class ModEntry : Mod
    {
        // Legacy save related
        private SaveData saveData;
        private string saveDataCachePath;

        // Config related
        private ModConfig config;

        // Asset related
        internal static readonly string harvestStatuePath = Path.Combine("Framework", "Assets");

        public override void Entry(IModHelper helper)
        {
            // Load the monitor
            ModResources.LoadMonitor(this.Monitor);

            // Load assets
            ModResources.LoadAssets(helper, harvestStatuePath);

            // Load our Harmony patches
            try
            {
                harmonyPatch();
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patch: {e}", LogLevel.Error);
                return;
            }

            // Load the config
            this.config = helper.ReadConfig<ModConfig>();

            // Add in our debug commands
            helper.ConsoleCommands.Add("gg_learn_recipe", "Use to force learn the Harvest Statue recipe. \n\nUsage: gg_learn_recipe", delegate { Game1.player.craftingRecipes.Add("HarvestStatueRecipe", 0); });

            // Hook into Content related events
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // Hook into Player.Warped event so we can spawn some Junimos if the area was recently harvested
            helper.Events.Player.Warped += OnWarped;

            // Hook into save related events
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    data["WizardHarvestStatueRecipe"] = "Enclosed you'll find blueprints for a statue imbued with forest magic.^ ^If placed indoors, it allows Junimos to enter buildings and harvest crops.^ ^Use it well...^ ^-M. Rasmodius, Wizard%item craftingRecipe HarvestStatueRecipe %%";
                });
            }
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.OldLocation.objects.Values.Any(o => o.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID)) && e.OldLocation.NameOrUniqueName != "CommunityCenter")
            {
                for (int i = e.OldLocation.characters.Count - 1; i >= 0; i--)
                {
                    if (e.OldLocation.characters[i] is Junimo)
                    {
                        e.OldLocation.characters.RemoveAt(i);
                    }
                }
            }

            if (!config.DoJunimosAppearAfterHarvest || !e.NewLocation.objects.Values.Any(o => o.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID)))
            {
                return;
            }

            // Location contains a Harvest Statue, see if we need to spawn Junimos
            if (e.NewLocation.objects.Values.FirstOrDefault(o => o.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID)) is Chest chest && chest != null)
            {
                if (bool.Parse(chest.modData[ModDataKeys.HAS_SPAWNED_JUNIMOS]))
                {
                    return;
                }

                // Harvest Statue hasn't spawned some Junimos yet, so spawn a few temp ones for fluff
                if (e.NewLocation.NameOrUniqueName != "CommunityCenter")
                {
                    new HarvestStatue(chest, e.NewLocation).SpawnJunimos(config.MaxAmountOfJunimosToAppearAfterHarvest);
                }
            }
        }

        public void harmonyPatch()
        {
            var harmony = new Harmony(this.ModManifest.UniqueID);

            new ObjectPatch(Monitor, Helper).Apply(harmony);
            new ChestPatch(Monitor, Helper).Apply(harmony);
            new CropPatch(Monitor, Helper).Apply(harmony);
            new JunimoPatch(Monitor, Helper).Apply(harmony);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (Game1.MasterPlayer.modData.TryGetValue(ModDataKeys.LAST_INSTALLED_MOD_VERSION, out string version) is false || version != this.ModManifest.Version.ToString())
            {
                Game1.MasterPlayer.modData[ModDataKeys.LAST_INSTALLED_MOD_VERSION] = this.ModManifest.Version.ToString();
            }
        }

        [EventPriority(EventPriority.Low)]
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Game1.MasterPlayer.mailReceived.Contains("WizardHarvestStatueRecipe") && (config.ForceRecipeUnlock || Game1.MasterPlayer.mailReceived.Contains("hasPickedUpMagicInk")))
            {
                Game1.MasterPlayer.mailbox.Add("WizardHarvestStatueRecipe");
            }
            if (Game1.MasterPlayer.mailReceived.Contains("WizardHarvestStatueRecipe") && !Game1.player.knowsRecipe("HarvestStatueRecipe"))
            {
                Game1.player.craftingRecipes.Add("HarvestStatueRecipe", 0);
            }

            if (!Game1.MasterPlayer.modData.ContainsKey(ModDataKeys.HAS_HANDLED_SDV14_MIGRATION))
            {
                ConvertOldHarvestStatues();
                Game1.MasterPlayer.modData[ModDataKeys.HAS_HANDLED_SDV14_MIGRATION] = true.ToString();
            }

            // Do morning harvest
            foreach (GameLocation location in Game1.locations)
            {
                DoMorningHarvest(location);

                if (location.buildings is not null)
                {
                    foreach (Building building in location.buildings)
                    {
                        GameLocation indoorLocation = building.indoors.Value;
                        if (indoorLocation is null)
                        {
                            continue;
                        }

                        DoMorningHarvest(indoorLocation);
                    }
                }
            }
        }

        private void DoMorningHarvest(GameLocation location)
        {
            foreach (Chest chest in location.Objects.Values.Where(o => o.ItemId.Equals(ModDataKeys.HARVEST_STATUE_ITEM_ID)))
            {
                // Reset daily modData flags
                chest.modData[ModDataKeys.HARVEST_STATUE_ID] = true.ToString();
                chest.modData[ModDataKeys.HAS_SPAWNED_JUNIMOS] = false.ToString();
                chest.modData[ModDataKeys.HAS_EATEN_CROPS] = false.ToString();

                // Gather any crops nearby
                var harvestStatue = new HarvestStatue(chest, location);
                harvestStatue.HarvestCrops(location, config.EnableHarvestMessage, config.DoJunimosEatExcessCrops, config.DoJunimosHarvestFromPots, config.DoJunimosHarvestFromFruitTrees, config.DoJunimosHarvestFromFlowers, config.DoJunimosSowSeedsAfterHarvest, config.MinimumFruitOnTreeBeforeHarvest);
            }
        }

        private void ConvertOldHarvestStatues()
        {
            // Find all chests that have the ModData HARVEST_STATUE_ID but are not the custom object HARVEST_STATUE_ITEM_ID
            foreach (GameLocation location in Game1.locations)
            {
                ConvertFlaggedChestsToHarvestStatues(location);

                if (location.buildings is not null)
                {
                    foreach (Building building in location.buildings)
                    {
                        GameLocation indoorLocation = building.indoors.Value;
                        if (indoorLocation is null)
                        {
                            continue;
                        }

                        ConvertFlaggedChestsToHarvestStatues(indoorLocation);
                    }
                }
            }
        }

        private void ConvertFlaggedChestsToHarvestStatues(GameLocation location)
        {
            foreach (Chest chest in location.Objects.Pairs.Where(p => p.Value is Chest chest && chest.ItemId.Equals(ModDataKeys.HARVEST_STATUE_ITEM_ID) is false).Select(p => p.Value).ToList())
            {
                if (!chest.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID))
                {
                    continue;
                }

                // Add the items from the temp Chest to the HarvestStatue
                var items = chest.Items;
                var modData = chest.modData.Pairs;
                var tileLocation = chest.TileLocation;
                location.removeObject(chest.TileLocation, false);

                var statueItem = new StardewValley.Object(tileLocation, ModDataKeys.HARVEST_STATUE_ITEM_ID);
                var wasReplaced = statueItem.placementAction(location, (int)tileLocation.X * 64, (int)tileLocation.Y * 64, Game1.player);
                Monitor.Log($"Attempting to replace old Harvest Statue at {tileLocation} -> Was Replaced: {wasReplaced}", LogLevel.Debug);

                // Move the modData over in case the Chests Anywhere mod is used (so we can retain name / category data)
                if (wasReplaced && location.objects.ContainsKey(tileLocation) && location.objects[tileLocation] is Chest statueObj)
                {
                    foreach (var pair in modData.Where(p => p.Key != ModDataKeys.OLD_HARVEST_STATUE_ID))
                    {
                        statueObj.modData[pair.Key] = pair.Value;
                    }

                    foreach (var item in items.Where(i => i != null))
                    {
                        statueObj.addItem(item);
                    }
                }
            }
        }
    }
}
