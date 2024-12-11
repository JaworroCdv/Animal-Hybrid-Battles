namespace AnimalHybridBattles.Lobby
{
    using System.Collections.Generic;
    using ChooseUnitsScreen;
    using Entities;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Pool;

    public class ChooseUnitsController : MonoBehaviour
    {
        [SerializeField] private Transform unitsContainer;
        [SerializeField] private UnitEntryController unitPrefab;
        [SerializeField] private AssetLabelReference unitsLabel;
        
        private ObjectPool<UnitEntryController> unitsPool;
        
        private void Awake()
        {
            unitsPool = new ObjectPool<UnitEntryController>(CreateUnitEntry, actionOnRelease: CleanUpEntry);
        }

        private void Start()
        {
            Addressables.LoadAssetsAsync<EntitySettings>(unitsLabel, null).Completed += handle =>
            {
                foreach (var entitySettings in handle.Result)
                {
                    unitsPool.Get().Initialize(entitySettings);
                }
            };
        }

        private UnitEntryController CreateUnitEntry()
        {
            return Instantiate(unitPrefab, unitsContainer);
        }

        private static void CleanUpEntry(UnitEntryController entry)
        {
            entry.CleanUp();
        }
    }
}
