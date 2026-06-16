using Mirror;
using UnityEngine;

namespace Mastic
{
    public class PlayerReset : NetworkBehaviour
    {
        [SerializeField] private EasyBinding reload = default;
        private IDamagable damagable;

        private void Awake()
            => damagable = GetComponent<IDamagable>();

        private void Update()
        {
            if (isLocalPlayer && reload.WasPressed)
                CmdReset();
        }

        [Command]
        private void CmdReset()
            => damagable.Die();
    }
}
