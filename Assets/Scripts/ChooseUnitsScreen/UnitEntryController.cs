namespace AnimalHybridBattles.ChooseUnitsScreen
{
    using Entities;
    using Player;
    using UnityEngine;
    using UnityEngine.UI;

    public class UnitEntryController : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image unitImage;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color unselectedColor;
        
        public void Initialize(EntitySettings entitySettings)
        {
            unitImage.sprite = entitySettings.Sprite;
            selectButton.onClick.AddListener(OnSelectButtonClicked);
            
            background.color = PlayerDataContainer.IsUnitSelected(entitySettings).Result ? selectedColor : unselectedColor;
            
            async void OnSelectButtonClicked()
            {
                selectButton.interactable = false;
                var wasAdded = await PlayerDataContainer.ToggleUnitSelection(entitySettings);
                selectButton.interactable = true;
                background.color = wasAdded ? selectedColor : unselectedColor;
            }
        }
        
        public void CleanUp()
        {
            selectButton.onClick.RemoveAllListeners();
        }
    }
}