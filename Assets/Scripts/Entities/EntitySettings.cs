namespace AnimalHybridBattles.Entities
{
    using FoxCultGames.UniqueScriptableObjects;
    using UnityEngine;

    [CreateAssetMenu(menuName = "Data/Animal Settings")]
    public class EntitySettings : UniqueScriptableObject
    {
        [SerializeField] private string entityName;
        [SerializeField] private Sprite sprite;
        [SerializeField] private int cost;
        [SerializeField] private float spawnCooldown;
        
        [Header("Stats")]
        [SerializeField] private int health;
        [SerializeField] private float movementSpeed;
        [SerializeField] private float attackCooldown;
        [SerializeField] private float attackDamage;

        public string EntityName => entityName;
        public Sprite Sprite => sprite;
        public int Cost => cost;
        public float SpawnCooldown => spawnCooldown;
        
        public int Health => health;
        public float MovementSpeed => movementSpeed;
        public float AttackCooldown => attackCooldown;
        public float AttackDamage => attackDamage;
    }
}