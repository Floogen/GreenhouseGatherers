using System.Collections.Generic;

namespace GreenhouseGatherers.Framework.Models
{
    public class SaveData
    {
        public List<HarvestStatueData> SavedStatueData { get; set; }

        public SaveData()
        {
            SavedStatueData = new List<HarvestStatueData>();
        }

        public SaveData(List<HarvestStatueData> savedStatueData)
        {
            SavedStatueData = savedStatueData;
        }
    }
}
