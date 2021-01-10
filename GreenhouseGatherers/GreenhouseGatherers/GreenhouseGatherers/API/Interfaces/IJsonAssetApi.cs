using System;
using System.Collections.Generic;

namespace GreenhouseGatherers.GreenhouseGatherers.API.Interfaces
{
    public interface IJsonAssetApi
    {
        List<string> GetAllBigCraftablesFromContentPack(string cp);
        IDictionary<string, int> GetAllBigCraftableIds();
        int GetBigCraftableId(string name);

        event EventHandler IdsAssigned;
    }
}
