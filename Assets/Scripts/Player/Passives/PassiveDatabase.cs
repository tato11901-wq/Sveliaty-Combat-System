using UnityEngine;
using System.Collections.Generic;

namespace Sveliaty.Passives
{
    [CreateAssetMenu(menuName = "Sveliaty/Passives/Passive Database")]
    public class PassiveDatabase : ScriptableObject
    {
        public List<PassiveSkill> allPassives = new List<PassiveSkill>();

        public List<PassiveSkill> GetThreeRandomPassives()
        {
            if (allPassives.Count <= 3) return new List<PassiveSkill>(allPassives);

            List<PassiveSkill> result = new List<PassiveSkill>();
            List<PassiveSkill> pool = new List<PassiveSkill>(allPassives);

            // Intentar obtener 1 de cada perfil (Si es posible)
            PassiveSkill muyBuena = pool.Find(p => p.profile == PassiveProfile.MuyBuenaConContra);
            if (muyBuena != null) { result.Add(muyBuena); pool.Remove(muyBuena); }

            PassiveSkill solida = pool.Find(p => p.profile == PassiveProfile.SolidaSinContra);
            if (solida != null) { result.Add(solida); pool.Remove(solida); }

            PassiveSkill situacional = pool.Find(p => p.profile == PassiveProfile.Situacional);
            if (situacional != null) { result.Add(situacional); pool.Remove(situacional); }

            // Llenar el resto si faltan
            while (result.Count < 3 && pool.Count > 0)
            {
                int index = Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return result;
        }
    }
}
