using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    /// <summary>
    /// 
    /// Current stuff:
    ///     How does drops effect interpolation or any of that stuff ? is that allowed ?
    ///     What happens to the system when we use Physics.Synctransforms ?
    ///         Should we do that after the returntopresent(); step ?
    ///             I think in this context it doesn't really matter,
    ///             but it prolly does with RB.
    /// Other stuff:
    ///     make it so that all the unlocal clients are sent as an array of pos rots and
    ///     then when unloading give insta sync to physics.
    ///     
    /// </summary>
    public class Weapon : NetworkBehaviour
    {
        [Header("References")]

        [SerializeField] private HitMarker blueHitMarker = default;
        [SerializeField] private HitMarker redHitMarker = default;

        [SerializeField] private NetworkPhysicsMovement movement = default;
        [SerializeField] private MouseMovement mouseMovement = default;
        [SerializeField] private PlayerEntity entity = default;
        [SerializeField] private LayerMask environmentMask = default;
        [SerializeField] private Transform eyes = null;
        [SerializeField] private CharacterController controller = default;
        [SerializeField] private AudioSource source = default;
        [SerializeField] private GameObject beamGraphic = default;
        [SerializeField] private Image overlay = default;

        [Header("Settings")]

        [SerializeField] private float range = default;
        [SerializeField] private float damage = default;
        [SerializeField] private float step = default;
        [SerializeField] private float maxCooldown = default;

        [Header("User")] 

        [SerializeField] private Image fillImage = default;
        [SerializeField] private AudioClip sound = default;
        [SerializeField] private Color fullColor = default;
        [SerializeField] private Color fadeColor = default;
        [SerializeField] private bool serverCanShootOverride = default;
        [SerializeField] private float shootEffectTime = default;

        private float cooldownTimer;
        private Transform cam; // we want to use the cam because its the "most accurate" for the client.
        private CameraHandlerInterpolate cameraHandlerInterpolate;
        private readonly List<ShootMessage> shootMessages = new List<ShootMessage>();
        private float beamTime;

        private void Awake()
        {
            cam = GameObject.FindWithTag("MainCamera").transform;
            cameraHandlerInterpolate = cam.GetComponent<CameraHandlerInterpolate>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            TargetSyncCooldown(connectionToClient, cooldownTimer, NetworkTime.time);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            source.spatialBlend = 0f;

            beamGraphic = cam.GetChild(0).gameObject;
        }

        private void Update()
        {
            if (isLocalPlayer || isServerOnly) { ChargeCooldown(); }

            if (!isLocalPlayer) { return; }

            if (Input.GetKeyDown(KeyCode.Mouse0) && CanCast())
            {
                Transform hit = InstantRaycast(cam.position, cam.forward);

                if (hit != null) { blueHitMarker.Damage(damage); }

                /*CmdShoot(new ShootMessage(cameraHandlerInterpolate.lerpValue, movement.id - 1,
                    mouseMovement.rotation.y, mouseMovement.rotation.x, movement.currentTick - 1,
                    cam.position, hit ? hit.position : Vector3.zero));*/

                cooldownTimer = 0f;

                source.PlayOneShot(sound);

                beamTime = Time.time + shootEffectTime;
            }
        }

        private void FixedUpdate()
        {
            beamGraphic.SetActive(beamTime >= Time.time);

            if (isLocalPlayer)
            {
                fillImage.fillAmount = cooldownTimer / maxCooldown; fillImage.color = CanCast() ? Color.white : Color.gray;
                overlay.color = beamTime >= Time.time ? fullColor : fadeColor;
            }
        }

        private bool CanCast()
        {
            return cooldownTimer >= maxCooldown;
        }

        private void ChargeCooldown()
        {
            cooldownTimer += Time.deltaTime;
            cooldownTimer = Mathf.Clamp(cooldownTimer, 0f, maxCooldown);
        }

        public void TryShoot()
        {
            for (int i = shootMessages.Count - 1; i >= 0; i--)
            {
                if (shootMessages[i].shotTick <= movement.processedTick)
                {
                    if (CanCast() && serverCanShootOverride)
                    {
                        Shoot(shootMessages[i]);
                        cooldownTimer = 0f;

                        RpcShoot(shootMessages[i].yRotation, shootMessages[i].xRotation);
                    }

                    shootMessages.RemoveAt(i);

                    TargetSyncCooldown(connectionToClient, cooldownTimer, NetworkTime.time);
                }
            }
        }

        [TargetRpc]
        public void TargetCallDamage(NetworkConnectionToClient conn, float damage)
        {
            redHitMarker.Damage(damage);
        }

        [TargetRpc]
        protected void TargetSyncCooldown(NetworkConnectionToClient conn, float cooldown, double sendTime)
        {
            if (cooldown != 0f) { source.Stop(); beamTime = 0f; blueHitMarker.Damage(-damage); Debug.LogWarning("unable to shoot !"); }

            cooldownTimer = cooldown + (float)(NetworkTime.time - sendTime);
            cooldownTimer = Mathf.Clamp(cooldownTimer, 0f, maxCooldown);
        }

        /// <summary>
        /// This only works because the player model isnt effected by aiming direction.
        /// </summary>
        [ClientRpc]
        private void RpcShoot(float yRotation, float xRotation)
        {
            if (isLocalPlayer) { return; }

            // we dont need this because the thing works already.
            transform.rotation = Quaternion.AngleAxis(yRotation, Vector3.up);
            eyes.localRotation = Quaternion.AngleAxis(xRotation, Vector3.right);

            source.PlayOneShot(sound);

            //beamGraphic.SetActive(true);

            beamTime = Time.time + shootEffectTime;

            // also flicker beam graphic.
        }

        [Command]
        private void CmdShoot(ShootMessage shootMessage)
        {
            shootMessage.Verify();

            // I think this is useful, need to test more.
            if (shootMessage.shotTick <= movement.processedTick)
            {
                if (CanCast() && serverCanShootOverride)
                {
                    Shoot(shootMessage);
                    cooldownTimer = 0f;

                    RpcShoot(shootMessage.yRotation, shootMessage.xRotation);
                }

                TargetSyncCooldown(connectionToClient, cooldownTimer, NetworkTime.time);
            }

            shootMessages.Add(shootMessage);
        }

        [Server]
        private void Shoot(ShootMessage shootMessage)
        {
            // or you could just send the rotation since you dont have to look where u aiming anyway.
            transform.rotation = Quaternion.AngleAxis(shootMessage.yRotation, Vector3.up);
            eyes.localRotation = Quaternion.AngleAxis(shootMessage.xRotation, Vector3.right);

            Vector3 recreatedPos = Vector3.Lerp(movement.previousEyePos, eyes.position, shootMessage.lerpValue);
            Vector3 recreatedDir = eyes.forward;

            // this is always zero so it works
            //Debug.LogWarning();

          //  LagCompensation.instance.RewindTime(shootMessage.id, entity);

            Transform hit = InstantRaycast(recreatedPos, recreatedDir);

            /*float eyeDiff = Vector3.Distance(recreatedPos, shootMessage.cheatEyesPos);
            if (eyeDiff > ServerAuthClientPredSimple.TOLERANCE) { Debug.LogWarning($"eye diff problem : {eyeDiff}"); }

            if (hit != null && shootMessage.cheatEnemyPos != Vector3.zero)
            {
                float enemyDiff = Vector3.Distance(hit.position, shootMessage.cheatEnemyPos);
                if (enemyDiff > ServerAuthClientPredSimple.TOLERANCE) { Debug.LogWarning($"enemy diff problem : {enemyDiff}"); }
            }*/

         //   LagCompensation.instance.ReturnToPresent();

            if (hit != null)
            {
                TargetCallDamage(connectionToClient, damage);

                // we're not putting self-damage off the table.
                // # think about the way bugs will appear if they do, you dont want the damage function itself to be the last line of defense, you cant
                // damage yourself because you cant't shoot yourself after all. so if you are able to shoot yourself we basically WANT that to be a big issue so it gets fixed quickly.
                // think: overwatch bastion ult bug >= vs ==
                hit.GetComponent<PlayerHealth>().Damage(damage);
            }
        }

        private Transform InstantRaycast(Vector3 pos, Vector3 dir)
        {
            float rayLength = range;

            RaycastHit hit;

            if (Physics.Raycast(pos, dir, out hit, range, environmentMask, QueryTriggerInteraction.Ignore))
            {
                rayLength = hit.distance;
            }

            int amount = Mathf.RoundToInt(rayLength / step); // closest guess with regards to very thin surfaces...

            // <= because the first is zero length if that makese sense.
            /*for (int l = 0; l <= amount; l++)
            {
                for (int p = 0; p < LagCompensation.instance.players.Count; p++)
                {
                    // !performance, you could also just remove the player from the list
                    if (LagCompensation.instance.players[p] == this) { continue; }

                    *//*if (Intersections.IsPointWithinCapsule(pos + (l * step * dir), LagCompensation.instance.players[p].movement.transform.position, controller.height, controller.radius))
                    {
                        return LagCompensation.instance.players[p].transform;
                    }*//*
                }
            }*/

            return null;
        }

        private struct ShootMessage
        {
            public float lerpValue;
            public uint id;
            public float yRotation;
            public float xRotation;

            public int shotTick;

            public Vector3 cheatEyesPos;
            public Vector3 cheatEnemyPos;

            public ShootMessage(float lerpValue, uint id, float yRotation, float xRotation, int shotTick, Vector3 cheatEyesPos, Vector3 cheatEnemyPos)
            {
                this.lerpValue = lerpValue;
                this.id = id;
                this.yRotation = yRotation;
                this.xRotation = xRotation;
                this.shotTick = shotTick;
                this.cheatEyesPos = cheatEyesPos;
                this.cheatEnemyPos = cheatEnemyPos;
            }

            public void Verify()
            {
                lerpValue = Mathf.Clamp(lerpValue, 0f, 1f);

                xRotation = Mathf.Clamp(xRotation, -MouseMovement.MAX_CAM_ANGLE, MouseMovement.MAX_CAM_ANGLE);
            }
        }
    }
}