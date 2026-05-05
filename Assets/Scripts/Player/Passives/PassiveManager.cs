using UnityEngine;
using System.Collections.Generic;

namespace Sveliaty.Passives
{
    public class PassiveManager : MonoBehaviour
    {
        public static PassiveManager Instance { get; private set; }

        [Header("References")]
        public PlayerManager playerManager;
        public CombatManager combatManager;
        
        [Header("State")]
        [SerializeField] private List<PassiveSkill> activePassives = new List<PassiveSkill>();
        public IReadOnlyList<PassiveSkill> ActivePassives => activePassives;

        private void Awake()
        {
            if (Instance == null) 
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else 
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            // Intentar registro inicial si ya están asignados en el inspector
            if (combatManager != null) RegisterCombatManager(combatManager);
            if (playerManager != null) RegisterPlayerManager(playerManager);
        }

        public void RegisterCombatManager(CombatManager manager)
        {
            // Limpiar suscripciones del manager anterior si existía
            if (combatManager != null)
            {
                combatManager.OnCombatStartEvent -= HandleCombatStart;
                combatManager.OnTurnStartEvent -= HandleTurnStart;
                combatManager.OnDamageResolveEvent -= HandleActionExecuted;
                combatManager.OnEnemyDefeatedEvent -= HandleEnemyDefeated;
            }

            combatManager = manager;

            // Suscribirse al nuevo
            if (combatManager != null)
            {
                combatManager.OnCombatStartEvent += HandleCombatStart;
                combatManager.OnTurnStartEvent += HandleTurnStart;
                combatManager.OnDamageResolveEvent += HandleActionExecuted;
                combatManager.OnEnemyDefeatedEvent += HandleEnemyDefeated;
                Debug.Log("[PassiveManager] Conectado a nuevo CombatManager.");
            }
        }

        public void RegisterPlayerManager(PlayerManager manager)
        {
            if (playerManager != null)
            {
                playerManager.OnDamageTakenEvent -= HandleDamageTaken;
            }

            playerManager = manager;

            if (playerManager != null)
            {
                playerManager.OnDamageTakenEvent += HandleDamageTaken;
            }
        }

        public void EquipPassive(PassiveSkill skill)
        {
            if (skill == null) return;
            
            // Chequeo de duplicados por nombre
            foreach(var p in activePassives)
            {
                if (p.passiveName == skill.passiveName) return;
            }

            PassiveSkill instance = Instantiate(skill);
            
            // Primero otorgamos beneficios iniciales
            instance.OnEquip(this, playerManager);
            
            // Luego registramos (ahora CanGainCards empezará a devolver false)
            activePassives.Add(instance);
            
            Debug.Log($"[PassiveManager] Pasiva equipada: {instance.passiveName}");
        }

        // ==========================================
        // HOOKS ASÍNCRONOS
        // ==========================================

        private void HandleCombatStart()
        {
            foreach (var p in activePassives) p.OnCombatStart(combatManager);
        }

        private void HandleTurnStart(TurnContext ctx)
        {
            foreach (var p in activePassives) p.OnTurnStart(combatManager);
        }

        private void HandleActionExecuted(CombatAction action, float damage)
        {
            foreach (var p in activePassives) p.OnActionExecuted(action, combatManager);
        }

        private void HandleDamageTaken(int damage)
        {
            foreach (var p in activePassives) p.OnDamageTaken(damage, combatManager);
        }

        private void HandleEnemyDefeated()
        {
            // Nota: CombatManager necesita pasar el wasSuperEffective de alguna forma
            // Para eso usamos el Modify Super Effective para trackearlo, o lo leemos.
            // Temporalmente pasaremos false, lo ajustaremos en la integración.
            foreach (var p in activePassives) p.OnEnemyDefeated(combatManager, false);
        }

        // ==========================================
        // HOOKS SÍNCRONOS (MODIFICADORES)
        // ==========================================

        public float GetModifiedArmorGain(float baseGain)
        {
            float gain = baseGain;
            foreach (var p in activePassives) p.ModifyArmorGain(ref gain);
            return gain;
        }

        public int GetModifiedShopPrice(int basePrice)
        {
            int price = basePrice;
            foreach (var p in activePassives) p.ModifyShopPrice(ref price);
            return Mathf.Max(0, price);
        }

        public float GetModifiedSuperEffectiveMultiplier(float baseMultiplier)
        {
            float mult = baseMultiplier;
            foreach (var p in activePassives) p.ModifySuperEffectiveMultiplier(ref mult);
            
            if (activePassives.Count > 0)
                Debug.Log($"[PassiveManager] Modificando mult superefectivo: {baseMultiplier} -> {mult} (Pasivas activas: {activePassives.Count})");
                
            return mult;
        }

        public float GetModifiedEnemyScaling(float baseScaling)
        {
            float scaling = baseScaling;
            foreach (var p in activePassives) p.ModifyEnemyScaling(ref scaling);
            return scaling;
        }

        public int GetModifiedAttempts(int baseAttempts)
        {
            int extra = 0;
            foreach (var p in activePassives) p.ModifyMaxAttempts(ref extra);
            return baseAttempts + extra;
        }

        public float GetModifiedCardRewardProb(float baseProb)
        {
            float prob = baseProb;
            foreach (var p in activePassives) p.ModifyCardRewardProb(ref prob);
            return Mathf.Clamp(prob, 0f, 100f);
        }

        public void GetModifiedCardRewardAmount(ref int randomCards, ref int choiceCards, bool wasSuperEffective)
        {
            foreach (var p in activePassives) 
                p.ModifyCardRewardAmount(ref randomCards, ref choiceCards, wasSuperEffective);
        }

        public int GetModifiedInkReward(int baseReward, bool isElite)
        {
            int ink = baseReward;
            foreach (var p in activePassives) p.ModifyInkReward(ref ink, isElite);
            return Mathf.Max(0, ink);
        }

        public int GetModifiedDamageDealt(int baseDamage)
        {
            int damage = baseDamage;
            foreach (var p in activePassives) p.ModifyDamageDealt(ref damage);
            return Mathf.Max(0, damage);
        }

        // ==========================================
        // RESTRICCIONES
        // ==========================================

        public bool CanUseAbility()
        {
            foreach (var p in activePassives)
                if (!p.CanUseAbility()) return false;
            return true;
        }

        public bool CanGainCards()
        {
            foreach (var p in activePassives)
                if (!p.CanGainCards()) return false;
            return true;
        }

        public bool IsFirstHitAlwaysSuperEffective()
        {
            foreach (var p in activePassives)
                if (p.IsFirstHitAlwaysSuperEffective()) return true;
            return false;
        }
    }
}
