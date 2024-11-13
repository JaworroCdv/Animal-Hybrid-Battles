namespace AnimalHybridBattles.Lobby
{
    using System;
    using System.Collections.Generic;
    using UnityEngine.UI;
    using Player;
    using TMPro;
    using Unity.Services.Lobbies;
    using Unity.Services.Lobbies.Models;
    using UnityEngine;

    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI lobbyJoinCodeText;
        [SerializeField] private Button[] playerReadyButtons;
        [SerializeField] private Button startGameButton;

        private readonly LobbyEventCallbacks callbacks = new();
        private readonly List<bool> playerReadyStates = new() { false, false };
        
        private int lobbyIndex;
        private float heartbeatTimer;
        private bool hasConnected;
        private bool isReady;
        
        private const string IsReadyKey = "IsReady";
        private const float HeartbeatInterval = 8f;
        
        private async void Start()
        {
            try
            {
                var lobby = await LobbyService.Instance.GetLobbyAsync(PlayerDataContainer.LobbyId);
                if (lobby == null)
                {
                    Debug.LogError($"No lobby with id: {PlayerDataContainer.LobbyId}");
                    return;
                }

                callbacks.PlayerDataChanged += LobbyCallbacks_PlayerDataChanged;
                callbacks.PlayerDataAdded += LobbyCallbacks_PlayerDataChanged;

                try
                {
                    await LobbyService.Instance.SubscribeToLobbyEventsAsync(PlayerDataContainer.LobbyId, callbacks);
                }
                catch (LobbyServiceException e)
                {
                    Debug.LogError($"Failed to subscribe to lobby events with: {e.Message}");
                    throw;
                }
                
                SetReadyState(false);

                var isHost = lobby.HostId == PlayerDataContainer.PlayerId;
                lobbyIndex = isHost ? 0 : 1;
                lobbyNameText.text = lobby.Name;
                lobbyJoinCodeText.text = lobby.LobbyCode;
                
                startGameButton.gameObject.SetActive(isHost);

                for (var i = 0; i < playerReadyButtons.Length; i++)
                {
                    var button = playerReadyButtons[i];
                    button.interactable = i == lobbyIndex;
                    
                    if (i == lobbyIndex)
                        button.onClick.AddListener(OnReadyToggled);
                }
                
                hasConnected = true;
                heartbeatTimer = HeartbeatInterval;
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to load lobby with: {e.Message}");
                throw;
            }
        }

        private async void Update()
        {
            if (!hasConnected)
                return;
            
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer > 0)
                return;
            
            heartbeatTimer = HeartbeatInterval;
            await LobbyService.Instance.SendHeartbeatPingAsync(PlayerDataContainer.LobbyId);
        }

        private void RefreshButtonState(int index, bool isReady)
        {
            playerReadyButtons[index].GetComponent<Image>().color = isReady ? Color.green : Color.red;
        }

        private void OnReadyToggled()
        {
            isReady = !isReady;
            SetReadyState(isReady);

            foreach (var button in playerReadyButtons)
                button.interactable = false;
        }

        private void LobbyCallbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playersData)
        {
            foreach (var (index, changedData) in playersData)
            {
                var isReady = changedData.ContainsKey(IsReadyKey) && changedData[IsReadyKey].Value.Value.ToLower() == "true";
                if (index == lobbyIndex)
                {
                    playerReadyButtons[index].interactable = true;
                    this.isReady = isReady;
                }
                
                RefreshButtonState(index, isReady); 
                
                if (lobbyIndex > 0)
                    continue;
                
                playerReadyStates[index] = isReady;
                startGameButton.interactable = playerReadyStates.TrueForAll(x => x);
            }
        }

        private static async void SetReadyState(bool isReady)
        {
            try
            {
                await LobbyService.Instance.UpdatePlayerAsync(PlayerDataContainer.LobbyId, PlayerDataContainer.PlayerId, new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        { IsReadyKey, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady.ToString()) }
                    }
                });
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to update player with: {e.Message}");
                throw;
            }
        }
    }
}