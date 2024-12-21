namespace AnimalHybridBattles.Lobby
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine.UI;
    using Player;
    using TMPro;
    using Unity.Netcode;
    using Unity.Netcode.Transports.UTP;
    using Unity.Services.Lobbies;
    using Unity.Services.Lobbies.Models;
    using Unity.Services.Relay;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI lobbyJoinCodeText;
        [SerializeField] private Button[] playerReadyButtons;
        [SerializeField] private Sprite notReadySprite;
        [SerializeField] private Sprite readySprite;
        [SerializeField] private Button startGameButton;

        private readonly List<bool> playerReadyStates = new() { false, false };
        
        private float heartbeatTimer;
        private bool hasConnected;
        private bool isReady;
        
        private const float HeartbeatInterval = 8f;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            PlayerDataContainer.OnLobbyDataChanged += LobbyCallbacks_LobbyChanged;
            PlayerDataContainer.OnPlayerDataChanged += LobbyCallbacks_PlayerDataChanged;
            await PlayerDataContainer.RequestLobbyRefresh();

            var isHost = PlayerDataContainer.IsHost();
            PlayerDataContainer.SetLobbyIndex(isHost ? 0 : 1);
            lobbyNameText.text = PlayerDataContainer.LobbyName;
            lobbyJoinCodeText.text = PlayerDataContainer.LobbyJoinCode;
                
            startGameButton.gameObject.SetActive(isHost);
            startGameButton.onClick.AddListener(StartServerAndGame);

            for (var i = 0; i < playerReadyButtons.Length; i++)
            {
                var button = playerReadyButtons[i];
                button.interactable = i == PlayerDataContainer.LobbyIndex;
                RefreshButtonState(i, PlayerDataContainer.IsPlayerReady(i));
                    
                if (i == PlayerDataContainer.LobbyIndex)
                    button.onClick.AddListener(OnReadyToggled);
            }
                
            hasConnected = true;
            heartbeatTimer = HeartbeatInterval;

            if (PlayerDataContainer.IsHost())
            {
                await StartRelay();
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                var allocation = await Relay.Instance.JoinAllocationAsync(PlayerDataContainer.RelayCode);
                
                NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetClientRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port, 
                    allocation.AllocationIdBytes, 
                    allocation.Key, 
                    allocation.ConnectionData,
                    allocation.HostConnectionData
                    );
                
                NetworkManager.Singleton.StartClient();
            }

            async Task StartRelay()
            {
                var allocation = await Relay.Instance.CreateAllocationAsync(LobbyCreationController.MaxPlayersPerLobbyCount);

                NetworkManager.Singleton.gameObject.GetComponent<UnityTransport>().SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port, 
                    allocation.AllocationIdBytes, 
                    allocation.Key, 
                    allocation.ConnectionData
                    );
                
                var joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
                
                await LobbyService.Instance.UpdateLobbyAsync(PlayerDataContainer.LobbyId, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { Constants.LobbyData.JoinCode, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                    }
                });
            }
        }

        private void OnDestroy()
        {
            PlayerDataContainer.OnLobbyDataChanged -= LobbyCallbacks_LobbyChanged;
            PlayerDataContainer.OnPlayerDataChanged -= LobbyCallbacks_PlayerDataChanged;
        }

        private async void Update()
        {
            if (!hasConnected || !PlayerDataContainer.IsHost() || SceneManager.GetActiveScene().name != Constants.Scenes.LobbySceneName)
                return;
            
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer > 0)
                return;
            
            heartbeatTimer = HeartbeatInterval;
            
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(PlayerDataContainer.LobbyId);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[LobbyController] Failed to send heartbeat with: {e.Message}");
                throw;
            }
        }

        private void RefreshButtonState(int index, bool isReady)
        {
            playerReadyButtons[index].GetComponent<Image>().sprite = isReady ? readySprite : notReadySprite;
        }

        private void OnReadyToggled()
        {
            isReady = !isReady;
            foreach (var button in playerReadyButtons)
                button.interactable = false;
            
            SetReadyState(isReady);
        }

        private static void SetReadyState(bool isReady)
        {
            try
            {
                _ = LobbyService.Instance.UpdatePlayerAsync(PlayerDataContainer.LobbyId, PlayerDataContainer.PlayerId, new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { Constants.PlayerData.IsReady, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady.ToString()) }
                    }
                });
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to update player with: {e.Message}");
                throw;
            }
        }

        private static void StartServerAndGame()
        {
            NetworkManager.Singleton.SceneManager.LoadScene(Constants.Scenes.UnitsChooseScreenSceneName, LoadSceneMode.Single);
        }

        private static void LobbyCallbacks_LobbyChanged(Dictionary<string,ChangedOrRemovedLobbyValue<DataObject>> lobbyChanges)
        {
            if (!lobbyChanges.TryGetValue(Constants.LobbyData.JoinCode, out var joinCodeData))
                return;

            if (!PlayerDataContainer.IsHost())
            {
                Relay.Instance.JoinAllocationAsync(joinCodeData.Value.Value);
            }
        }

        private void LobbyCallbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playersData)
        {
            foreach (var (index, changedData) in playersData)
            {
                var isReady = changedData.ContainsKey(Constants.PlayerData.IsReady) && changedData[Constants.PlayerData.IsReady].Value.Value.ToLower() == "true";
                if (index == PlayerDataContainer.LobbyIndex)
                {
                    playerReadyButtons[index].interactable = true;
                    this.isReady = isReady;
                }
                
                RefreshButtonState(index, isReady); 
                
                if (PlayerDataContainer.LobbyIndex > 0)
                    continue;
                
                playerReadyStates[index] = isReady;
                startGameButton.interactable = playerReadyStates.TrueForAll(x => x);
            }
        }
    }
}