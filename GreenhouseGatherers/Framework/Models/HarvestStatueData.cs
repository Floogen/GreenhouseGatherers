using Microsoft.Xna.Framework;

namespace GreenhouseGatherers.Framework.Models
{
    public class HarvestStatueData
    {
        public string GameLocation { get; set; }
        public Vector2 Tile { get; set; }

        public HarvestStatueData(string gameLocation, Vector2 tile)
        {
            GameLocation = gameLocation;
            Tile = tile;
        }
    }
}
