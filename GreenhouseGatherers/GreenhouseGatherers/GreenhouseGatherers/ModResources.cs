﻿using StardewModdingAPI;
using System.Text.RegularExpressions;

namespace GreenhouseGatherers
{
    public static class ModResources
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

        public static string SplitCamelCaseText(string input)
        {
            return string.Join(" ", Regex.Split(input, @"(?<!^)(?=[A-Z](?![A-Z]|$))"));
        }
    }
}
