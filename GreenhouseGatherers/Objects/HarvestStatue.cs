﻿using GreenhouseGatherers.Framework.Extensions;
using GreenhouseGatherers.Utilities;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;

namespace GreenhouseGatherers.GreenhouseGatherers.Objects
{
    public class HarvestStatue
    {
        private IMonitor monitor = ModResources.GetMonitor();

        // Storage related
        private Chest chest;
        private GameLocation location;

        // Statue related
        public bool isFull = false;
        public bool harvestedToday = false;
        public bool hasSpawnedJunimos = false;
        public List<Vector2> harvestedTiles = new List<Vector2>();

        // Config related
        private bool enableHarvestMessage = true;
        private bool doJunimosEatExcessCrops = true;
        private bool doJunimosHarvestFromPots = true;
        private bool doJunimosHarvestFromFruitTrees = true;
        private bool doJunimosHarvestFromFlowers = true;
        private bool doJunimosSowSeedsAfterHarvest = true;
        private int minimumFruitOnTreeBeforeHarvest = 3;

        // Graphic related
        private int currentSheetIndex;

        public HarvestStatue()
        {

        }

        public HarvestStatue(Chest storage, GameLocation gameLocation)
        {
            chest = storage;
            location = gameLocation;
        }

        public void SpawnJunimos(int maxJunimosToSpawn = -1)
        {
            if (!harvestedToday || harvestedTiles.Count == 0)
            {
                return;
            }

            if (maxJunimosToSpawn == -1)
            {
                maxJunimosToSpawn = harvestedTiles.Count / 2;
            }

            int junimosToSpawnUpper = System.Math.Min(harvestedTiles.Count, maxJunimosToSpawn);
            for (int x = 0; x < Game1.random.Next(junimosToSpawnUpper / 4, junimosToSpawnUpper); x++)
            {
                Vector2 tile = location.getRandomTile();

                if (!location.isTileLocationTotallyClearAndPlaceable(tile) || !(location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Type", "Back") == "Wood" || location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Type", "Back") == "Stone"))
                {
                    continue;
                }

                Junimo j = new Junimo(tile * 64f, 6, false);
                if (!location.isCollidingPosition(j.GetBoundingBox(), Game1.viewport, j))
                {
                    location.characters.Add(j);
                }

                //monitor.Log($"Spawning some Junimos at {location.Name}: {tile.X}, {tile.Y}.", LogLevel.Debug);
            }
            Game1.playSound("tinyWhip");

            hasSpawnedJunimos = true;
        }

        public void HarvestCrops(GameLocation location, bool enableHarvestMessage = true, bool doJunimosEatExcessCrops = true, bool doJunimosHarvestFromPots = true, bool doJunimosHarvestFromFruitTrees = true, bool doJunimosHarvestFromFlowers = true, bool doJunimosSowSeedsAfterHarvest = false, int minimumFruitOnTreeBeforeHarvest = 3)
        {
            // Set configs
            this.enableHarvestMessage = enableHarvestMessage;
            this.doJunimosEatExcessCrops = doJunimosEatExcessCrops;
            this.doJunimosHarvestFromPots = doJunimosHarvestFromPots;
            this.doJunimosHarvestFromFruitTrees = doJunimosHarvestFromFruitTrees;
            this.doJunimosHarvestFromFlowers = doJunimosHarvestFromFlowers;
            this.doJunimosSowSeedsAfterHarvest = doJunimosSowSeedsAfterHarvest;
            this.minimumFruitOnTreeBeforeHarvest = minimumFruitOnTreeBeforeHarvest;

            string locationName = ModResources.SplitCamelCaseText(location.Name);
            // Check if we're at capacity and that Junimos aren't allowed to eat excess crops
            if (chest.Items.Count >= chest.GetActualCapacity() && !doJunimosEatExcessCrops)
            {
                Game1.showRedMessage($"The Junimos at the {locationName} couldn't harvest due to lack of storage!");
                return;
            }

            // Look and harvest for crops & forage products on the ground
            monitor.Log("Searching for crops and forage products on ground...", LogLevel.Trace);
            SearchForGroundCrops(location);

            // Look and harvest for crops & forage products inside IndoorPots
            if (doJunimosHarvestFromPots)
            {
                monitor.Log("Searching for crops and forage products within Garden Pots...", LogLevel.Trace);
                SearchForIndoorPots(location);
            }

            if (doJunimosHarvestFromFruitTrees)
            {
                monitor.Log("Searching for fruits from Fruit Trees...", LogLevel.Trace);
                SearchForFruitTrees(location);
            }

            if (isFull)
            {
                Game1.showRedMessage($"The Junimos at the {locationName} couldn't harvest due to lack of storage!");
                return;
            }

            // Check if the Junimos ate the crops due to no inventory space
            if (bool.Parse(chest.modData[ModDataKeys.HAS_EATEN_CROPS]))
            {
                Game1.showRedMessage($"The Junimos at the {locationName} ate harvested crops due to lack of storage!");
                return;
            }

            if (harvestedToday && enableHarvestMessage)
            {
                // Let the player know we harvested
                Game1.addHUDMessage(new HUDMessage($"The Junimos at the {locationName} have harvested crops.", 2));
                return;
            }
        }

        private bool HasRoomForHarvest()
        {
            if (chest.Items.Count >= chest.GetActualCapacity() && !doJunimosEatExcessCrops)
            {
                return false;
            }

            return true;
        }

        private void AttemptSowSeed(string seedIndex, HoeDirt hoeDirt, Vector2 tile)
        {
            // -74 == Object.SeedsCategory
            Item seedItem = chest.Items.FirstOrDefault(i => i != null && i.Category == -74 && i.ItemId == seedIndex);
            if (seedItem != null)
            {
                // Remove one seed from the stack, or the whole item if it is the last seed of the stack
                seedItem.Stack -= 1;

                if (seedItem.Stack == 0)
                {
                    chest.Items.Remove(seedItem);
                }

                // Plant the seed on the ground
                //hoeDirt.crop = new Crop(seedIndex, (int)tile.X, (int)tile.Y);
                hoeDirt.plant(seedIndex, Game1.MasterPlayer, false);
            }
        }

        private void SearchForGroundCrops(GameLocation location)
        {
            // Search for crops
            foreach (KeyValuePair<Vector2, TerrainFeature> tileToHoeDirt in location.terrainFeatures.Pairs.Where(p => p.Value is HoeDirt && (p.Value as HoeDirt).crop != null))
            {
                if (!HasRoomForHarvest())
                {
                    isFull = true;
                    return;
                }

                Vector2 tile = tileToHoeDirt.Key;
                HoeDirt hoeDirt = (tileToHoeDirt.Value as HoeDirt);

                Crop crop = hoeDirt.crop;
                if (!hoeDirt.readyForHarvest())
                {
                    // Crop is either not fully grown or it has not regrown since last harvest
                    //monitor.Log($"Crop at ({tile.X}, {tile.Y}) is not ready for harvesting: {crop.forageCrop} | {crop.regrowAfterHarvest} | {crop.dayOfCurrentPhase}, {crop.currentPhase}", LogLevel.Debug);
                    continue;
                }
                //monitor.Log($"Harvesting crop ({tile.X}, {tile.Y}): {crop.forageCrop} | {crop.regrowAfterHarvest} | {crop.dayOfCurrentPhase}, {crop.currentPhase}", LogLevel.Debug);

                if (!doJunimosHarvestFromFlowers && new Object(tile, crop.indexOfHarvest.Value).Category == -80)
                {
                    // Crop is flower and config has been set to skip them
                    continue;
                }

                // Crop exists and is fully grown, attempt to harvest it
                crop.harvest((int)tile.X, (int)tile.Y, hoeDirt, null);
                harvestedToday = true;
                harvestedTiles.Add(tile);

                // Clear any non-renewing crop
                if (crop.RegrowsAfterHarvest() is false)
                {
                    var seedIndex = crop.netSeedIndex.Value;
                    hoeDirt.crop = null;

                    // Attempt to replant, if it is enabled and has valid seed
                    if (doJunimosSowSeedsAfterHarvest)
                    {
                        AttemptSowSeed(seedIndex, hoeDirt, tile);
                    }
                }
            }

            // Search for forage products
            List<Vector2> tilesToRemove = new List<Vector2>();
            foreach (KeyValuePair<Vector2, Object> tileToForage in location.objects.Pairs.Where(p => p.Value.isForage()))
            {
                if (!HasRoomForHarvest())
                {
                    isFull = true;
                    return;
                }

                Vector2 tile = tileToForage.Key;
                if (chest.addItem(tileToForage.Value.getOne()) != null)
                {
                    chest.modData[ModDataKeys.HAS_EATEN_CROPS] = true.ToString();
                }

                tilesToRemove.Add(tile);
                harvestedToday = true;
                harvestedTiles.Add(tile);
            }

            // Clean up the harvested forage products
            tilesToRemove.ForEach(t => location.removeObject(t, false));
        }

        private void SearchForIndoorPots(GameLocation location)
        {
            // Search for IndoorPots with crops
            foreach (KeyValuePair<Vector2, Object> tileToIndoorPot in location.objects.Pairs.Where(p => p.Value is IndoorPot))
            {
                if (!HasRoomForHarvest())
                {
                    isFull = true;
                    return;
                }

                Vector2 tile = tileToIndoorPot.Key;
                IndoorPot pot = tileToIndoorPot.Value as IndoorPot;
                HoeDirt hoeDirt = pot.hoeDirt.Value;

                // HoeDirt seems to be missing its currentLocation when coming from IndoorPots, which is problematic for Crop.harvest()
                if (hoeDirt.Location is null)
                {
                    hoeDirt.Location = location;
                }

                if (hoeDirt.readyForHarvest())
                {
                    if (!doJunimosHarvestFromFlowers && new Object(tile, hoeDirt.crop.indexOfHarvest.Value).Category == -80)
                    {
                        // Crop is flower and config has been set to skip them
                        continue;
                    }

                    hoeDirt.crop.harvest((int)tile.X, (int)tile.Y, hoeDirt, null);
                    harvestedToday = true;

                    // Clear any non-renewing crop
                    if (hoeDirt.crop.RegrowsAfterHarvest() is false)
                    {
                        var seedIndex = hoeDirt.crop.netSeedIndex.Value;
                        hoeDirt.crop = null;

                        // Attempt to replant, if it is enabled and has valid seed
                        if (doJunimosSowSeedsAfterHarvest)
                        {
                            AttemptSowSeed(seedIndex, hoeDirt, tile);
                        }
                    }
                }
            }

            // Search for IndoorPots with forage items
            foreach (KeyValuePair<Vector2, Object> tileToIndoorPot in location.objects.Pairs.Where(p => p.Value is IndoorPot))
            {
                if (!HasRoomForHarvest())
                {
                    isFull = true;
                    return;
                }

                Vector2 tile = tileToIndoorPot.Key;
                IndoorPot pot = tileToIndoorPot.Value as IndoorPot;

                if (pot.heldObject.Value != null && pot.heldObject.Value.isForage())
                {
                    if (chest.addItem(pot.heldObject.Value.getOne()) != null)
                    {
                        chest.modData[ModDataKeys.HAS_EATEN_CROPS] = true.ToString();
                    }

                    pot.heldObject.Value = null;
                    harvestedToday = true;
                }
            }
        }

        private void SearchForFruitTrees(GameLocation location)
        {
            // Search for fruit trees
            if (minimumFruitOnTreeBeforeHarvest > 3)
            {
                minimumFruitOnTreeBeforeHarvest = 3;
            }

            foreach (KeyValuePair<Vector2, TerrainFeature> tileToFruitTree in location.terrainFeatures.Pairs.Where(p => p.Value is FruitTree && (p.Value as FruitTree).fruit.Count >= minimumFruitOnTreeBeforeHarvest).ToList())
            {
                if (!HasRoomForHarvest())
                {
                    isFull = true;
                    return;
                }

                Vector2 tile = tileToFruitTree.Key;
                FruitTree fruitTree = (tileToFruitTree.Value as FruitTree);

                for (int i = 0; i < fruitTree.fruit.Count; i++)
                {
                    var fruit = fruitTree.fruit[i];
                    fruit.Quality = fruitTree.GetQuality();

                    if (chest.addItem(fruit) is not null)
                    {
                        chest.modData[ModDataKeys.HAS_EATEN_CROPS] = true.ToString();

                        fruitTree.fruit[i] = null;
                    }
                }

                harvestedToday = true;
            }
        }

        public void AddItems(NetObjectList<Item> items)
        {
            foreach (var item in items)
            {
                chest.addItem(item);
            }
        }
    }
}
