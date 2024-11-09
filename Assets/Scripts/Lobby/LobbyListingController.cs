using System.Collections.Generic;
using AnimalHybridBattles.Player;
using TMPro;
using Unity.Services.Lobbies;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AnimalHybridBattles.Lobby
{
    public class LobbyListingController : MonoBehaviour
    {
        [SerializeField] private LobbyFilterController filterController;
        [SerializeField] private TMP_InputField joinCodeInputField;
        [SerializeField] private Button joinCodeButton;

        private readonly List<string> lobbiesIds = new();
        
        private IObjectPool<LobbyEntryController> lobbyEntryPool;
        
        private void Start()
        {
            filterController.OnFilterChanged += LobbyFilterController_OnFilterChanged;
            joinCodeButton.onClick.AddListener(OnJoinCodeButtonClicked);
            
            ListAvailableLobbies();
        }

        private async void OnJoinCodeButtonClicked()
        {
            try
            {
                var joinCode = joinCodeInputField.text;
                var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode);
                if (lobby == null)
                    return;

                PlayerDataContainer.LobbyId = lobby.Id;
                SceneManager.LoadScene(Constants.Scenes.LobbySceneName);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Joining by code failed with: {e.Message}");
                throw;
            }
        }

        private async void ListAvailableLobbies()
        {
            try
            {
                foreach (var lobbyId in lobbiesIds)
                {
                    var lobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                
                    var entry = lobbyEntryPool.Get();
                    entry.Initialize(lobby);   
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Lobby listing failed with: {e.Message}");
                throw;
            }
        }

        private void LobbyFilterController_OnFilterChanged(FilterValueChangedArgs args)
        {
            lobbiesIds.Clear();
            lobbiesIds.AddRange(args.AvailableLobbyIds);
            
            lobbyEntryPool.Clear();
            
            ListAvailableLobbies();
        }
    }
}