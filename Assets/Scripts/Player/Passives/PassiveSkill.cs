using UnityEngine;
using System.Collections.Generic;


namespace Sveliaty.Passives
{
    public abstract class PassiveSkill : ScriptableObject
    {
        public string passiveName;
        [TextArea] public string description;
        public PassiveProfile profile;
        public Sprite icon;

        // Eventos de Ciclo de Vida
        public virtual void OnEquip(PassiveManager manager, PlayerManager playerManager) { }
        public virtual void OnUnequip(PassiveManager manager, PlayerManager playerManager) { }

        // Hooks de Eventos (Asíncronos / Notificaciones)
        public virtual void OnCombatStart(CombatManager combatManager) { }
        public virtual void OnTurnStart(CombatManager combatManager) { }
        public virtual void OnActionExecuted(CombatAction action, CombatManager combatManager) { }
        public virtual void OnEnemyDefeated(CombatManager combatManager, bool wasSuperEffective) { }
        public virtual void OnDamageTaken(int damage, CombatManager combatManager) { }

        // Modificadores de Valor (Síncronos)
        public virtual void ModifyArmorGain(ref float armorGain) { }
        public virtual void ModifyShopPrice(ref int price) { }
        public virtual void ModifySuperEffectiveMultiplier(ref float multiplier) { }
        public virtual void ModifyEnemyScaling(ref float scalingMultiplier) { }
        public virtual void ModifyMaxAttempts(ref int extraAttempts) { }
        public virtual void ModifyCardRewardProb(ref float prob) { }
        public virtual void ModifyCardRewardAmount(ref int randomCards, ref int choiceCards, bool wasSuperEffective) { }
        public virtual void ModifyInkReward(ref int inkReward, bool isElite) { }
        public virtual void ModifyDamageDealt(ref int damageDealt) { }

        // Restricciones
        public virtual bool CanUseAbility() { return true; }
        public virtual bool CanGainCards() { return true; }
        public virtual bool IsFirstHitAlwaysSuperEffective() { return false; }
    }
}
