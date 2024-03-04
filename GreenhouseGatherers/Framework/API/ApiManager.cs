﻿using GreenhouseGatherers.Framework.API.Interfaces.DynamicGameAssets;
using GreenhouseGatherers.Framework.Utilities;
using StardewModdingAPI;

namespace GreenhouseGatherers.Framework.API
{
    public static class ApiManager
    {
        private static IMonitor monitor = ModResources.GetMonitor();

        private static DynamicGameAssetsApi dynamicGameAssets;

        public static void HookIntoDynamicGameAssets(IModHelper helper)
        {
            // Attempt to hook into the IMobileApi interface
            dynamicGameAssets = helper.ModRegistry.GetApi<DynamicGameAssetsApi>("spacechase0.DynamicGameAssets");

            if (dynamicGameAssets is null)
            {
                monitor.Log("Failed to hook into spacechase0.DynamicGameAssets.", LogLevel.Error);
                return;
            }

            monitor.Log("Successfully hooked into spacechase0.DynamicGameAssets.", LogLevel.Debug);
        }

        public static DynamicGameAssetsApi GetDynamicGameAssetsInterface()
        {
            return dynamicGameAssets;
        }

        public static string GetHarvestStatueModDataFlag()
        {
            return ModDataKeys.HARVEST_STATUE_ID;
        }
    }
}