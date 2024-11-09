using AnimalHybridBattles.Player;
using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LobbyEntry = Unity.Services.Lobbies.Models.Lobby;

namespace AnimalHybridBattles.Lobby
{
    public class LobbyEntryController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private GameObject isPrivateIcon;
        [SerializeField] private Button joinButton;

        private string lobbyId;
        
        public void Initialize(LobbyEntry lobby)
        {
            lobbyId = lobby.Id;
            
            lobbyNameText.text = lobby.Name;
            isPrivateIcon.SetActive(lobby.IsPrivate);
            
            joinButton.onClick.AddListener(OnJoinButtonClicked);
        }

        public void CleanUp()
        {
            joinButton.onClick.RemoveAllListeners();
        }

        private async void OnJoinButtonClicked()
        {
            try
            {
                await LobbyService.Instance.GetLobbyAsync(lobbyId);
                PlayerDataContainer.LobbyId = lobbyId;
                SceneManager.LoadScene(Constants.Scenes.LobbySceneName);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to connect to lobby with: {e.Message}");
                throw;
            }
        }
    }
}