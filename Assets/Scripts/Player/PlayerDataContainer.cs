namespace AnimalHybridBattles.Player
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Entities;
    using Lobby;
    using Unity.Services.Authentication;
    using Unity.Services.Lobbies;
    using Unity.Services.Lobbies.Models;
    using UnityEngine;

    public static class PlayerDataContainer
    {
        public static string PlayerId { get; private set; }
        public static string LobbyId { get; private set; }
        public static int LobbyIndex { get; private set; }
        
        private static HashSet<Guid> cachedSelectedUnits;
        private static Task<Lobby> currentLobbyPendingTask;

        private static Lobby lobbyCache;
        private static LobbyEventCallbacks lobbyCallbacks;
        
        public static void JoinLobby(Lobby lobby)
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            LobbyId = lobby.Id;

            OnLobbyChanged(null);
            lobbyCallbacks = new LobbyEventCallbacks();
            lobbyCallbacks.LobbyChanged += OnLobbyChanged;
            LobbyService.Instance.SubscribeToLobbyEventsAsync(LobbyId, lobbyCallbacks);
        }

        public static async Task<bool> ToggleUnitSelection(EntitySettings entitySettings)
        {
            cachedSelectedUnits ??= await GetSelectedUnits();
            if (!cachedSelectedUnits.Remove(entitySettings.Guid.GetGUID))
            {
                if (cachedSelectedUnits.Count >= ChooseUnitsController.MaxUnits)
                    return false;
                
                cachedSelectedUnits.Add(entitySettings.Guid.GetGUID);
            }

            try
            {
                _ = LobbyService.Instance.UpdatePlayerAsync(LobbyId, PlayerId, new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject> 
                    { 
                        { Constants.PlayerData.Units, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, JsonUtility.ToJson(cachedSelectedUnits)) }
                    }
                });
                return cachedSelectedUnits.Contains(entitySettings.Guid.GetGUID);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[PlayerDataContainer] Failed to toggle unit selection with: {e.Message}");
                throw;
            }
        }

        private static async Task<HashSet<Guid>> GetSelectedUnits()
        {
            try
            {
                if (currentLobbyPendingTask != null)
                    await currentLobbyPendingTask;
                
                if (lobbyCache.Players[LobbyIndex].Data == null || !lobbyCache.Players[LobbyIndex].Data.TryGetValue(Constants.PlayerData.Units, out var unitsData))
                    return new HashSet<Guid>();

                var units = JsonUtility.FromJson<HashSet<Guid>>(unitsData.Value);
                return new HashSet<Guid>(units);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[PlayerDataContainer] Failed to check if unit is selected with: {e.Message}");
                throw;
            }
        }

        public static async Task<bool> IsSelected(EntitySettings entitySettings)
        {
            cachedSelectedUnits ??= await GetSelectedUnits();
            return cachedSelectedUnits.Contains(entitySettings.Guid.GetGUID);
        }

        public static void SetLobbyIndex(int index)
        {
            LobbyIndex = index;
        }

        private static async void OnLobbyChanged(ILobbyChanges lobbyChanges)
        {
            currentLobbyPendingTask = LobbyService.Instance.GetLobbyAsync(LobbyId);
            lobbyCache = await currentLobbyPendingTask;
            currentLobbyPendingTask = null;
        }
    }
}