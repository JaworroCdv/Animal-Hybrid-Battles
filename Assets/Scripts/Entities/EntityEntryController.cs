namespace AnimalHybridBattles.Entities
{
    using System;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.UI;

    public class EntityEntryController : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Image entityImage;
        [SerializeField] private Image cooldownImage;

        private EntitySettings entitySettings;

        public void Initialize(EntitySettings entitySettings, Action<Guid> spawnEntityCallback, Func<float> cooldownCallback)
        {
            this.entitySettings = entitySettings;
            entityImage.sprite = entitySettings.Sprite;
            
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