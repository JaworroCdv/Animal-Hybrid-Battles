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

            selectButton.interactable = false;
            PlayerDataContainer.IsSelected(entitySettings).ContinueWith(task =>
            {
                selectButton.interactable = true;
                background.color = task.Result ? selectedColor : unselectedColor;
            });
            
            void OnSelectButtonClicked()
            {
                selectButton.interactable = false;
                PlayerDataContainer.ToggleUnitSelection(entitySettings).ContinueWith(task =>
                {
                    selectButton.interactable = true;
                    background.color = task.Result ? selectedColor : unselectedColor;
                });
            }
        }
        
        public void CleanUp()
        {
            selectButton.onClick.RemoveAllListeners();
        }
    }
}