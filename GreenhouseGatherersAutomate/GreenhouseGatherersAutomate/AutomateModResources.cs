using StardewModdingAPI;

namespace GreenhouseGatherersAutomate.GreenhouseGatherersAutomate
{
    public static class AutomateModResources
    {
        private static IMonitor monitor;

        public static void LoadMonitor(IMonitor iMonitor)
        {
            monitor = iMonitor;
        }

        public static IMonitor GetMonitor()
        {
            return monitor;
        }
    }
}
