namespace AnimalHybridBattles.ChooseUnitsScreen
{
    using System;
    using Entities;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class UnitEntryController : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image unitImage;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color unselectedColor;

        public event Action<EntitySettings> OnUnitHovered;
        public EntitySettings EntitySettings { get; private set; }
        
        public void Initialize(EntitySettings entitySettings, Predicate<Guid> isSelected, Action<Guid> toggleUnitSelection)
        {
            EntitySettings = entitySettings;
            
            unitImage.sprite = entitySettings.Sprite;
            selectButton.onClick.AddListener(OnToggleSelection);

            selectButton.interactable = false;
            background.color = isSelected.Invoke(entitySettings.Guid) ? selectedColor : unselectedColor;
            selectButton.interactable = true;

            void OnToggleSelection()
            {
                toggleUnitSelection?.Invoke(entitySettings.Guid);
            }
        }
        
        public void UpdateUnitSelection(bool isSelected)
        {
            background.color = isSelected ? selectedColor : unselectedColor;
        }
        
        public void CleanUp()
        {
            selectButton.onClick.RemoveAllListeners();
        }

        public void SetInteractable(bool isInteractable)
        {
            selectButton.interactable = isInteractable;
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            OnUnitHovered?.Invoke(EntitySettings);
        }
    }
}