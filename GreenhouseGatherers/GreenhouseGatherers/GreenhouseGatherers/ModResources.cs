using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}
