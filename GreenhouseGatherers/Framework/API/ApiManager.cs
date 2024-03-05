using GreenhouseGatherers.Framework.Utilities;
using StardewModdingAPI;

namespace GreenhouseGatherers.Framework.API
{
    public static class ApiManager
    {
        private static IMonitor monitor = ModResources.GetMonitor();

        public static string GetHarvestStatueModDataFlag()
        {
            return ModDataKeys.HARVEST_STATUE_ID;
        }
    }
}
