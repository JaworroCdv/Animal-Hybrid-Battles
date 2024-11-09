using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnimalHybridBattles.Player;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
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
        [SerializeField] private LobbyEntryController lobbyEntryPrefab;
        [SerializeField] private Transform lobbyEntryContainer;

        private readonly List<string> lobbiesIds = new();
        private readonly List<LobbyEntryController> lobbyEntries = new();
            
        private IObjectPool<LobbyEntryController> lobbyEntryPool;

        private async void Start()
        {
            filterController.OnFilterChanged += LobbyFilterController_OnFilterChanged;
            joinCodeButton.onClick.AddListener(OnJoinCodeButtonClicked);

            lobbyEntryPool = new ObjectPool<LobbyEntryController>(CreateLobbyEntry, GetLobbyEntry, CleanupEntry);

            await GetAllLobbies();
            ListAvailableLobbies();
        }

        private LobbyEntryController CreateLobbyEntry()
        {
            return Instantiate(lobbyEntryPrefab, lobbyEntryContainer);
        }

        private void GetLobbyEntry(LobbyEntryController entry)
        {
            entry.gameObject.SetActive(true);
            lobbyEntries.Add(entry);
        }

        private void CleanupEntry(LobbyEntryController entry)
        {
            lobbyEntries.Remove(entry);
            entry.gameObject.SetActive(false);
            entry.CleanUp();
        }

        private async Task GetAllLobbies()
        {
            var availableLobbies = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new(QueryFilter.FieldOptions.HasPassword, false.ToString(), QueryFilter.OpOptions.EQ)
                }
            });
            
            lobbiesIds.Clear();
            lobbiesIds.AddRange(availableLobbies.Results.Select(x => x.Id));
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

        private void LobbyFilterController_OnFilterChanged(FilterValueChangedArgs args)
        {
            lobbiesIds.Clear();
            lobbiesIds.AddRange(args.AvailableLobbyIds);

            for (var i = lobbyEntries.Count - 1; i >= 0; i--)
                lobbyEntryPool.Release(lobbyEntries[i]);
            
            lobbyEntries.Clear();
            lobbyEntryPool.Clear();
            
            ListAvailableLobbies();
        }
    }
}