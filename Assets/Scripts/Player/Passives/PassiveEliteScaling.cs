using UnityEngine;

namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/Elite Scaling")]
    public class PassiveEliteScaling : PassiveSkill
    {
        private void Reset()
        {
            passiveName = "Escalado de Élite";
            description = "Siempre recibes recompensa de carta y puedes elegir 2 recompensas si el ataque fue Súper Efectivo.\nContra: Los enemigos escalan un 50% más rápido.";
            profile = PassiveProfile.MuyBuenaConContra;
        }

        public override void ModifyEnemyScaling(ref float scalingMultiplier)
        {
            // Escalan un 50% extra sobre el multiplicador base
            scalingMultiplier += 0.5f;
        }

        public override void ModifyCardRewardProb(ref float prob)
        {
            // 100% recompensa
            prob = 100f;
        }

        public override void ModifyCardRewardAmount(ref int randomCards, ref int choiceCards, bool wasSuperEffective)
        {
            if (wasSuperEffective)
            {
                // Elegir 2 recompensas
                choiceCards = 2;
                randomCards = 0; 
            }
        }
    }
}
