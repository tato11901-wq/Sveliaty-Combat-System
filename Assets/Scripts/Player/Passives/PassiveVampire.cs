using UnityEngine;


namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/Vampire")]
    public class PassiveVampire : PassiveSkill
    {
        private void Reset()
        {
            passiveName = "Vampiro";
            description = "Te curas 5 de vida al inicio de cada combate.\nContra: Pierdes 2 de vida al inicio de cada turno, sin importar tu armadura.";
            profile = PassiveProfile.MuyBuenaConContra;
        }

        public override void OnCombatStart(CombatManager combatManager)
        {
            if (combatManager.playerManager != null)
            {
                combatManager.playerManager.ModifyHealth(5);
                Debug.Log("[Pasiva] Curación de 5 HP al iniciar combate.");
            }
        }

        public override void OnTurnStart(CombatManager combatManager)
        {
            if (combatManager.playerManager != null)
            {
                combatManager.playerManager.ModifyHealth(-2, true);
                Debug.Log("[Pasiva] Daño de 2 HP al iniciar turno.");
            }
        }
    }
}
