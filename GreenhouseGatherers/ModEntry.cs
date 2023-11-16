﻿using GreenhouseGatherers.GreenhouseGatherers.API;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Objects;
using System;
using System.Reflection;
using HarmonyLib;
using StardewValley;
using System.Linq;
using GreenhouseGatherers.GreenhouseGatherers.Objects;
using GreenhouseGatherers.GreenhouseGatherers.Models;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Buildings;
using System.IO;
using GreenhouseGatherers.GreenhouseGatherers.Patches;
using GreenhouseGatherers.GreenhouseGatherers.Patches.Objects;
using StardewValley.Monsters;
using GreenhouseGatherers.Utilities;

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
        internal static readonly string harvestStatuePath = Path.Combine("assets", "HarvestStatue");

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

            // Hook into Content related events
            helper.Events.Content.AssetRequested += OnAssetRequested;

            // Hook into the game launch
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

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
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    data["HarvestStatueRecipe"] = "74 1 390 350 268 150/Home/232/true/null/Harvest Statue";
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

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Check if spacechase0's DynamicGameAssets is in the current mod list
            if (Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets"))
            {
                Monitor.Log("Attempting to hook into spacechase0.DynamicGameAssets.", LogLevel.Debug);
                ApiManager.HookIntoDynamicGameAssets(Helper);

                var contentPack = Helper.ContentPacks.CreateTemporary(
                    Path.Combine(Helper.DirectoryPath, harvestStatuePath),
                    "PeacefulEnd.GreenhouseGatherers.HarvestStatue",
                    "PeacefulEnd.GreenhouseGatherers.HarvestStatue",
                    "Adds craftable Junimo Harvest Statues.",
                    "PeacefulEnd",
                    new SemanticVersion("1.0.0"));

                // Check if furyx639's Expanded Storage is in the current mod list
                if (Helper.ModRegistry.IsLoaded("furyx639.ExpandedStorage"))
                {
                    Monitor.Log("Attempting to hook into furyx639.ExpandedStorage.", LogLevel.Debug);
                }
                else
                {
                    // Add the Harvest Statue purely via Json Assets
                    ApiManager.GetDynamicGameAssetsInterface().AddEmbeddedPack(contentPack.Manifest, Path.Combine(Helper.DirectoryPath, harvestStatuePath));
                }
            }
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
            if (Game1.MasterPlayer.mailReceived.Contains("WizardHarvestStatueRecipe") && !Game1.MasterPlayer.knowsRecipe("HarvestStatueRecipe"))
            {
                Game1.MasterPlayer.craftingRecipes.Add("HarvestStatueRecipe", 0);
            }

            if (!Game1.MasterPlayer.modData.ContainsKey(ModDataKeys.HAS_CONVERTED_OLD_STATUES))
            {
                ConvertOldHarvestStatues();
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
            foreach (Chest chest in location.Objects.Values.Where(o => o.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID)))
            {
                // Reset daily modData flags
                chest.modData[ModDataKeys.HAS_SPAWNED_JUNIMOS] = false.ToString();

                // Gather any crops nearby
                var harvestStatue = new HarvestStatue(chest, location);
                harvestStatue.HarvestCrops(location, config.EnableHarvestMessage, config.DoJunimosEatExcessCrops, config.DoJunimosHarvestFromPots, config.DoJunimosHarvestFromFruitTrees, config.DoJunimosHarvestFromFlowers, config.DoJunimosSowSeedsAfterHarvest, config.MinimumFruitOnTreeBeforeHarvest);
            }
        }

        private void ConvertOldHarvestStatues()
        {
            // Find all chests with the "is-harvest-statue" == "true"
            Monitor.Log("Loading...", LogLevel.Trace);
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
            foreach (Chest chest in location.Objects.Pairs.Where(p => p.Value is Chest).Select(p => p.Value).ToList())
            {
                if (!chest.modData.ContainsKey(ModDataKeys.OLD_HARVEST_STATUE_ID))
                {
                    continue;
                }

                // Add the items from the temp Chest to the HarvestStatue
                var items = chest.Items;
                var modData = chest.modData.Pairs;
                var tileLocation = chest.TileLocation;
                location.removeObject(chest.TileLocation, false);

                var statueItem = ApiManager.GetDynamicGameAssetsInterface().SpawnDGAItem("PeacefulEnd.GreenhouseGatherers.HarvestStatue/HarvestStatue") as Item;
                var wasReplaced = (statueItem as StardewValley.Object).placementAction(location, (int)tileLocation.X * 64, (int)tileLocation.Y * 64, Game1.player);
                Monitor.Log($"Attempting to replace old Harvest Statue at {tileLocation} | Was Replaced: {wasReplaced}", LogLevel.Debug);

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
