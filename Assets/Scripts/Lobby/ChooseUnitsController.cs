namespace AnimalHybridBattles.Lobby
{
    using System;
    using System.Collections.Generic;
    using ChooseUnitsScreen;
    using Entities;
    using NetworkSerialization;
    using Player;
    using TMPro;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Pool;
    using UnityEngine.SceneManagement;

    public class ChooseUnitsController : NetworkBehaviour
    {
        [SerializeField] private float roundStartTimerInMinutes = 1f;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Transform unitsContainer;
        [SerializeField] private UnitEntryController unitPrefab;
        [SerializeField] private AssetLabelReference unitsLabel;

        private readonly NetworkVariable<long> roundStartTicks = new();
        private readonly List<UnitEntryController> unitEntries = new();
        
        private NetworkList<Guid> selectedUnits;
        private ObjectPool<UnitEntryController> unitsPool;
        
        public const int MaxUnitsPerPlayer = 5;
        
        private void Awake()
        {
            UserNetworkVariableSerialization<Guid>.WriteValue = GuidSerializationExtensions.WriteValueSafe;
            UserNetworkVariableSerialization<Guid>.ReadValue = GuidSerializationExtensions.ReadValueSafe;
            UserNetworkVariableSerialization<Guid>.DuplicateValue = GuidSerializationExtensions.DuplicateValue;
            
            selectedUnits = new NetworkList<Guid>();
            
            unitsPool = new ObjectPool<UnitEntryController>(CreateUnitEntry, actionOnRelease: CleanUpEntry);
            timerText.text = "Choose your units";
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!NetworkManager.IsServer)
                return;
            
            for (var i = 0; i < LobbyCreationController.MaxPlayersPerLobbyCount * MaxUnitsPerPlayer; i++)
                selectedUnits.Add(Guid.Empty);
        }

        private void Start()
        {
            foreach (var entitySettings in GameData.EntitySettings.Values)
            {
                var entry = unitsPool.Get();
                entry.Initialize(entitySettings, IsUnitSelected, x => ToggleUnitSelectionRpc(x, PlayerDataContainer.LobbyIndex));
                unitEntries.Add(entry);
            }
        }

        private void Update()
        {
            if (roundStartTicks.Value == default)
                return;

            var roundStartTime = new DateTime(roundStartTicks.Value);
            var timeLeft = roundStartTime - DateTime.UtcNow;
            if (timeLeft <= TimeSpan.Zero)
            {
                timerText.text = "0:00";

                var startingIndex = PlayerDataContainer.LobbyIndex * MaxUnitsPerPlayer;
                for (var i = 0; i < MaxUnitsPerPlayer; i++)
                    PlayerDataContainer.SelectedUnits[i] = selectedUnits[startingIndex + i];
                
                if (NetworkManager.IsServer)
                    NetworkManager.SceneManager.LoadScene(Constants.Scenes.GameplaySceneName, LoadSceneMode.Single);
                
                return;
            }

            timerText.text = $"{timeLeft.Minutes}:{timeLeft.Seconds:D2}";
        }
        
        private bool IsUnitSelected(Guid unitGuid)
        {
            var unitStartingIndex = PlayerDataContainer.LobbyIndex * MaxUnitsPerPlayer;
            for (var i = unitStartingIndex; i < unitStartingIndex + MaxUnitsPerPlayer; i++)
            {
                if (selectedUnits[i] == unitGuid)
                    return true;
            }
            
            return false;
        }
        
        [Rpc(SendTo.Server)]
        private void ToggleUnitSelectionRpc(Guid unitGuid, int playerIndex, RpcParams rpcParams = default)
        {
            var unitStartingIndex = playerIndex * MaxUnitsPerPlayer;
            
            for (var i = unitStartingIndex; i < unitStartingIndex + MaxUnitsPerPlayer; i++)
            {
                if (selectedUnits[i] != unitGuid)
                    continue;
                
                selectedUnits[i] = Guid.Empty;
                
                SendSelectionResultRpc(unitGuid, false, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
                return;
            }

            for (var i = unitStartingIndex; i < unitStartingIndex + MaxUnitsPerPlayer; i++)
            {
                if (selectedUnits[i] != Guid.Empty) 
                    continue;
                
                selectedUnits[i] = unitGuid;
                CheckIfAllPlayersAreReadyRpc();
                
                SendSelectionResultRpc(unitGuid, true, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
                return;
            }
            
            SendSelectionResultRpc(unitGuid, false, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SendSelectionResultRpc(Guid unitGuid, bool isUnitSelected, RpcParams rpcParams)
        {
            foreach (var entry in unitEntries)
            {
                if (entry.EntitySettings.Guid != unitGuid)
                    continue;
                
                entry.UpdateUnitSelection(isUnitSelected);
                break;
            }
        }

        [Rpc(SendTo.Server)]
        private void CheckIfAllPlayersAreReadyRpc()
        {
            for (var i = 0; i < selectedUnits.Count; i++)
            {
                if (selectedUnits[i] == Guid.Empty)
                    return;
            }
            
            roundStartTicks.Value = DateTime.UtcNow.AddMinutes(roundStartTimerInMinutes).Ticks;
        }

        private UnitEntryController CreateUnitEntry()
        {
            return Instantiate(unitPrefab, unitsContainer);
        }

        private void CleanUpEntry(UnitEntryController entry)
        {
            unitEntries.Remove(entry);
            entry.CleanUp();
        }
    }
}
