﻿using GreenhouseGatherers.GreenhouseGatherers.API.Interfaces;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenhouseGatherers.GreenhouseGatherers.API
{
    public static class ApiManager
    {
        private static string modID;
        private static IJsonAssetApi jsonAssetApi;
        private static IMonitor monitor = Resources.GetMonitor();

        public static void HookIntoJsonAssets(IModHelper helper)
        {
            // Get modID
            modID = helper.ModRegistry.ModID;

            // Attempt to hook into the IMobileApi interface
            jsonAssetApi = helper.ModRegistry.GetApi<IJsonAssetApi>("spacechase0.JsonAssets");

            if (jsonAssetApi is null)
            {
                monitor.Log("Failed to hook into spacechase0.JsonAssets.", LogLevel.Error);
                return;
            }

            monitor.Log("Successfully hooked into spacechase0.JsonAssets.", LogLevel.Debug);
        }

        public static int GetHarvestStatueID()
        {
            if (jsonAssetApi is null)
            {
                return -1;
            }

            return jsonAssetApi.GetObjectId("Harvest Statue");
        }
    }
}