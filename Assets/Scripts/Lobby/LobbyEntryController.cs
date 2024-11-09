using TMPro;
using UnityEngine;
using LobbyEntry = Unity.Services.Lobbies.Models.Lobby;

namespace AnimalHybridBattles.Lobby
{
    public class LobbyEntryController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private GameObject isPrivateIcon;
        
        public void Initialize(LobbyEntry lobby)
        {
            lobbyNameText.text = lobby.Name;
            isPrivateIcon.SetActive(lobby.IsPrivate);
        }
    }
}