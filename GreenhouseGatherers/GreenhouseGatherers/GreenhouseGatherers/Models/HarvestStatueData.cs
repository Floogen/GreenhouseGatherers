using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
