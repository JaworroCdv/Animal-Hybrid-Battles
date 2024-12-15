namespace AnimalHybridBattles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Entities;
    using UnityEngine.AddressableAssets;

    public static class GameData
    {
        public static Dictionary<Guid, EntitySettings> EntitySettings { get; private set; }

        public static async Task LoadData()
        {
            await LoadEntitiesData();
        }

        private static async Task LoadEntitiesData()
        {
            var entities = await Addressables.LoadAssetsAsync<EntitySettings>("Units", null).Task;
            EntitySettings = entities.ToDictionary(x => x.Guid);
        }
    }
}