using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace AnimalHybridBattles.Lobby
{
    public struct FilterValueChangedArgs
    {
        public IReadOnlyList<string> AvailableLobbyIds;
    }
    
    public class LobbyFilterController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField lobbyNameFilter;
        [SerializeField] private Toggle privateStateFilter;

        public event Action<FilterValueChangedArgs> OnFilterChanged;
        
        private void Start()
        {
            lobbyNameFilter.onValueChanged.AddListener(LobbyNameFilter_OnValueChanged);
            privateStateFilter.onValueChanged.AddListener(PrivateStateFilter_OnValueChanged);
        }

        private async void FilterAvailableLobbies(string lobbyName, bool isPrivate)
        {
            try
            {
                var availableLobbies = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
                {
                    Filters = new List<QueryFilter>
                    {
                        new(QueryFilter.FieldOptions.Name, lobbyName, QueryFilter.OpOptions.CONTAINS),
                        new(QueryFilter.FieldOptions.HasPassword, isPrivate.ToString(), QueryFilter.OpOptions.EQ)
                    }
                });
            
                OnFilterChanged?.Invoke(new FilterValueChangedArgs
                {
                    AvailableLobbyIds = availableLobbies.Results.Select(x => x.Id).ToList()
                });
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Lobby filtering failed with: {e.Message}");
                throw;
            }
        }

        private void LobbyNameFilter_OnValueChanged(string newValue)
        {
            FilterAvailableLobbies(newValue, privateStateFilter.isOn);
        }

        private void PrivateStateFilter_OnValueChanged(bool isPrivate)
        {
            FilterAvailableLobbies(lobbyNameFilter.text, isPrivate);
        }
    }
}