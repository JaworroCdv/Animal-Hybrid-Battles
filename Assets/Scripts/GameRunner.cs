namespace AnimalHybridBattles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entities;
    using Lobby;
    using Player;
    using TMPro;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;

    public class GameRunner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject entityPrefab;
        [SerializeField] private List<EntityEntryController> entityEntries;
        [SerializeField] private List<Transform> spawnPoints;
        [SerializeField] private List<Slider> playerHpSliders;
        [SerializeField] private List<Slider> enemyHpSliders;
        [SerializeField] private int startingEnergy = 50;
        [SerializeField] private float gameDuration = 120f;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private GameObject winScreen;
        [SerializeField] private GameObject loseScreen;
        [SerializeField] private TextMeshProUGUI pointsText;
        
        public event Action OnPlayerMoneyChanged;
        
        public static GameRunner Instance { get; private set; }
        public NetworkList<int> PlayersEnergy { get; private set; }
        
        private int SpawnPointsPerPlayer => spawnPoints.Count / LobbyCreationController.MaxPlayersPerLobbyCount;
        
        private readonly NetworkVariable<float> gameTimer = new();
        private readonly NetworkVariable<bool> gameEnded = new();
        
        private NetworkList<float> cooldownTimers;
        private NetworkList<int> playerPoints;
        private bool[] spawnPointsOccupied;
        private float energyTimer;
        private float pointsTimer;
        
        private void Awake()
        {
            Instance = this;
            
            NetworkManager.OnServerStopped += NetworkManager_OnServerStopped;
            NetworkManager.OnConnectionEvent += NetworkManager_OnConnectionEvent;
            
            spawnPointsOccupied = new bool[SpawnPointsPerPlayer];
            playerPoints = new NetworkList<int>();
            playerPoints.OnListChanged += _ =>
            {
                if (playerPoints.Count < 2)
                    return;
                
                pointsText.text = $"{playerPoints[0]} - {playerPoints[1]}";
            };
            
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

            if (NetworkManager.IsServer)
            {
                for (var i = 0; i < LobbyCreationController.MaxPlayersPerLobbyCount * ChooseUnitsController.MaxUnitsPerPlayer; i++)
                    cooldownTimers.Add(0);
            }
            
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
                }, () => cooldownTimers[index], x =>
                {
                    if (PlayersEnergy.Count < PlayerDataContainer.LobbyIndex)
                        return false;
                    
                    return PlayersEnergy[PlayerDataContainer.LobbyIndex] >= x.Cost;
                });
            }
            
            if (!NetworkManager.IsServer)
                return;
            
            energyTimer = Time.time;
            pointsTimer = Time.time;
            
            playerPoints.Add(0);
            playerPoints.Add(0);
            
            PlayersEnergy.Add(startingEnergy);
            PlayersEnergy.Add(startingEnergy);

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
        
        public void GoToMainMenu()
        {
            NetworkManager.Singleton.Shutdown();
            Time.timeScale = 1f;
            SceneManager.LoadScene(Constants.Scenes.MainMenuSceneName);
        }

        public void GoToLobby()
        {
            if (!NetworkManager.IsServer)
                DestroyRemainingEntitiesRpc();
            
            Time.timeScale = 1f;
            Destroy(FindFirstObjectByType<LobbyController>().gameObject);
            SceneManager.LoadScene(Constants.Scenes.LobbySceneName);
        }

        [Rpc(SendTo.Server)]
        private void DestroyRemainingEntitiesRpc()
        {
            var networkObjects = NetworkManager.SpawnManager.SpawnedObjects.Values;
            for (var i = networkObjects.Count - 1; i >= 0; i--)
            {
                if (!networkObjects.ElementAt(i).TryGetComponent<EntityController>(out _))
                    continue;

                networkObjects.ElementAt(i).Despawn();
            }
        }
        
        private void Update()
        {
            if (winScreen.activeSelf || loseScreen.activeSelf)
                return;
            
            UpdateEntityHealthSliders();
            
            if (!NetworkManager.IsServer || gameEnded.Value)
                return;
            
            gameTimer.Value -= Time.deltaTime;
            if (gameTimer.Value <= 0)
            {
                gameEnded.Value = true;
                var winner = playerPoints[0] > playerPoints[1] ? NetworkManager.ConnectedClientsIds[0] : playerPoints[0] == playerPoints[1] ? ulong.MaxValue : NetworkManager.ConnectedClientsIds[1];
                GameEndedRpc(winner);
                return;
            }

            if (Time.time - pointsTimer >= 20f)
            {
                pointsTimer = Time.time;
                playerPoints[0] = Mathf.Max(0, playerPoints[0] - 1);
                playerPoints[1] = Mathf.Max(0, playerPoints[1] - 1);
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
            if (!NetworkManager.IsConnectedClient)
                return;
            
            var index = 0;
            var enemyIndex = 0;
            foreach (var networkObject in NetworkManager.SpawnManager.SpawnedObjects.Values)
            {
                if (!networkObject.TryGetComponent<EntityController>(out var entityController))
                    continue;

                if (networkObject.IsOwner)
                {
                    playerHpSliders[index].gameObject.SetActive(true);
                    playerHpSliders[index].value = entityController.Health.Value / entityController.MaxHealth;
                    playerHpSliders[index].transform.position = Camera.main.WorldToScreenPoint(entityController.transform.position + Vector3.up * 0.5f);
                
                    index++;
                }
                else
                {
                    enemyHpSliders[enemyIndex].gameObject.SetActive(true);
                    enemyHpSliders[enemyIndex].value = entityController.Health.Value / entityController.MaxHealth;
                    enemyHpSliders[enemyIndex].transform.position = Camera.main.WorldToScreenPoint(entityController.transform.position + Vector3.up * 0.5f);
                    
                    enemyIndex++;
                }
            }
            
            for (var i = index; i < playerHpSliders.Count; i++)
                playerHpSliders[i].gameObject.SetActive(false);
            
            for (var i = enemyIndex; i < enemyHpSliders.Count; i++)
                enemyHpSliders[i].gameObject.SetActive(false);
        }

        [Rpc(SendTo.Everyone)]
        private void GameEndedRpc(ulong winnerId)
        {
            Time.timeScale = 0f;
            
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
            InitializeEntityLocallyRpc(entity.NetworkObjectId, entityGuid, spawnedIndex, playerIndex);
            
            var startingIndex = playerIndex * ChooseUnitsController.MaxUnitsPerPlayer;
            cooldownTimers[startingIndex + index] = GameData.EntitySettings[entityGuid].SpawnCooldown;
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void InitializeEntityLocallyRpc(ulong objectId, Guid entityGuid, int spawnedIndex, int playerIndex)
        {
            var spawnedObject = NetworkManager.SpawnManager.SpawnedObjects[objectId];
            var entityController = spawnedObject.GetComponent<EntityController>();
            entityController.Initialize(GameData.EntitySettings[entityGuid]);
            entityController.OnDestroyed += OnDestroyed;

            void OnDestroyed()
            {
                entityController.OnDestroyed -= OnDestroyed;
                
                if (playerIndex == PlayerDataContainer.LobbyIndex)
                    spawnPointsOccupied[spawnedIndex] = false;
                
                if (!NetworkManager.IsServer)
                    return;
                
                playerPoints[playerIndex == 0 ? 1 : 0] += 1;
            }
        }
        
        private void HideAllHpSliders()
        {
            foreach (var slider in playerHpSliders)
                slider?.gameObject.SetActive(false);
            
            foreach (var slider in enemyHpSliders)
                slider?.gameObject.SetActive(false);
        }

        private void NetworkManager_OnServerStopped(bool isServer)
        {
            if (!PlayerDataContainer.IsHost())
                winScreen?.SetActive(true);
            
            HideAllHpSliders();
        }

        private void NetworkManager_OnConnectionEvent(NetworkManager arg1, ConnectionEventData arg2)
        {
            if (arg2.EventType == ConnectionEvent.ClientDisconnected && winScreen != null)
                winScreen.SetActive(true);
            
            HideAllHpSliders();
        }
    }
}