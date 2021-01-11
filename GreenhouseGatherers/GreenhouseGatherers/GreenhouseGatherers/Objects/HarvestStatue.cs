using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
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

		public HarvestStatue(Vector2 position, int itemID) : base(true, position, itemID)
		{
			this.Name = "Harvest Statue";
			base.type.Value = "Crafting";
			base.startingLidFrame.Value = itemID;
			base.bigCraftable.Value = true;
			base.canBeSetDown.Value = true;
		}

		public void HarvestCrops(GameLocation location)
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
					monitor.Log($"Crop at ({tile.X}, {tile.Y}) is not ready for harvesting: {crop.fullyGrown} | {crop.regrowAfterHarvest} | {crop.dayOfCurrentPhase}, {crop.currentPhase}", LogLevel.Debug);
					continue;
                }

				// Crop exists and is fully grown, harvest it
				monitor.Log($"Harvesting crop at ({tile.X}, {tile.Y}): {crop.fullyGrown} | {crop.regrowAfterHarvest} | {crop.dayOfCurrentPhase}, {crop.currentPhase}", LogLevel.Debug);
				crop.harvest((int)tile.X, (int)tile.Y, hoeDirt, null);

				if (crop.regrowAfterHarvest == -1)
                {
					hoeDirt.destroyCrop(tileLocation, false, location);
                }
			}
        }

		public void SetItems(NetObjectList<Item> items)
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
