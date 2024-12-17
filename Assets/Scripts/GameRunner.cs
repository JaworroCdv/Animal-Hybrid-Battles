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
        [SerializeField] private List<Transform> spawnPoints;
        
        private int SpawnPointsPerPlayer => spawnPoints.Count / LobbyCreationController.MaxPlayersPerLobbyCount;
        
        private NetworkList<float> cooldownTimers;
        private bool[] spawnPointsOccupied;
        
        private void Awake()
        {
            spawnPointsOccupied = new bool[SpawnPointsPerPlayer];
            cooldownTimers = new NetworkList<float>(writePerm: NetworkVariableWritePermission.Owner);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            for (var i = 0; i < ChooseUnitsController.MaxUnitsPerPlayer; i++)
            {
                var index = i;
                var entitySettings = GameData.EntitySettings[PlayerDataContainer.SelectedUnits[i]];
                entityEntries[i].Initialize(entitySettings, entityGuid =>
                {
                    var startingIndex = SpawnPointsPerPlayer * PlayerDataContainer.LobbyIndex;
                    for (var j = startingIndex; j < startingIndex + SpawnPointsPerPlayer; j++)
                    {
                        if (spawnPointsOccupied[j - startingIndex])
                            continue;
                
                        spawnPointsOccupied[j - startingIndex] = true;
                        SpawnEntityRpc(entityGuid, index, j - startingIndex, PlayerDataContainer.LobbyIndex, spawnPoints[j].position);
                        return;
                    }
                }, () => cooldownTimers[index]);
            }
            
            if (!NetworkManager.IsServer)
                return;
            
            for (var i = 0; i < LobbyCreationController.MaxPlayersPerLobbyCount * ChooseUnitsController.MaxUnitsPerPlayer; i++)
                cooldownTimers.Add(0);
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
        private void SpawnEntityRpc(Guid entityGuid, int index, int spawnedIndex, int playerIndex, Vector2 position, RpcParams rpcParams = default)
        {
            var clientId = NetworkManager.ConnectedClientsIds[playerIndex];
            var entity = Instantiate(entityPrefab);
            entity.SpawnWithOwnership(clientId);
            
            entity.transform.position = position;
            InitializeEntityLocallyRpc(entity.NetworkObjectId, entityGuid, spawnedIndex);
            
            var startingIndex = playerIndex * ChooseUnitsController.MaxUnitsPerPlayer;
            cooldownTimers[startingIndex + index] = GameData.EntitySettings[entityGuid].SpawnCooldown;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void InitializeEntityLocallyRpc(ulong objectId, Guid entityGuid, int spawnedIndex)
        {
            var spawnedObject = NetworkManager.SpawnManager.SpawnedObjects[objectId];
            var entityController = spawnedObject.GetComponent<EntityController>();
            entityController.Initialize(GameData.EntitySettings[entityGuid]);
            entityController.OnDestroyed += OnDestroyed;

            void OnDestroyed()
            {
                entityController.OnDestroyed -= OnDestroyed;
                spawnPointsOccupied[spawnedIndex] = false;
            }
        }
    }
}