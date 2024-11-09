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
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to load lobby with: {e.Message}");
                throw;
            }
        }
    }
}