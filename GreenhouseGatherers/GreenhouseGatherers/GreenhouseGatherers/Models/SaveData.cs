using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenhouseGatherers.GreenhouseGatherers.Models
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
