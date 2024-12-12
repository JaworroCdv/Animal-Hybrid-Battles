using System.Threading.Tasks;
using AnimalHybridBattles.Player;
using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LobbyEntry = Unity.Services.Lobbies.Models.Lobby;

namespace AnimalHybridBattles.Lobby
{
    public class LobbyCreationController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField lobbyNameText;
        [SerializeField] private Toggle isPrivateToggle;
        [SerializeField] private TMP_InputField passwordText;
        [SerializeField] private TextMeshProUGUI errorPromptText;
        [SerializeField] private Button createLobbyButton;

        public const int MaxPlayersPerLobbyCount = 2;
        
        private void Start()
        {
            isPrivateToggle.isOn = false;
            OnPrivateStateToggled(isPrivateToggle.isOn);
            
            createLobbyButton.onClick.AddListener(TryCreatingLobby);
            isPrivateToggle.onValueChanged.AddListener(OnPrivateStateToggled);
            
            errorPromptText.gameObject.SetActive(false);
        }

        private async void TryCreatingLobby()
        {
            if (!ValidateLobbyName() || !ValidatePassword())
                return;

            errorPromptText.gameObject.SetActive(false);
            
            try
            {
                createLobbyButton.interactable = false;

                var lobby = await CreateLobby();
                if (lobby == null)
                    return;
                
                PlayerDataContainer.JoinLobby(lobby);
                SceneManager.LoadScene(Constants.Scenes.LobbySceneName);
            }
            catch (LobbyServiceException e)
            {
                createLobbyButton.interactable = true;
                
                Debug.LogError($"Lobby Creation failed with: {e.Message}");
                throw;
            }
        }

        private async Task<LobbyEntry> CreateLobby()
        {
            if (isPrivateToggle.isOn)
            {
                return await LobbyService.Instance.CreateLobbyAsync(lobbyNameText.text, MaxPlayersPerLobbyCount,
                    new CreateLobbyOptions
                    {
                        Password = isPrivateToggle.isOn ? passwordText.text : ""
                    });
            }

            return await LobbyService.Instance.CreateLobbyAsync(lobbyNameText.text, MaxPlayersPerLobbyCount);
        }

        private bool ValidateLobbyName()
        {
            if (!string.IsNullOrEmpty(lobbyNameText.text))
                return true;
            
            HandleError("Please enter a name");
            return false;
        }

        private bool ValidatePassword()
        {
            if (!isPrivateToggle.isOn)
                return true;

            if (string.IsNullOrEmpty(passwordText.text))
            {
                HandleError("Please enter a password");
                return false;
            }

            if (passwordText.text.Length < 8)
            {
                HandleError("Password must be at least 8 characters");
                return false;
            }

            return true;
        }

        private void HandleError(string error)
        {
            errorPromptText.text = error;
            errorPromptText.gameObject.SetActive(true);
        }

        private void OnPrivateStateToggled(bool isPrivate)
        {
            passwordText.gameObject.SetActive(isPrivate);
        }
    }
}