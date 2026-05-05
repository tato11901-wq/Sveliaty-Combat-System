using UnityEngine;


namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/First Strike")]
    public class PassiveFirstStrike : PassiveSkill
    {
        private bool isFirstHit;

        private void Reset()
        {
            passiveName = "Golpe Certero";
            description = "Tu primer ataque en cada combate siempre es Súper Efectivo, sin importar la afinidad del enemigo.\nContra: Comienzas cada combate con 1 intento menos.";
            profile = PassiveProfile.Situacional;
        }

        public override void OnCombatStart(CombatManager combatManager)
        {
            isFirstHit = true;
        }

        public override void OnActionExecuted(CombatAction action, CombatManager combatManager)
        {
            isFirstHit = false;
        }

        public override bool IsFirstHitAlwaysSuperEffective()
        {
            return isFirstHit;
        }

        public override void ModifyMaxAttempts(ref int extraAttempts)
        {
            extraAttempts -= 1;
        }
    }
}
