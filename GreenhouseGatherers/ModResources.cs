using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.IO;
using System.Text.RegularExpressions;

namespace GreenhouseGatherers
{
    public static class ModResources
    {
        private static IMonitor monitor;
        internal static Texture2D emptyStatue;
        internal static Texture2D filledStatue;

        public static void LoadMonitor(IMonitor iMonitor)
        {
            monitor = iMonitor;
        }

        public static IMonitor GetMonitor()
        {
            return monitor;
        }

        public static void LoadAssets(IModHelper helper, string primaryPath)
        {
            emptyStatue = helper.ModContent.Load<Texture2D>(Path.Combine(primaryPath, "empty.png"));
            filledStatue = helper.ModContent.Load<Texture2D>(Path.Combine(primaryPath, "filled.png"));
        }

        public static string SplitCamelCaseText(string input)
        {
            return string.Join(" ", Regex.Split(input, @"(?<!^)(?=[A-Z](?![A-Z]|$))"));
        }
    }
}
