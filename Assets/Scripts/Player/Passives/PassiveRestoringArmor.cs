using UnityEngine;


namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/Restoring Armor")]
    public class PassiveRestoringArmor : PassiveSkill
    {
        private int baseArmor;

        private void Reset()
        {
            passiveName = "Armadura Regenerante";
            description = "Al inicio de cada turno, tu armadura se restaura a su valor base si fue reducida.\nContra: Toda ganancia de armadura queda reducida a la mitad.";
            profile = PassiveProfile.MuyBuenaConContra;
        }

        public override void OnCombatStart(CombatManager combatManager)
        {
            if (combatManager.playerManager != null)
            {
                baseArmor = combatManager.playerManager.ActiveArmor;
            }
        }

        public override void OnTurnStart(CombatManager combatManager)
        {
            if (combatManager.playerManager != null && combatManager.playerManager.ActiveArmor < baseArmor)
            {
                combatManager.playerManager.SetArmor(baseArmor);
                Debug.Log("[Pasiva] Armadura restaurada.");
            }
        }

        public override void ModifyArmorGain(ref float armorGain)
        {
            armorGain *= 0.5f;
        }
    }
}
