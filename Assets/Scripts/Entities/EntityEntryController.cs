namespace AnimalHybridBattles.Entities
{
    using System;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class EntityEntryController : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Image entityImage;
        [SerializeField] private Image cooldownImage;
        [SerializeField] private TextMeshProUGUI entityCost;

        [SerializeField] private Color validColor;
        [SerializeField] private Color invalidColor;

        private EntitySettings entitySettings;
        private Predicate<EntitySettings> hasEnoughEnergyCallback;

        public void Initialize(EntitySettings entitySettings, Action<Guid> spawnEntityCallback, Func<float> cooldownCallback, Predicate<EntitySettings> hasEnoughEnergyCallback)
        {
            this.entitySettings = entitySettings;
            entityImage.sprite = entitySettings.Sprite;
            entityCost.text = entitySettings.Cost.ToString();
            
            this.hasEnoughEnergyCallback = hasEnoughEnergyCallback;
            
            selectButton.onClick.AddListener(() =>
            {
                if (cooldownCallback.Invoke() > 0)
                    return;
                
                spawnEntityCallback.Invoke(entitySettings.Guid);
            });
        }

        private void Update()
        {
            if (hasEnoughEnergyCallback == null || entitySettings == null)
                return;
            
            entityCost.color = hasEnoughEnergyCallback.Invoke(entitySettings) ? validColor : invalidColor;
        }

        public void UpdateCooldown(float cooldown)
        {
            cooldownImage.fillAmount = cooldown / entitySettings.SpawnCooldown;
        }
    }
}