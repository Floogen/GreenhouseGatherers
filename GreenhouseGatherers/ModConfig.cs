namespace GreenhouseGatherers.GreenhouseGatherers
{
    public class ModConfig
    {
        public bool DoJunimosEatExcessCrops { get; set; }
        public bool DoJunimosHarvestFromPots { get; set; }
        public bool DoJunimosAppearAfterHarvest { get; set; }
        public bool DoJunimosHarvestFromFruitTrees { get; set; }
        public bool DoJunimosHarvestFromFlowers { get; set; }
        public bool DoJunimosSowSeedsAfterHarvest { get; set; }
        public bool ForceRecipeUnlock { get; set; }
        public bool EnableHarvestMessage { get; set; }

        public int MaxAmountOfJunimosToAppearAfterHarvest { get; set; }
        public int MinimumFruitOnTreeBeforeHarvest { get; set; }

        public ModConfig()
        {
            DoJunimosEatExcessCrops = true;
            DoJunimosHarvestFromPots = true;
            DoJunimosAppearAfterHarvest = true;
            DoJunimosHarvestFromFruitTrees = true;
            DoJunimosHarvestFromFlowers = true;
            DoJunimosSowSeedsAfterHarvest = true;

            ForceRecipeUnlock = false;
            EnableHarvestMessage = true;

            MaxAmountOfJunimosToAppearAfterHarvest = -1;
            MinimumFruitOnTreeBeforeHarvest = 3;
        }
    }
}
