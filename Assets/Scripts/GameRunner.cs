namespace AnimalHybridBattles
{
    using System;
    using System.Collections.Generic;
    using Entities;
    using Lobby;
    using Player;
    using TMPro;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.UI;

    public class GameRunner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject entityPrefab;
        [SerializeField] private List<EntityEntryController> entityEntries;
        [SerializeField] private List<Transform> spawnPoints;
        [SerializeField] private List<Slider> entityHpSliders;
        [SerializeField] private int startingEnergy = 50;
        [SerializeField] private float gameDuration = 120f;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject winScreen;
        [SerializeField] private GameObject loseScreen;
        
        public event Action OnPlayerMoneyChanged; 
        
        public static GameRunner Instance { get; private set; }
        public NetworkList<int> PlayersEnergy { get; private set; }
        
        private int SpawnPointsPerPlayer => spawnPoints.Count / LobbyCreationController.MaxPlayersPerLobbyCount;
        
        private readonly NetworkVariable<float> gameTimer = new();
        private readonly NetworkVariable<bool> gameEnded = new();
        
        private NetworkList<float> cooldownTimers;
        private bool[] spawnPointsOccupied;
        private float energyTimer;
        
        private void Awake()
        {
            Instance = this;
            
            spawnPointsOccupied = new bool[SpawnPointsPerPlayer];
            PlayersEnergy = new NetworkList<int>();
            PlayersEnergy.OnListChanged += _ => OnPlayerMoneyChanged?.Invoke();
            
            gameTimer.Value = gameDuration;
            gameEnded.Value = false;
            cooldownTimers = new NetworkList<float>(writePerm: NetworkVariableWritePermission.Owner);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            gameTimer.OnValueChanged += TimerValueChanged;
            
            for (var i = 0; i < ChooseUnitsController.MaxUnitsPerPlayer; i++)
            {
                var index = i;
                var entitySettings = GameData.EntitySettings[PlayerDataContainer.SelectedUnits[i]];
                entityEntries[i].Initialize(entitySettings, entityGuid =>
                {
                    if (GameData.EntitySettings[entityGuid].Cost > PlayersEnergy[PlayerDataContainer.LobbyIndex])
                        return;
                    
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
            
            energyTimer = Time.time;
            
            PlayersEnergy.Add(startingEnergy);
            PlayersEnergy.Add(startingEnergy);
            
            for (var i = 0; i < LobbyCreationController.MaxPlayersPerLobbyCount * ChooseUnitsController.MaxUnitsPerPlayer; i++)
                cooldownTimers.Add(0);

            void TimerValueChanged(float previousValue, float newValue)
            {
                if (newValue <= 0)
                {
                    timerText.text = "00:00";
                    return;
                }
                
                timerText.text = $"{Mathf.FloorToInt(newValue / 60)}:{Mathf.FloorToInt(newValue % 60):D2}";
            }
        }

        private void Update()
        {
            if (!NetworkManager.IsServer || gameEnded.Value)
                return;

            UpdateEntityHealthSliders();
            
            gameTimer.Value -= Time.deltaTime;
            if (gameTimer.Value <= 0)
            {
                gameEnded.Value = true;
                GameEndedRpc(NetworkManager.ServerClientId);
                return;
            }

            if (Time.time - energyTimer >= 1f)
            {
                energyTimer = Time.time;
                AddEnergyRpc();
            }
            
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

        private void UpdateEntityHealthSliders()
        {
            var index = 0;
            foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjects.Values)
            {
                if (!networkObject.TryGetComponent<EntityController>(out var entityController))
                    continue;
                
                entityHpSliders[index].gameObject.SetActive(true);
                entityHpSliders[index].value = entityController.Health.Value / entityController.MaxHealth;
                entityHpSliders[index].transform.position = Camera.main.WorldToScreenPoint(entityController.transform.position + Vector3.up * 0.5f);
                index++;
            }
            
            for (var i = index; i < entityHpSliders.Count; i++)
                entityHpSliders[i].gameObject.SetActive(false);
        }

        [Rpc(SendTo.Everyone)]
        private void GameEndedRpc(ulong winnerId)
        {
            if (NetworkManager.LocalClientId == winnerId)
                winScreen.SetActive(true);
            else
                loseScreen.SetActive(true);
        }

        [Rpc(SendTo.Server)]
        private void AddEnergyRpc()
        {
            PlayersEnergy[0] += 1;
            PlayersEnergy[1] += 1;
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
            PlayersEnergy[playerIndex] -= GameData.EntitySettings[entityGuid].Cost;
            
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