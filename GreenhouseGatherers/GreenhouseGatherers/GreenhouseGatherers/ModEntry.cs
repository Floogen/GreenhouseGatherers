using GreenhouseGatherers.GreenhouseGatherers.API;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Objects;
using System;
using System.Reflection;
using Harmony;

namespace GreenhouseGatherers.GreenhouseGatherers
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            // Load our Harmony patches
            try
            {
                harmonyPatch();
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patch: {e}", LogLevel.Error);
            }

            // Load the monitor
            Resources.LoadMonitor(this.Monitor);

            // Hook into the game launch
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
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
        }
    }
}
