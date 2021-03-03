using System;
using System.Collections.Generic;

namespace GreenhouseGatherers.GreenhouseGatherers.API.Interfaces.JsonAssets
{
    public interface IJsonAssetApi
    {
        void LoadAssets(string path);
        List<string> GetAllBigCraftablesFromContentPack(string cp);
        IDictionary<string, int> GetAllBigCraftableIds();
        int GetBigCraftableId(string name);

        event EventHandler IdsAssigned;
    }
}
