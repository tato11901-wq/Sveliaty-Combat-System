using UnityEngine;


namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/Mercenary")]
    public class PassiveMercenary : PassiveSkill
    {
        private PlayerManager cachedPlayer;

        private void Reset()
        {
            passiveName = "Mercenario";
            description = "Tu daño base se reduce a la mitad, pero ganas daño extra igual al 10% de tu tinta actual.\nContra: Ganas un 50% menos de tinta.";
            profile = PassiveProfile.MuyBuenaConContra;
        }

        public override void OnEquip(PassiveManager manager, PlayerManager playerManager)
        {
            cachedPlayer = playerManager;
        }

        public override void ModifyDamageDealt(ref int damageDealt)
        {
            // Reduce base damage by half (ignoring cards/objects, simplifying to halving total raw output)
            // But uses 10% of current ink as damage bonus
            
            int baseReduced = damageDealt / 2;
            int inkBonus = 0;
            
            if (cachedPlayer != null)
            {
                inkBonus = cachedPlayer.GetInk() / 10;
            }

            damageDealt = baseReduced + inkBonus;
        }

        public override void ModifyInkReward(ref int inkReward, bool isElite)
        {
            // Reduce ink gained by 50%
            inkReward /= 2;
        }
    }
}
