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
	}
}
