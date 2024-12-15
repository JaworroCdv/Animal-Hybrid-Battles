namespace AnimalHybridBattles
{
    using System;
    using System.Collections.Generic;
    using Entities;
    using Lobby;
    using Player;
    using Unity.Netcode;
    using UnityEngine;

    public class GameRunner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject entityPrefab;
        [SerializeField] private List<EntityEntryController> entityEntries;

        private NetworkList<float> cooldownTimers;

        private void Awake()
        {
            cooldownTimers = new NetworkList<float>(writePerm: NetworkVariableWritePermission.Owner);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            for (var i = 0; i < ChooseUnitsController.MaxUnitsPerPlayer; i++)
            {
                var index = i;
                var entitySettings = GameData.EntitySettings[PlayerDataContainer.SelectedUnits[i]];
                entityEntries[i].Initialize(entitySettings, entityGuid => SpawnEntityRpc(entityGuid, index, PlayerDataContainer.LobbyIndex), () => cooldownTimers[index]);
            }
            
            if (!NetworkManager.IsServer)
                return;
            
            for (var i = 0; i < LobbyCreationController.MaxPlayersPerLobbyCount * ChooseUnitsController.MaxUnitsPerPlayer; i++)
                cooldownTimers.Add(0);
        }

        public override void OnDestroy()
        {
            cooldownTimers.Dispose();
            base.OnDestroy();
        }

        private void Update()
        {
            if (!NetworkManager.IsServer)
                return;
            
            for (var i = 0; i < cooldownTimers.Count; i++)
            {
                var playerIndex = Mathf.FloorToInt(i / (float)ChooseUnitsController.MaxUnitsPerPlayer);
                if (cooldownTimers[i] <= 0)
                    continue;

                cooldownTimers[i] -= Time.deltaTime;
                if (cooldownTimers[i] < 0)
                    cooldownTimers[i] = 0;
                
                UpdateEntryCooldownRpc(playerIndex, i % ChooseUnitsController.MaxUnitsPerPlayer, cooldownTimers[i]);
            }
        }
        
        [Rpc(SendTo.Everyone)]
        private void UpdateEntryCooldownRpc(int playerIndex, int index, float cooldownTimer, RpcParams rpcParams = default)
        {
            if (playerIndex != PlayerDataContainer.LobbyIndex)
                return;
            
            entityEntries[index].UpdateCooldown(cooldownTimer);
        }

        [Rpc(SendTo.Server)]
        private void SpawnEntityRpc(Guid entityGuid, int index, int playerIndex, RpcParams rpcParams = default)
        {
            var entity = NetworkManager.SpawnManager.InstantiateAndSpawn(entityPrefab, NetworkManager.LocalClientId, true, position: Vector3.zero);
            InitializeEntityLocallyRpc(entity.NetworkObjectId, entityGuid);
            
            var startingIndex = playerIndex * ChooseUnitsController.MaxUnitsPerPlayer;
            cooldownTimers[startingIndex + index] = GameData.EntitySettings[entityGuid].SpawnCooldown;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void InitializeEntityLocallyRpc(ulong objectId, Guid entityGuid)
        {
            NetworkManager.SpawnManager.SpawnedObjects[objectId].GetComponent<EntityController>().Initialize(GameData.EntitySettings[entityGuid]);
        }
    }
}