using Mirror;
using UnityEngine;

namespace Mastic
{
    public class PlayerInitializer : NetworkBehaviour
    {
        [SerializeField] private string defaultName = default;
        [SerializeField] private Object[] localRemove = default;
        [SerializeField] private Object[] unlocalRemove = default;
        [SerializeField] private Object[] serverRemove = default;

        private void Start()
        {
            gameObject.name = defaultName;
            if (!isServer)
            {
                if (isLocalPlayer)
                {
                    gameObject.name += " | local client";
                    RemoveAll(localRemove);
                }
                else
                {
                    gameObject.name += " | unlocal client";
                    RemoveAll(unlocalRemove);
                }
            }
            else
            {
                gameObject.name += " | server";
                RemoveAll(serverRemove);
            }

            print($"{gameObject.name}: setup completed.");
            Destroy(this);
        }

        private void RemoveAll(Object[] components) 
        {
            for (int i = 0; i < components.Length; i++)
            {
                Destroy(components[i]);
            }
        }
    }
}