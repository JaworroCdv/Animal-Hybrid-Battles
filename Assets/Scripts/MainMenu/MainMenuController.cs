using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimalHybridBattles.MainMenu
{
    using Lobby;
    using Unity.Services.Lobbies;

    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button exitLobbyButton;

        private const string CreateLobbySceneName = "CreateLobby";
        private const string JoinLobbySceneName = "JoinLobby";
        
        public async void Start()
        {
            await GameData.LoadData();

            var lobbyController = FindFirstObjectByType<LobbyController>();
            if (lobbyController)
                Destroy(lobbyController.gameObject);
            
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            
            ((ILobbyServiceSDKConfiguration)LobbyService.Instance).EnableLocalPlayerLobbyEvents(true);
            
            createLobbyButton.onClick.AddListener(OnCreateLobbyButtonClicked);
            joinLobbyButton.onClick.AddListener(OnJoinLobbyButtonClicked);
            exitLobbyButton.onClick.AddListener(OnExitLobbyButtonClicked);
        }

        private static void OnCreateLobbyButtonClicked()
        {
            SceneManager.LoadScene(CreateLobbySceneName);
        }

        private static void OnJoinLobbyButtonClicked()
        {
            SceneManager.LoadScene(JoinLobbySceneName);
        }

        private static void OnExitLobbyButtonClicked()
        {
#if !UNITY_EDITOR
            Application.Quit();
#else 
            EditorApplication.isPlaying = false;
#endif
        }
    }
}