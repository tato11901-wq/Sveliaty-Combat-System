using UnityEngine;


namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/Discount Store")]
    public class PassiveDiscountStore : PassiveSkill
    {
        private void Reset()
        {
            passiveName = "Tienda de Descuentos";
            description = "Todo en la tienda cuesta un 50% menos.\nContra: Pierdes 5 de tinta cada vez que recibes daño y solo ganas tinta en combates Élite.";
            profile = PassiveProfile.MuyBuenaConContra;
        }

        public override void ModifyShopPrice(ref int price)
        {
            price /= 2;
        }

        public override void OnDamageTaken(int damage, CombatManager combatManager)
        {
            if (combatManager.playerManager != null)
            {
                // Penalización fija por cada golpe que logre penetrar armadura (damage > 0)
                if (damage > 0)
                {
                    combatManager.playerManager.AddInk(-5);
                    Debug.Log("[Pasiva DiscountStore] Perdiste 5 de tinta por recibir daño.");
                }
            }
        }

        public override void ModifyInkReward(ref int inkReward, bool isElite)
        {
            if (!isElite)
            {
                inkReward = 0;
                Debug.Log("[Pasiva DiscountStore] Recompensa de tinta anulada por no ser Élite.");
            }
        }
    }
}
