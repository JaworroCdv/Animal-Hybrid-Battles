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
        public static event Action<Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>>> OnLobbyDataChanged;
        public static event Action<Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>>> OnPlayerDataChanged;
        
        public static string PlayerId { get; private set; }
        public static string LobbyId { get; private set; }
        public static int LobbyIndex { get; private set; }

        public static string LobbyName => lobbyCache.Name;
        public static string LobbyJoinCode => lobbyCache.LobbyCode;
        
        private static HashSet<Guid> cachedSelectedUnits;

        private static Lobby lobbyCache;
        private static LobbyEventCallbacks lobbyCallbacks;
        
        public static void JoinLobby(Lobby lobby)
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            LobbyId = lobby.Id;
            lobbyCache = lobby;

            OnLobbyChanged(null);
            lobbyCallbacks = new LobbyEventCallbacks();
            lobbyCallbacks.LobbyChanged += OnLobbyChanged;
            lobbyCallbacks.DataAdded += LobbyCallbacks_LobbyChanged;
            lobbyCallbacks.DataChanged += LobbyCallbacks_LobbyChanged;
            lobbyCallbacks.PlayerDataChanged += LobbyCallbacks_PlayerDataChanged;
            lobbyCallbacks.PlayerDataAdded += LobbyCallbacks_PlayerDataChanged;

            try
            {
                LobbyService.Instance.SubscribeToLobbyEventsAsync(LobbyId, lobbyCallbacks);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[PlayerDataContainer] Failed to subscribe to callbacks: {e.Message}");
                throw;
            }
        }

        public static bool ToggleUnitSelection(EntitySettings entitySettings)
        {
            cachedSelectedUnits ??= GetSelectedUnits();
            if (!cachedSelectedUnits.Remove(entitySettings.Guid))
            {
                if (cachedSelectedUnits.Count >= ChooseUnitsController.MaxUnits)
                    return false;
                
                cachedSelectedUnits.Add(entitySettings.Guid);
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
                return cachedSelectedUnits.Contains(entitySettings.Guid);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[PlayerDataContainer] Failed to toggle unit selection with: {e.Message}");
                throw;
            }
        }

        public static bool IsSelected(EntitySettings entitySettings)
        {
            cachedSelectedUnits ??= GetSelectedUnits();
            return cachedSelectedUnits.Contains(entitySettings.Guid);
        }

        public static void SetLobbyIndex(int index)
        {
            LobbyIndex = index;
        }

        public static bool IsHost()
        {
            return PlayerId == lobbyCache.HostId;
        }

        public static bool IsPlayerReady(int lobbyIndex)
        {
            if (lobbyCache == null || lobbyCache.Players.Count <= lobbyIndex || lobbyCache.Players[lobbyIndex].Data == null)
                return false;
            
            return lobbyCache.Players[lobbyIndex].Data.TryGetValue(Constants.PlayerData.IsReady, out var isReadyData) && bool.Parse(isReadyData.Value);
        }

        public static async Task RequestLobbyRefresh()
        {
            lobbyCache = await LobbyService.Instance.GetLobbyAsync(LobbyId);
        }

        private static HashSet<Guid> GetSelectedUnits()
        {
            try
            {
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

        private static void OnLobbyChanged(ILobbyChanges lobbyChanges)
        {
            if (lobbyChanges is { LobbyDeleted: false })
                lobbyChanges.ApplyToLobby(lobbyCache);
        }

        private static void LobbyCallbacks_LobbyChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> changes)
        {
            OnLobbyDataChanged?.Invoke(changes);
        }

        private static void LobbyCallbacks_PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> changes)
        {
            OnPlayerDataChanged?.Invoke(changes);
        }
    }
}