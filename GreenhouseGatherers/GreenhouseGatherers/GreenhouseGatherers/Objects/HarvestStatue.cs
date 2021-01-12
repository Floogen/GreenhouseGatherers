using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace GreenhouseGatherers.GreenhouseGatherers.Objects
{
	[XmlInclude(typeof(HarvestStatue))]
	public class HarvestStatue : StardewValley.Objects.Chest
    {
		private IMonitor monitor = ModResources.GetMonitor();

		public bool ateCrops = false;
		public bool harvestedToday = false;
		public bool hasSpawnedJunimos = false;
		public List<Vector2> harvestedTiles = new List<Vector2>();

		// Config related
		private bool doJunimosEatExcessCrops = true;
		private bool doJunimosHarvestFromPots = true;

		protected override void initNetFields()
		{
			base.initNetFields();
		}

		public override int maximumStackSize()
		{
			return 1;
		}

		public HarvestStatue()
		{

		}

		public HarvestStatue(Vector2 position, int itemID, bool doJunimosEatExcessCrops = true, bool doJunimosHarvestFromPots = true) : base(true, position, itemID)
		{
			this.Name = "Harvest Statue";
			this.doJunimosEatExcessCrops = doJunimosEatExcessCrops;
			this.doJunimosHarvestFromPots = doJunimosHarvestFromPots;

			base.type.Value = "Crafting";
			base.startingLidFrame.Value = itemID;
			base.bigCraftable.Value = true;
			base.canBeSetDown.Value = true;
		}

		public void SpawnJunimos(GameLocation location, int maxJunimosToSpawn = -1)
        {
			if (!harvestedToday || harvestedTiles.Count == 0)
            {
				return;
            }

			if (maxJunimosToSpawn == -1)
            {
				maxJunimosToSpawn = harvestedTiles.Count / 2;
			}

			for (int x = 0; x < Game1.random.Next(1, maxJunimosToSpawn); x++)
            {
				Vector2 tile = location.getRandomTile();

				if (location.isWaterTile((int)tile.X, (int)tile.Y) || !location.isTileLocationTotallyClearAndPlaceable(tile))
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

		public void HarvestCrops(GameLocation location)
        {
			// Check if we're at capacity and that Junimos aren't allowed to eat excess crops
			if (this.items.Count >= this.GetActualCapacity() && !doJunimosEatExcessCrops)
            {
				Game1.showRedMessage($"The Junimos at the {location.Name} couldn't harvest due to lack of storage!");
			}

			// Look and harvest for crops & forage products on the ground
			monitor.Log("Searching for crops and forage products on ground...", LogLevel.Trace);
			SearchForGroundCrops(location);

			// Look and harvest for crops & forage products inside IndoorPots
			if (doJunimosHarvestFromPots)
            {
				monitor.Log("Searching for crops and forage products within IndoorPots...", LogLevel.Trace);
				SearchForIndoorPots(location);
			}

			// Check if the Junimos ate the crops due to no inventory space
			if (ateCrops)
			{
				Game1.showRedMessage($"The Junimos at the {location.Name} ate harvested crops due to lack of storage!");
				return;
			}
			
			if (harvestedToday)
            {
				// Let the player know we harvested
				Game1.addHUDMessage(new HUDMessage($"The Junimos at the {location.Name} have harvested crops.", 2));
				return;
			}
		}

        private void SearchForGroundCrops(GameLocation location)
        {
			// Search for crops
			foreach (KeyValuePair<Vector2, TerrainFeature> tileToHoeDirt in location.terrainFeatures.Pairs.Where(p => p.Value is HoeDirt && (p.Value as HoeDirt).crop != null))
			{
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

				// Crop exists and is fully grown, harvest it
				crop.harvest((int)tile.X, (int)tile.Y, hoeDirt, null);
				harvestedToday = true;
				harvestedTiles.Add(tile);

				// Clear any non-renewing crop
				if (crop.regrowAfterHarvest == -1)
				{
					hoeDirt.crop = null;
				}
			}

			// Search for forage products
			List<Vector2> tilesToRemove = new List<Vector2>();
			foreach (KeyValuePair<Vector2, Object> tileToForage in location.objects.Pairs.Where(p => p.Value.isForage(location)))
			{
				Vector2 tile = tileToForage.Key;
				if (this.addItem(tileToForage.Value.getOne()) != null)
				{
					ateCrops = true;
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
				Vector2 tile = tileToIndoorPot.Key;
				IndoorPot pot = tileToIndoorPot.Value as IndoorPot;
				HoeDirt hoeDirt = pot.hoeDirt.Value;

				// HoeDirt seems to be missing its currentLocation when coming from IndoorPots, which is problematic for Crop.harvest()
				if (hoeDirt.currentLocation is null)
                {
					hoeDirt.currentLocation = location;
				}

				if (hoeDirt.readyForHarvest())
				{
					hoeDirt.crop.harvest((int)tile.X, (int)tile.Y, hoeDirt, null);
					harvestedToday = true;

					// Clear any non-renewing crop
					if (hoeDirt.crop.regrowAfterHarvest == -1)
					{
						hoeDirt.crop = null;
					}
				}
			}

			// Search for IndoorPots with forage items
			foreach (KeyValuePair<Vector2, Object> tileToIndoorPot in location.objects.Pairs.Where(p => p.Value is IndoorPot))
			{
				Vector2 tile = tileToIndoorPot.Key;
				IndoorPot pot = tileToIndoorPot.Value as IndoorPot;

				if (pot.heldObject.Value != null && pot.heldObject.Value.isForage(location))
				{
					if (this.addItem(pot.heldObject.Value.getOne()) != null)
					{
						ateCrops = true;
					}

					pot.heldObject.Value = null;
					harvestedToday = true;
				}
			}
		}

		public void AddItems(NetObjectList<Item> items)
        {
			foreach (var item in items)
			{
				this.addItem(item);
			}

			UpdateSprite();
		}

		private void UpdateSprite()
        {
			if (!this.items.Any())
			{
				base.startingLidFrame.Value = this.ParentSheetIndex;
			}
			else
            {
				base.startingLidFrame.Value = this.ParentSheetIndex + 1;
			}
		}

        public override void ShowMenu()
        {
			// Set source to 0 so recolor doesn't show up
			Game1.activeClickableMenu = new ItemGrabMenu(this.GetItemsForPlayer(Game1.player.UniqueMultiplayerID), reverseGrab: false, showReceivingMenu: true, InventoryMenu.highlightAllItems, grabItemFromInventory, null, grabItemFromChest, snapToBottom: false, canBeExitedWithKey: true, playRightClickSound: true, allowRightClick: true, showOrganizeButton: true, 0, this.fridge ? null : this, -1, this);
		}

        public override bool placementAction(GameLocation location, int x, int y, Farmer who = null)
		{
			base.tileLocation.Value = new Vector2(x / 64, y / 64);
			return true;
		}

		public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
        {
			if (!Game1.didPlayerJustRightClick(ignoreNonMouseHeldInput: true))
			{
				return false;
			}

			this.GetMutex().RequestLock(delegate
			{
				this.frameCounter.Value = 1;
				Game1.playSound("stoneStep");
				Game1.player.Halt();
			});

			return true;
		}

		public override void updateWhenCurrentLocation(GameTime time, GameLocation environment)
		{
			if (this.synchronized.Value)
			{
				this.openChestEvent.Poll();
			}
			if (this.localKickStartTile.HasValue)
			{
				if (Game1.currentLocation == environment)
				{
					if (this.kickProgress == 0f)
					{
						if (Utility.isOnScreen((this.localKickStartTile.Value + new Vector2(0.5f, 0.5f)) * 64f, 64))
						{
							Game1.playSound("clubhit");
						}

						base.shakeTimer = 100;
					}
				}
				else
				{
					this.localKickStartTile = null;
					this.kickProgress = -1f;
				}
				if (this.kickProgress >= 0f)
				{
					float move_duration = 0.25f;
					this.kickProgress += (float)(time.ElapsedGameTime.TotalSeconds / (double)move_duration);
					if (this.kickProgress >= 1f)
					{
						this.kickProgress = -1f;
						this.localKickStartTile = null;
					}
				}
			}
			else
			{
				this.kickProgress = -1f;
			}
			this.fixLidFrame();
			this.mutex.Update(environment);
			if (base.shakeTimer > 0)
			{
				base.shakeTimer -= time.ElapsedGameTime.Milliseconds;
				if (base.shakeTimer <= 0)
				{
					base.health = 10;
				}
			}

			if ((bool)this.playerChest)
			{
				if ((int)this.frameCounter > -1 && this.GetMutex().IsLockHeld())
				{
					this.ShowMenu();
					this.frameCounter.Value = -1;
				}
				else if ((int)this.frameCounter == -1 && Game1.activeClickableMenu == null && this.GetMutex().IsLockHeld())
				{
					this.GetMutex().ReleaseLock();
					this.frameCounter.Value = 1;
					environment.localSound("stoneStep");
				}

				UpdateSprite();
			}
		}
    }
}
