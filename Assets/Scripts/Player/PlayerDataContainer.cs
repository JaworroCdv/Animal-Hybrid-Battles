namespace AnimalHybridBattles.Player
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Entities;
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

        private static Task<Lobby> currentUnitPendingTask;
        
        public static void JoinLobby(Lobby lobby)
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            LobbyId = lobby.Id;
        }

        public static async Task<bool> ToggleUnitSelection(EntitySettings entitySettings)
        {
            cachedSelectedUnits ??= await GetSelectedUnits();
            if (cachedSelectedUnits.Add(entitySettings.Guid.GetGUID))
                cachedSelectedUnits.Remove(entitySettings.Guid.GetGUID);

            try
            {
                var lobby = await LobbyService.Instance.GetLobbyAsync(LobbyId);
                if (!lobby.Players[LobbyIndex].Data.TryGetValue(Constants.PlayerData.Units, out var unitsData))
                {
                    var units = new HashSet<Guid> { entitySettings.Guid.GetGUID };
                    await LobbyService.Instance.UpdatePlayerAsync(LobbyId, PlayerId, new UpdatePlayerOptions
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { Constants.PlayerData.Units, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, JsonUtility.ToJson(units)) }
                        }
                    });
                    return true;
                }
                else
                {
                    var units = JsonUtility.FromJson<HashSet<Guid>>(unitsData.Value);
                    var entityGuid = entitySettings.Guid.GetGUID;
                    var wasEntryAdded = true;
                    if (!units.Add(entityGuid))
                    {
                        units.Remove(entitySettings.Guid.GetGUID);
                        wasEntryAdded = false;
                    }

                    await LobbyService.Instance.UpdatePlayerAsync(LobbyId, PlayerId, new UpdatePlayerOptions
                    {
                        Data = new Dictionary<string, PlayerDataObject>
                        {
                            { Constants.PlayerData.Units, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, JsonUtility.ToJson(units)) }
                        }
                    });
                    return wasEntryAdded;
                }
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"[PlayerDataContainer] Failed to toggle unit selection with: {e.Message}");
                throw;
            }
        }

        public static async Task<HashSet<Guid>> GetSelectedUnits()
        {
            try
            {
                currentUnitPendingTask = LobbyService.Instance.GetLobbyAsync(LobbyId);
                
                var lobby = await currentUnitPendingTask;
                if (!lobby.Players[LobbyIndex].Data.TryGetValue(Constants.PlayerData.Units, out var unitsData))
                    return new HashSet<Guid>();

                var units = JsonUtility.FromJson<HashSet<Guid>>(unitsData.Value);
                var unitIds = new HashSet<Guid>();
                foreach (var unitId in units)
                    unitIds.Add(unitId);

                currentUnitPendingTask = null;
                return unitIds;
            }
            catch (LobbyServiceException e)
            {
                currentUnitPendingTask = null;
                Debug.LogError($"[PlayerDataContainer] Failed to check if unit is selected with: {e.Message}");
                throw;
            }
        }
        
        public static async Task<bool> IsSelected(EntitySettings entitySettings)
        {
            Task.WaitAll(currentUnitPendingTask);
            
            cachedSelectedUnits ??= await GetSelectedUnits();
            return cachedSelectedUnits.Contains(entitySettings.Guid.GetGUID);
        }

        public static void SetLobbyIndex(int index)
        {
            LobbyIndex = index;
        }
    }
}