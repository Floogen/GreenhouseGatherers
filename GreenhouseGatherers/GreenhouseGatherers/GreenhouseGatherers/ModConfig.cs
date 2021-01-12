using GreenhouseGatherers.GreenhouseGatherers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenhouseGatherers.GreenhouseGatherers
{
    public class ModConfig
    {
        public bool DoJunimosEatExcessCrops { get; set; }
        public bool DoJunimosHarvestFromPots { get; set; }
        public bool DoJunimosAppearAfterHarvest { get; set; }
        public bool DoJunimosHarvestFromFruitTrees { get; set; }

        public int MaxAmountOfJunimosToAppearAfterHarvest { get; set; }

        public ModConfig()
        {
            DoJunimosEatExcessCrops = true;
            DoJunimosHarvestFromPots = true;
            DoJunimosAppearAfterHarvest = true;
            DoJunimosHarvestFromFruitTrees = true;

            MaxAmountOfJunimosToAppearAfterHarvest = -1;
        }
    }
}
