using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GreenhouseGatherers.GreenhouseGatherers.Objects
{
	[XmlInclude(typeof(HarvestStatue))]
	public class HarvestStatue : StardewValley.Objects.Chest
    {
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
			base.bigCraftable.Value = true;
			base.canBeSetDown.Value = true;
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
				this.frameCounter.Value = 5;
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
					this.frameCounter.Value = 2;
					environment.localSound("stoneStep");
				}
			}
		}
	}
}
