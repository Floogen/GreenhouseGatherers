using System.Collections.Generic;

namespace GreenhouseGatherers.GreenhouseGatherers.API.Interfaces
{
    public interface IJsonAssetApi
    {
        List<string> GetAllObjectsFromContentPack(string cp);
        IDictionary<string, int> GetAllObjectIds();
        int GetObjectId(string name);
    }
}
