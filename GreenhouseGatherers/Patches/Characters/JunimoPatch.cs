using GreenhouseGatherers.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using System;
using System.Linq;

namespace GreenhouseGatherers.GreenhouseGatherers.Patches.Objects
{
    internal class JunimoPatch : PatchTemplate
    {
        private readonly Type _object = typeof(Junimo);

        internal JunimoPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Constructor(typeof(Junimo), null), postfix: new HarmonyMethod(GetType(), nameof(JunimoPostfix)));
        }

        private static void JunimoPostfix(ref NetColor ___color)
        {
            if (Game1.currentLocation is not null && Game1.currentLocation.objects is not null && !Game1.currentLocation.objects.Values.Any(o => o.modData.ContainsKey(ModDataKeys.HARVEST_STATUE_ID)) || Game1.currentLocation.Name == "CommunityCenter")
            {
                return;
            }

            // Set a random color for the Junimo
            if (Game1.random.NextDouble() < 0.01)
            {
                switch (Game1.random.Next(8))
                {
                    case 0:
                        ___color.Value = Color.Red;
                        break;
                    case 1:
                        ___color.Value = Color.Goldenrod;
                        break;
                    case 2:
                        ___color.Value = Color.Yellow;
                        break;
                    case 3:
                        ___color.Value = Color.Lime;
                        break;
                    case 4:
                        ___color.Value = new Color(0, 255, 180);
                        break;
                    case 5:
                        ___color.Value = new Color(0, 100, 255);
                        break;
                    case 6:
                        ___color.Value = Color.MediumPurple;
                        break;
                    case 7:
                        ___color.Value = Color.Salmon;
                        break;
                }
                if (Game1.random.NextDouble() < 0.01)
                {
                    ___color.Value = Color.White;
                }
            }
            else
            {
                switch (Game1.random.Next(8))
                {
                    case 0:
                        ___color.Value = Color.LimeGreen;
                        break;
                    case 1:
                        ___color.Value = Color.Orange;
                        break;
                    case 2:
                        ___color.Value = Color.LightGreen;
                        break;
                    case 3:
                        ___color.Value = Color.Tan;
                        break;
                    case 4:
                        ___color.Value = Color.GreenYellow;
                        break;
                    case 5:
                        ___color.Value = Color.LawnGreen;
                        break;
                    case 6:
                        ___color.Value = Color.PaleGreen;
                        break;
                    case 7:
                        ___color.Value = Color.Turquoise;
                        break;
                }
            }
        }
    }
}
