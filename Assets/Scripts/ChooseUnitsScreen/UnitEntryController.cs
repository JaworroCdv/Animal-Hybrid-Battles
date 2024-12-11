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
        
        public async void Initialize(EntitySettings entitySettings)
        {
            unitImage.sprite = entitySettings.Sprite;
            selectButton.onClick.AddListener(ToggleUnitSelection);

            selectButton.interactable = false;
            background.color = await PlayerDataContainer.IsSelected(entitySettings) ? selectedColor : unselectedColor;
            selectButton.interactable = true;
            
            async void ToggleUnitSelection()
            {
                selectButton.interactable = false;
                background.color = await PlayerDataContainer.ToggleUnitSelection(entitySettings) ? selectedColor : unselectedColor;
                selectButton.interactable = true;
            }
        }
        
        public void CleanUp()
        {
            selectButton.onClick.RemoveAllListeners();
        }
    }
}