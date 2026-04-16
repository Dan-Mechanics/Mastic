using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Method whereby the cast time on the server is consistent and secure.
    /// </summary>
    public class FireStrike : NetworkBehaviour
    {
        [SerializeField] private Transform eyes = null;
        [SerializeField] private CharacterController controller = null;
        [SerializeField] private Weapon weapon = null;
        [SerializeField] private GameObject fireStrikePrefab = null;
        [SerializeField] private CapsuleCollider hitbox = null;
        [SerializeField] private AudioSource source = null;
        [SerializeField] private AudioClip sound = null;

        [SerializeField] private float projectileSpeed = 0f;
        [SerializeField] private float castTime = 0f;

        private void Update()
        {
            if (!isLocalPlayer) { return; }

            if (Input.GetKeyDown(KeyCode.E)) 
            {
                CmdCast();
                source.PlayOneShot(sound);
            }
        }

        /// <summary>
        /// ADD: cooldown and recovery or something ...
        /// </summary>
        [Command]
        private void CmdCast() 
        {
            // wait for casttime

            Invoke(nameof(Cast), castTime);
        }

        [Server]
        private void Cast() 
        {
            GameObject proj = Instantiate(fireStrikePrefab, eyes.position, Quaternion.LookRotation(eyes.forward));

            proj.transform.forward = eyes.forward;
            proj.GetComponent<Rigidbody>().linearVelocity = eyes.forward * projectileSpeed;
            Physics.IgnoreCollision(proj.GetComponent<SphereCollider>(), controller);

            proj.GetComponent<FireStrikeBehaviour>().Setup(weapon, true);

            RpcCast(eyes.position, eyes.forward);
        }

        [ClientRpc]
        private void RpcCast(Vector3 pos, Vector3 forward) 
        {
            GameObject proj = Instantiate(fireStrikePrefab, pos, Quaternion.LookRotation(forward));

            proj.transform.forward = forward;
            proj.GetComponent<Rigidbody>().linearVelocity = forward * projectileSpeed;
            if (controller != null) { Physics.IgnoreCollision(proj.GetComponent<SphereCollider>(), controller); }
            else { Physics.IgnoreCollision(proj.GetComponent<SphereCollider>(), hitbox); }

            proj.GetComponent<FireStrikeBehaviour>().Setup(null, false);
        }
    }
}