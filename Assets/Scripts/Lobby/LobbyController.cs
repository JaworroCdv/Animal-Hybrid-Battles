using AnimalHybridBattles.Player;
using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;

namespace AnimalHybridBattles.Lobby
{
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI lobbyJoinCodeText;

        private float heartbeatTimer;
        private bool hasConnected;
        
        private const float HeartbeatInterval = 5f;
        
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
                
                lobbyNameText.text = lobby.Name;
                lobbyJoinCodeText.text = lobby.LobbyCode;
                
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
            
            await LobbyService.Instance.SendHeartbeatPingAsync(PlayerDataContainer.LobbyId);
            heartbeatTimer = HeartbeatInterval;
        }
    }
}