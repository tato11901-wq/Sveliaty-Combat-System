using UnityEngine;

namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/Extra Reward")]
    public class PassiveExtraReward : PassiveSkill
    {
        private void Reset()
        {
            passiveName = "Cazarrecompensas";
            description = "Al derrotar a un enemigo explotando su debilidad, ganas 1 carta aleatoria adicional.\nContra: El multiplicador de daño Súper Efectivo se reduce de x1.5 a x1.1.";
            profile = PassiveProfile.Situacional;
        }

        public override void ModifySuperEffectiveMultiplier(ref float multiplier)
        {
            multiplier = 1.1f;
        }

        public override void ModifyCardRewardAmount(ref int randomCards, ref int choiceCards, bool wasSuperEffective)
        {
            if (wasSuperEffective)
            {
                randomCards += 1;
            }
        }
    }
}
