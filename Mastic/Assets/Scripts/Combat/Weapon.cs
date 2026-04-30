using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    /// <summary>
    /// Add: cooldown integration.
    /// </summary>
    public class Weapon : NetworkBehaviour, IShootable
    {
        /// <summary>
        /// Meaning that the local client has dealed damage to an enemy on the server.
        /// </summary>
        public event Action<float> OnDealDamage;
        
        [Header("References")]
        [SerializeField] private HitMarker blueHitMarker = default;
        [SerializeField] private HitMarker redHitMarker = default;

        [SerializeField] private Rigidbody rb = default;
        [SerializeField] private NetworkMovement movement = default;
        [SerializeField] private PlayerEntity playerEntity;
        [SerializeField] private PlayerLook playerLook;
        [SerializeField] private Transform eyes = null;

        [SerializeField] private AudioSource source = default;
        [SerializeField] private GameObject beamGraphic = default;
        [SerializeField] private Image overlay = default;
        [SerializeField] private Image fillImage = default;
        [SerializeField] private AudioClip sound = default;
        [SerializeField] private Color fullColor = default;
        [SerializeField] private Color fadeColor = default;

        [Header("Settings")]
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private float range = default;
        [SerializeField] private float damage = default;
        //[SerializeField] private float step = default;
        [SerializeField] private float cooldown = default;
        [SerializeField] private bool serverCanShootOverride = default;
        [SerializeField] private float shootEffectTime = default;

        private LagCompensation lagCompensation;
        private ICameraInterpolation cameraInterpolation;

        private Transform cam;
        private Vector3 prevPosition;
        private Vector3 currPosition;

        private readonly List<ShootMessage> pendingShootMessages = new List<ShootMessage>();
        private float beamTime;
        private float cooldownTimer;

        private void Awake()
        {
            cam = GameObject.FindWithTag("MainCamera").transform;
            cameraInterpolation = cam.GetComponent<ICameraInterpolation>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
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
                RaycastHit hit;

                if (Physics.Raycast(cam.position, eyes.forward, out hit, range, mask, QueryTriggerInteraction.Ignore) && IsPlayer(hit))
                {
                    blueHitMarker.Damage(damage);
                }

                // !allocation this can be done better.
                // so we are now using our own tick which is lowkey sketch but it works.
                CmdShoot(new ShootMessage(cameraInterpolation.lerpValue, movement.id - 1,
                    playerLook.RotationY, playerLook.RotationY, movement.currentTick - 1));

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
                fillImage.fillAmount = cooldownTimer / cooldown; fillImage.color = CanCast() ? Color.white : Color.gray;
                overlay.color = beamTime >= Time.time ? fullColor : fadeColor;
            }
        }

        private bool CanCast()
        {
            return cooldownTimer >= cooldown;
        }

        private void ChargeCooldown()
        {
            cooldownTimer += Time.deltaTime;
            cooldownTimer = Mathf.Clamp(cooldownTimer, 0f, cooldown);
        }

        public void CheckShootMessages() 
        {
            for (int i = pendingShootMessages.Count - 1; i >= 0; i--)
            {
                if (pendingShootMessages[i].movementTick > movement.processedTick) { continue; }

                if (CanCast() && serverCanShootOverride)
                {
                    Shoot(pendingShootMessages[i]);
                    cooldownTimer = 0f;

                    // yah and also send message if you couldnt shoot because then we have to delete the prediction.
                    RpcShoot(pendingShootMessages[i].yRotation, pendingShootMessages[i].xRotation);
                }

                pendingShootMessages.RemoveAt(i);

                TargetSyncCooldown(connectionToClient, cooldownTimer, NetworkTime.time);
            }
        }

        [TargetRpc]
        public void TargetDisplayHitPip(NetworkConnectionToClient conn, float damage) 
        {
            redHitMarker.Damage(damage);
        }

        [TargetRpc]
        private void TargetSyncCooldown(NetworkConnectionToClient conn, float cooldown, double sendTime)
        {
            if (cooldown != 0f) 
            { 
                source.Stop();
                beamTime = 0f;
                blueHitMarker.Damage(-damage);

                Debug.LogWarning("unable to shoot !");
            }

            cooldownTimer = cooldown + (float)(NetworkTime.time - sendTime);
            cooldownTimer = Mathf.Clamp(cooldownTimer, 0f, this.cooldown);
        }

        /// <summary>
        /// This only works because the player model isnt effected by aiming direction...
        /// </summary>
        [ClientRpc]
        private void RpcShoot(float yRotation, float xRotation) 
        {
            if (isLocalPlayer) { return; }

            // we dont need this because the thing works already ... ???
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
            pendingShootMessages.Add(shootMessage);

            /*if (shootMessage.shotTick <= movement.processedTick)
            {

                if (CanCast() && serverCanShootOverride)
                {
                    Shoot(shootMessage);
                    cooldownTimer = 0f;

                    // yah and also send message if you couldnt shoot because then we have to delete the prediction.
                    RpcShoot(shootMessage.yRotation, shootMessage.xRotation);
                }

                TargetSyncCooldown(connectionToClient, cooldownTimer, NetworkTime.time);
            }
            else
            {
                shootMessages.Add(shootMessage);
            }*/
        }

        [Server]
        private void Shoot(ShootMessage shootMessage) 
        {
            lagCompensation.SetAsTick(shootMessage.rollbackTick);
            
            playerLook.SetAsRotation(shootMessage.xRotation, shootMessage.yRotation);
            cameraInterpolation.Interject(currPosition, prevPosition, rb.linearVelocity);
            cameraInterpolation.SetValue(shootMessage.lerpValue);

            Physics.SyncTransforms();
            if (!Physics.Raycast(cam.position, eyes.forward,
                out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
                return;

            if (!hit.transform.root.TryGetComponent(out IDamagable damagable))
                return;

            damagable.Damage(damage);
            TargetDisplayHitPip(connectionToClient, damage);
            hit.transform.root.GetComponent<PlayerHealth>().Damage(damage);
        }

        [Server]
        public void DoShootTick(int movementTick)
        {
            // TODO: MAKE SURE THIS IS CORRECT.
            prevPosition = currPosition;
            currPosition = transform.position;

            // !FIX
            for (int i = pendingShootMessages.Count - 1; i >= 0; i--)
            {
                if (pendingShootMessages[i].movementTick > movementTick)
                    continue;

                if (CanCast() && serverCanShootOverride)
                {
                    Shoot(pendingShootMessages[i]);
                    cooldownTimer = 0f;

                    // yah and also send message if you couldnt shoot because then we have to delete the prediction.
                    RpcShoot(pendingShootMessages[i].yRotation, pendingShootMessages[i].xRotation);
                }

                pendingShootMessages.RemoveAt(i);
                TargetSyncCooldown(connectionToClient, cooldownTimer, NetworkTime.time);
            }
        }
    }
}