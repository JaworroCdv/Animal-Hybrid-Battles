namespace AnimalHybridBattles.Entities
{
    using System;
    using System.Linq;
    using Player;
    using PrimeTween;
    using Unity.Netcode;
    using UnityEngine;

    public class EntityController : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer entitySprite;

        public event Action OnDestroyed; 
        
        public readonly NetworkVariable<float> Health = new();
        public float MaxHealth => entitySettings.Health;
        
        private EntitySettings entitySettings;
        private bool isRequestingAttack;
        private float attackResetTimer;
        private float attackCooldown;

        public void Initialize(EntitySettings entitySettings)
        {
            this.entitySettings = entitySettings;
            entitySprite.sprite = entitySettings.Sprite;

            if (!IsOwner)
                return;
            
            SetEntityVariablesRpc(NetworkObjectId, entitySettings.Health);
        }

        public override void OnNetworkDespawn()
        {
            OnDestroyed?.Invoke();
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsOwner || entitySettings == null || Health.Value <= 0)
                return;

            if (isRequestingAttack && Time.time - attackResetTimer >= 5f)
                isRequestingAttack = false;
            
            if (isRequestingAttack)
                return;
            
            if (attackCooldown <= 0)
            {
                isRequestingAttack = true;
                attackResetTimer = Time.time;
                AttackRpc(NetworkObjectId, PlayerDataContainer.LobbyIndex);
                return;
            }

            attackCooldown -= Time.deltaTime;
        }

        [Rpc(SendTo.Server)]
        private void SetEntityVariablesRpc(ulong entityId, float health)
        {
            var entity = NetworkManager.SpawnManager.SpawnedObjects[entityId];
            var entityController = entity.GetComponent<EntityController>();
            entityController.Health.Value = health;
        }

        [Rpc(SendTo.Server)]
        private void AttackRpc(ulong entityId, int playerIndex)
        {
            var enemyIndex = playerIndex == 0 ? 1 : 0;
            var enemyUnits = NetworkManager.ConnectedClientsList[enemyIndex].OwnedObjects.Where(x => x.HasComponent<EntityController>()).ToList();
            if (enemyUnits.Count == 0)
            {
                GameRunner.Instance.AttackPlayerRpc(entitySettings.AttackDamage, playerIndex == 0 ? 1 : 0);
                AnimateAttackRpc(entityId, ulong.MaxValue);
                return;
            }
            
            var randomEntity = enemyUnits.RandomItem();
            var entityController = NetworkManager.SpawnManager.SpawnedObjects[entityId].GetComponent<EntityController>();
            
            randomEntity.GetComponent<EntityController>().Health.Value -= entityController.entitySettings.AttackDamage;
            
            AnimateAttackRpc(entityId, randomEntity.NetworkObjectId);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        private void AnimateAttackRpc(ulong entityId, ulong attackedEntityId)
        {
            if (entityId != NetworkObjectId)
                return;
            
            var entity = NetworkManager.SpawnManager.SpawnedObjects[entityId];
            var attackedEntity = attackedEntityId != ulong.MaxValue ? NetworkManager.SpawnManager.SpawnedObjects[attackedEntityId] : null;
            
            attackCooldown = entitySettings.AttackCooldown;
            isRequestingAttack = false;
            
            var sequence = Sequence.Create()
                .Chain(Tween.Scale(entity.transform, 0.9f, 0.5f, 0.5f, Ease.OutBack));
                
            if (attackedEntity)
                sequence.Group(Tween.Rotation(attackedEntity.transform, Vector3.back * 10f, Vector3.zero, 0.5f, Ease.OutBack));
                        
            sequence
                .OnComplete(() =>
                {
                    if (!IsServer || !attackedEntity)
                        return;
                    
                    var attackedEntityController = attackedEntity.GetComponent<EntityController>();
                    if (attackedEntityController.Health.Value <= 0)
                        attackedEntityController.NetworkObject.Despawn();
                });
        }
    }
}