using System;
using GreenhouseGatherersAutomate.GreenhouseGatherersAutomate.Automate;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace GreenhouseGatherersAutomate.GreenhouseGatherersAutomate
{
    /// <summary>The mod entry point.</summary>
    public class AutomatedModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            // Hook into the game launch
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Check if Pathoschild's Automate is in the current mod list
            if (Helper.ModRegistry.IsLoaded("Pathoschild.Automate"))
            {
                Monitor.Log("Attempting to hook into Pathoschild.Automate.", LogLevel.Debug);
                try
                {
                    IAutomateAPI automateApi = Helper.ModRegistry.GetApi<IAutomateAPI>("Pathoschild.Automate");

                    // Add the AutomationFactory for Harvest Statue
                    automateApi.AddFactory(new HarvestStatueFactory());
                }
                catch (Exception ex)
                {
                    Monitor.Log($"There was an issue with hooking into Pathoschild.Automate: {ex}", LogLevel.Error);
                }
            }
        }
    }
}