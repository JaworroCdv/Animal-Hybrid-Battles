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
            selectButton.onClick.AddListener(ToggleUnitSelection);

            selectButton.interactable = false;
            background.color = PlayerDataContainer.IsSelected(entitySettings) ? selectedColor : unselectedColor;
            selectButton.interactable = true;
            
            void ToggleUnitSelection()
            {
                selectButton.interactable = false;
                background.color = PlayerDataContainer.ToggleUnitSelection(entitySettings) ? selectedColor : unselectedColor;
                selectButton.interactable = true;
            }
        }
        
        public void CleanUp()
        {
            selectButton.onClick.RemoveAllListeners();
        }
    }
}