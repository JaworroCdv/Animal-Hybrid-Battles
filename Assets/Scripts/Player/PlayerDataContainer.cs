namespace AnimalHybridBattles.Player
{
    using Unity.Services.Authentication;
    using Unity.Services.Lobbies.Models;

    public static class PlayerDataContainer
    {
        public static string PlayerId { get; private set; }
        public static string LobbyId { get; private set; }
        
        public static void JoinLobby(Lobby lobby)
        {
            PlayerId = AuthenticationService.Instance.PlayerId;
            LobbyId = lobby.Id;
        }
    }
}