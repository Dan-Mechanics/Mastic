using Mirror;
using UnityEngine;

namespace Mastic
{
    public class PlayerNetworkSetup : NetworkBehaviour
    {
        [SerializeField] private Object[] localRemove = null;
        [SerializeField] private Object[] unlocalRemove = null;
        [SerializeField] private Object[] serverRemove = null;

        private void Start() => Setup();

        /// <summary>
        /// You could also use ENABLED because that's less performance when joining.
        /// </summary>
        private void Setup()
        {
            gameObject.name = $"player";

            if (!isServerOnly)
            {
                if (isLocalPlayer)
                {
                    gameObject.name += " | local client";

                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;

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

            Debug.Log($"{gameObject.name} : setup completed.");

            Destroy(this);
        }

        private void RemoveAll(Object[] component) 
        {
            for (int i = 0; i < component.Length; i++)
            {
                Remove(component[i]);
            }
        }

        private void Remove(Object component) 
        {
            // ??
            if (component == null) { return; }

            Destroy(component);
        }
    }
}