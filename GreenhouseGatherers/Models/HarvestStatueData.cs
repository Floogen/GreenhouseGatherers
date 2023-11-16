using Microsoft.Xna.Framework;

namespace GreenhouseGatherers.GreenhouseGatherers.Models
{
    public class HarvestStatueData
    {
        public string GameLocation { get; set; }
        public Vector2 Tile { get; set; }

        public HarvestStatueData(string gameLocation, Vector2 tile)
        {
            this.GameLocation = gameLocation;
            this.Tile = tile;
        }
    }
}
