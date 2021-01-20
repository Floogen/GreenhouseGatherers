using GreenhouseGatherers.GreenhouseGatherers.API;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Objects;
using System;
using System.Reflection;
using Harmony;
using StardewValley;
using System.Linq;
using GreenhouseGatherers.GreenhouseGatherers.Objects;
using GreenhouseGatherers.GreenhouseGatherers.Models;
using StardewValley.Characters;

namespace GreenhouseGatherers.GreenhouseGatherers
{
    public class ModEntry : Mod
    {
        // Save related
        private SaveData saveData;
        private int harvestStatueID;
        private string saveDataCachePath;

        // Config related
        private ModConfig config;

        public override void Entry(IModHelper helper)
        {
            // Load the monitor
            ModResources.LoadMonitor(this.Monitor);

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

            // Hook into the game launch
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            // Hook into ObjectListChanged event so we can catch when Harvest Statues are placed / removed
            helper.Events.World.ObjectListChanged += this.OnObjectListChanged;

            // Hook into Player.Warped event so we can spawn some Junimos if the area was recently harvested
            helper.Events.Player.Warped += this.OnWarped;

            // Hook into save related events
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.OldLocation.numberOfObjectsWithName("Harvest Statue") > 0 && e.OldLocation.NameOrUniqueName != "CommunityCenter")
            {
                for (int i = e.OldLocation.characters.Count - 1; i >= 0; i--)
                {
                    if (e.OldLocation.characters[i] is Junimo)
                    {
                        e.OldLocation.characters.RemoveAt(i);
                    }
                }
            }

            if (!config.DoJunimosAppearAfterHarvest || e.NewLocation.numberOfObjectsWithName("Harvest Statue") == 0)
            {
                return;
            }

            // Location contains a Harvest Statue, see if we need to spawn Junimos
            HarvestStatue statueObj = e.NewLocation.objects.Pairs.First(p => p.Value.Name == "Harvest Statue").Value as HarvestStatue;
            if (statueObj is null)
            {
                Monitor.Log("Incorrectly attempted to perform Junimo spawning in a location without Harvest Statue!", LogLevel.Trace);
                return;
            }

            if (statueObj.hasSpawnedJunimos)
            {
                return;
            }

            // Harvest Statue hasn't spawned some Junimos yet, so spawn a few temp ones for fluff
            if (e.NewLocation.NameOrUniqueName != "CommunityCenter")
            {
                statueObj.SpawnJunimos(e.NewLocation, config.MaxAmountOfJunimosToAppearAfterHarvest);
            }
        }

        public void harmonyPatch()
        {
            var harmony = HarmonyInstance.Create(this.ModManifest.UniqueID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Check if spacechase0's JsonAssets is in the current mod list
            if (Helper.ModRegistry.IsLoaded("spacechase0.JsonAssets"))
            {
                Monitor.Log("Attempting to hook into spacechase0.JsonAssets.", LogLevel.Debug);
                ApiManager.HookIntoJsonAssets(Helper);
            }

            // Hook into Json Asset's IdsAssigned event
            ApiManager.GetJsonAssetInterface().IdsAssigned += OnIdsAssigned;
        }

        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            if (saveData is null)
            {
                saveData = new SaveData();
            }

            // Add any placed Harvest Statues to our cache
            foreach (var tileObjectPair in e.Added.Where(o => o.Value.ParentSheetIndex == harvestStatueID && !saveData.SavedStatueData.Any(s => s.GameLocation == e.Location.NameOrUniqueName && s.Tile.Equals(o.Key))))
            {
                saveData.SavedStatueData.Add(new HarvestStatueData(e.Location.NameOrUniqueName, tileObjectPair.Key));
            }

            // Remove any destroyed Harvest Statues from our cache
            foreach (var tileObjectPair in e.Removed.Where(o => o.Value.ParentSheetIndex == harvestStatueID))
            {
                saveData.SavedStatueData = saveData.SavedStatueData.Where(s => !(s.GameLocation == e.Location.NameOrUniqueName && s.Tile.Equals(tileObjectPair.Key))).ToList();
            }
        }

        private void OnIdsAssigned(object sender, EventArgs e)
        {
            // Get the Harvest Statue item ID
            harvestStatueID = ApiManager.GetHarvestStatueID();
        }

        [EventPriority(EventPriority.High + 1)]
        private void OnSaving(object sender, SavingEventArgs e)
        {
            // Save the cache
            this.Helper.Data.WriteJsonFile(saveDataCachePath, saveData);

            // Find all the HarvestStatue objects and convert them to a chest
            foreach (HarvestStatueData statueData in saveData.SavedStatueData)
            {
                GameLocation location = Game1.getLocationFromName(statueData.GameLocation);

                // Get the items from the HarvestStatue object
                HarvestStatue statueObj = location.getObjectAtTile((int)statueData.Tile.X, (int)statueData.Tile.Y) as HarvestStatue;
                if (statueObj is null)
                {
                    Monitor.Log($"Harvest Statue at ({statueData.Tile.X}, {statueData.Tile.Y}) was missing, unable to offload items!", LogLevel.Debug);
                    continue;
                }

                // Add the items from HarvestStatue to temp Chest, so the player will still have their items if mod is uninstalled
                Chest chest = new Chest(true, statueData.Tile);
                foreach (var item in statueObj.items)
                {
                    chest.addItem(item);
                }

                // Set the tempChest.modData to statueObj.modData in case the Chests Anywhere mod is used (so we can retain name / category data)
                if (statueObj != null)
                {
                    chest.modData = statueObj.modData;
                }

                // Remove the HarvestStatue by placing the Chest
                location.setObject(statueData.Tile, chest);
            }
            return;
        }

        private void LoadHarvestStatuesFromCache()
        {
            // See if there is an old cache we need to read, otherwise load in a new cache
            saveData = new SaveData();
            saveDataCachePath = $"data/{Constants.SaveFolderName}.json";

            saveData = this.Helper.Data.ReadJsonFile<SaveData>(saveDataCachePath);
            if (saveData is null)
            {
                return;
            }

            foreach (var statueData in saveData.SavedStatueData)
            {
                GameLocation location = Game1.getLocationFromName(statueData.GameLocation);
                if (location is null)
                {
                    Monitor.Log($"Bad location name of {statueData.GameLocation} with coordinates at ({statueData.Tile.X}, {statueData.Tile.Y}), unable to restore items!", LogLevel.Alert);
                    continue;
                }

                // Get the items from the temp Chest object
                Chest chest = location.getObjectAtTile((int)statueData.Tile.X, (int)statueData.Tile.Y) as Chest;
                if (chest is null)
                {
                    Monitor.Log($"Offloaded chest at ({statueData.Tile.X}, {statueData.Tile.Y}) was missing, unable to restore items!", LogLevel.Debug);
                    continue;
                }

                // Add the items from the temp Chest to the HarvestStatue
                HarvestStatue statueObj = new HarvestStatue(statueData.Tile, harvestStatueID, config.EnableHarvestMessage, config.DoJunimosEatExcessCrops, config.DoJunimosHarvestFromPots, config.DoJunimosHarvestFromFruitTrees, config.MinimumFruitOnTreeBeforeHarvest);
                statueObj.AddItems(chest.items);

                // Set the statueObj.modData to tempChest.modData in case the Chests Anywhere mod is used (so we can retain name / category data)
                Chest tempChest = location.getObjectAtTile((int)statueData.Tile.X, (int)statueData.Tile.Y) as Chest;
                if (tempChest != null)
                {
                    statueObj.modData = tempChest.modData;
                }

                // Remove the temp Chest by placing HarvestStatue
                location.setObject(statueData.Tile, statueObj);

                // Gather any crops nearby
                statueObj.HarvestCrops(location);
            }

            // Purge the cache of any invalid locations (duplicated or non-existing)
            saveData.SavedStatueData = saveData.SavedStatueData.Where(s => Game1.getLocationFromName(s.GameLocation) != null).GroupBy(s => s.GameLocation).Select(s => s.First()).ToList();
        }

        [EventPriority(EventPriority.Low)]
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!Game1.MasterPlayer.mailReceived.Contains("WizardHarvestStatueRecipe") && (config.ForceRecipeUnlock || Game1.MasterPlayer.mailReceived.Contains("hasPickedUpMagicInk")))
            {
                Helper.Content.AssetEditors.Add(new RecipeMail());
                Game1.MasterPlayer.mailbox.Add("WizardHarvestStatueRecipe");
            }

            LoadHarvestStatuesFromCache();
        }
    }
}
