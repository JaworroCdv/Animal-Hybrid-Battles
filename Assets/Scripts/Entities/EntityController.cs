namespace AnimalHybridBattles.Entities
{
    using Unity.Netcode;
    using UnityEngine;

    public class EntityController : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer entitySprite;
        
        private EntitySettings entitySettings;
        
        public void Initialize(EntitySettings entitySettings)
        {
            entitySprite.sprite = entitySettings.Sprite;
        }
    }
}