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
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace GreenhouseGatherers.GreenhouseGatherers
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            // Load the monitor
            Resources.LoadMonitor(this.Monitor);

            // Load our Harmony patches
            try
            {
                harmonyPatch();
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patch: {e}", LogLevel.Error);
            }

            // Hook into the game launch
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;

            // Hook into save related events
            helper.Events.GameLoop.Saving += this.OnSaving;
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

            ApiManager.GetJsonAssetInterface().IdsAssigned += OnIdsAssigned;
        }

        private void OnIdsAssigned(object sender, EventArgs e)
        {
            Monitor.Log(ApiManager.GetHarvestStatueID().ToString(), LogLevel.Debug);
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            // Find all the HarvestStatue objects and convert them to a chest
            foreach (GameLocation location in Game1.locations.Where(l => !l.IsOutdoors))
            {
                Monitor.Log($"Going through all indoor locations to remove statue [{ApiManager.GetHarvestStatueID()}]...", LogLevel.Debug);
                List<Vector2> tiles = new List<Vector2>();
                foreach (Vector2 statueLocation in location.netObjects.Keys.Where(v => location.netObjects[v].ParentSheetIndex == ApiManager.GetHarvestStatueID()))
                {
                    Monitor.Log($"Removing [{location.netObjects[statueLocation].parentSheetIndex}] statue at [{location.name}] {statueLocation.X},{statueLocation.Y}", LogLevel.Debug);
                    tiles.Add(statueLocation);
                }

                foreach (Vector2 position in tiles)
                {
                    location.netObjects.Remove(position);
                }
            }
        }
    }
}
