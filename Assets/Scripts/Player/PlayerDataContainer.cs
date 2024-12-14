namespace AnimalHybridBattles.Player
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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