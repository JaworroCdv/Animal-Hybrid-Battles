namespace AnimalHybridBattles.Player
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lobby;
    using Unity.Services.Authentication;
    using Unity.Services.Lobbies;
    using Unity.Services.Lobbies.Models;
    using UnityEngine;

    public static class PlayerDataContainer
    {
        public static event Action<Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>>> OnLobbyDataChanged;
        public static event Action<Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>>> OnPlayerDataChanged;
        
        public static readonly Guid[] SelectedUnits = new Guid[ChooseUnitsController.MaxUnitsPerPlayer];
        
        public static string PlayerId { get; private set; }
        public static string LobbyId { get; private set; }
        public static int LobbyIndex { get; private set; }
        public static Lobby LobbyCache { get; private set; }

        public static string LobbyName => LobbyCache.Name;
        public static string LobbyJoinCode => LobbyCache.LobbyCode;
        public static string RelayCode => LobbyCache.Data[Constants.LobbyData.JoinCode].Value;

        private static LobbyEventCallbacks lobbyCallbacks;
        
        public static void JoinLobby(Lobby lobby)
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            LobbyId = lobby.Id;
            LobbyCache = lobby;

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
            return PlayerId == LobbyCache.HostId;
        }

        public static bool IsPlayerReady(int lobbyIndex)
        {
            if (LobbyCache == null || LobbyCache.Players.Count <= lobbyIndex || LobbyCache.Players[lobbyIndex].Data == null)
                return false;
            
            return LobbyCache.Players[lobbyIndex].Data.TryGetValue(Constants.PlayerData.IsReady, out var isReadyData) && bool.Parse(isReadyData.Value);
        }

        public static async Task RequestLobbyRefresh()
        {
            LobbyCache = await LobbyService.Instance.GetLobbyAsync(LobbyId);
        }

        private static void OnLobbyChanged(ILobbyChanges lobbyChanges)
        {
            if (lobbyChanges is { LobbyDeleted: false })
                lobbyChanges.ApplyToLobby(LobbyCache);
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