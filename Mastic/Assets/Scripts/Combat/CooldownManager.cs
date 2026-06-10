using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class CooldownManager : MonoBehaviour
    {
        [SerializeField] private List<Object> abilities = default;

        public void Initialize(CooldownHandler cooldownHandler, CooldownDisplay cooldownDisplayer)
        {
            List<string> names = new List<string>();
            abilities.ForEach(x => names.Add(x.GetType().Name.ToLowerInvariant()));

            cooldownHandler.Initialize(names);
            cooldownDisplayer.Initialize(cooldownHandler, names);
        }
    }
}
