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
            if (!config.DoJunimosAppearAfterHarvest || e.NewLocation.numberOfObjectsWithName("Harvest Statue") == 0)
            {
                return;
            }

            // Location contains a Harvest Statue, see if we need to spawn Junimos
            HarvestStatue statueObj = e.NewLocation.objects.Pairs.First(p => p.Value.Name == "Harvest Statue").Value as HarvestStatue;
            if (statueObj.hasSpawnedJunimos)
            {
                return;
            }

            // Harvest Statue hasn't spawned some Junimos yet, so spawn a few temp ones for fluff
            statueObj.SpawnJunimos(e.NewLocation, config.MaxAmountOfJunimosToAppearAfterHarvest);
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
            // Add any placed Harvest Statues to our cache
            foreach (var tileObjectPair in e.Added.Where(o => o.Value.ParentSheetIndex == harvestStatueID))
            {
                saveData.SavedStatueData.Add(new HarvestStatueData(e.Location.Name, tileObjectPair.Key));
            }

            // Remove any destroyed Harvest Statues from our cache
            foreach (var tileObjectPair in e.Removed.Where(o => o.Value.ParentSheetIndex == harvestStatueID))
            {
                saveData.SavedStatueData = saveData.SavedStatueData.Where(s => !(s.GameLocation == e.Location.Name && s.Tile.Equals(tileObjectPair.Key))).ToList();
            }
        }

        private void OnIdsAssigned(object sender, EventArgs e)
        {
            // Get the Harvest Statue item ID
            harvestStatueID = ApiManager.GetHarvestStatueID();
        }

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

                // Add the items from HarvestStatue to temp Chest, so the player will still have their items if mod is uninstalled
                Chest chest = new Chest(true, statueData.Tile);
                foreach (var item in statueObj.items)
                {
                    chest.addItem(item);
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

            var saveDataCache = this.Helper.Data.ReadJsonFile<SaveData>(saveDataCachePath);
            if (saveDataCache is null)
            {
                return;
            }

            foreach (var statueData in saveDataCache.SavedStatueData)
            {
                GameLocation location = Game1.getLocationFromName(statueData.GameLocation);

                // Get the items from the temp Chest object
                Chest chest = location.getObjectAtTile((int)statueData.Tile.X, (int)statueData.Tile.Y) as Chest;

                // Add the items from the temp Chest to the HarvestStatue
                HarvestStatue statueObj = new HarvestStatue(statueData.Tile, harvestStatueID, config.EnableHarvestMessage, config.DoJunimosEatExcessCrops, config.DoJunimosHarvestFromPots, config.DoJunimosHarvestFromFruitTrees, config.MinimumFruitOnTreeBeforeHarvest);
                statueObj.AddItems(chest.items);

                // Remove the temp Chest by placing HarvestStatue
                location.setObject(statueData.Tile, statueObj);

                // Gather any crops nearby
                statueObj.HarvestCrops(location);
            }
        }

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
