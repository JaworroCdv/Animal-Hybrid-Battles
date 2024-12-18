namespace AnimalHybridBattles.Entities
{
    using System;
    using TMPro;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.UI;

    public class EntityEntryController : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Image entityImage;
        [SerializeField] private Image cooldownImage;
        [SerializeField] private TextMeshProUGUI entityCost;

        private EntitySettings entitySettings;

        public void Initialize(EntitySettings entitySettings, Action<Guid> spawnEntityCallback, Func<float> cooldownCallback)
        {
            this.entitySettings = entitySettings;
            entityImage.sprite = entitySettings.Sprite;
            entityCost.text = entitySettings.Cost.ToString();
            
            selectButton.onClick.AddListener(() =>
            {
                if (cooldownCallback.Invoke() > 0)
                    return;
                
                spawnEntityCallback.Invoke(entitySettings.Guid);
            });
        }

        public void UpdateCooldown(float cooldown)
        {
            cooldownImage.fillAmount = cooldown / entitySettings.SpawnCooldown;
        }
    }
}