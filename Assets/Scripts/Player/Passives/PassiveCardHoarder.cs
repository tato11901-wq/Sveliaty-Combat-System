using UnityEngine;
using System.Collections.Generic;
using System;

namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/Card Hoarder")]
    public class PassiveCardHoarder : PassiveSkill
    {
        private void Reset()
        {
            passiveName = "Acaparador de Cartas";
            description = "Recibes 10 cartas aleatorias inmediatamente.\nContra: No puedes ganar más cartas durante el resto de la partida.";
            profile = PassiveProfile.MuyBuenaConContra;
        }

        public override void OnEquip(PassiveManager manager, PlayerManager playerManager)
        {
            if (playerManager != null)
            {
                // Dar 10 cartas aleatorias
                AffinityType[] allTypes = (AffinityType[])Enum.GetValues(typeof(AffinityType));
                
                for(int i = 0; i < 10; i++)
                {
                    AffinityType randomType = allTypes[UnityEngine.Random.Range(0, allTypes.Length)];
                    playerManager.AddCards(randomType, 1);
                }
                
                Debug.Log("[Pasiva] Otorgadas 10 cartas aleatorias.");
            }
        }

        public override bool CanGainCards()
        {
            return false;
        }
    }
}
