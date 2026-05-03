using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mastic
{
    public class NetworkMovement : NetworkBehaviour
    {
        public int MovementTick => currentTick;

        public event Action<bool, int> OnPendingBufferChanged;
        public event Action<int> OnCurrentTickChanged;
        public event Action<string> OnCheatsChanged;
        public event Action<bool> OnReconsileStateChanged;
        public event Action<StateMessage> OnReceiveAuthoritativeState;

        [HideInInspector] public int processedTick;
        [HideInInspector] public uint id;
        [HideInInspector] public Vector3 previousEyePos;

        [SerializeField] private Rigidbody rb = default;
        [SerializeField] private Transform eyes = default;
        [SerializeField] private MouseLook mouseLook = default;
        [SerializeField] private PhysicsMovement physicsMovement = default;
        [SerializeField] private GameObject authGraphicPrefab = default;
        [SerializeField] private EasyBinding forward = default;
        [SerializeField] private EasyBinding left = default;
        [SerializeField] private EasyBinding backward = default;
        [SerializeField] private EasyBinding right = default;
        [SerializeField] private UnityEvent onReconsile = default;

        private int currentTick;

        private int clientPacketMultiplier;
        //private bool clientDropMessage;
        private Transform serverAuthGraphic;
        private ICameraInterpolation interpolation;

        public const int BUFFER_SIZE = 64;
        public const int MAX_PENDING_INPUT_COUNT = 8;
        public const float TOLERANCE = 0.001f;

        private readonly List<InputMessage> pendingInputMessages = new List<InputMessage>();
        private readonly StateMessage[] stateBuffer = new StateMessage[BUFFER_SIZE];
        private readonly InputMessage[] inputBuffer = new InputMessage[BUFFER_SIZE];

        private StateMessage mostRecentServerStateMessage;
        private InputMessage previousInputMessage;

        private int receivedTick = -1; // CARE: this used to be 0.
        private bool hasReceivedFirstMessage;

        private bool bufferHasTicks;
        private float standardInterval;

        private bool w, a, s, d;
        private float timer;

        public void Setup(int standardTickrate, ICameraInterpolation interpolation)
        {
            standardInterval = 1f / standardTickrate;
            this.interpolation = interpolation;
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            serverAuthGraphic = Instantiate(authGraphicPrefab, Vector3.zero, Quaternion.identity).transform;

            //SetTickrate(fullTickrate, true);

            for (int i = 0; i < stateBuffer.Length; i++)
            {
                stateBuffer[i].position = transform.position;
            }

            previousInputMessage.tick = -1;
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            // if (Input.GetKeyDown(KeyCode.Mouse3)) { clientDropMessage = true; }
            if (Input.GetKey(KeyCode.Mouse4)) { clientPacketMultiplier = 2; }
            else if (Input.GetKey(KeyCode.Mouse2)) { clientPacketMultiplier = 0; }
            else { clientPacketMultiplier = 1; }
            
            timer += Time.deltaTime;
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;

                OnCheatsChanged?.Invoke($"cheats: {clientPacketMultiplier}");

                for (int i = 0; i < clientPacketMultiplier; i++)
                {
                    DoClientTick();
                }
            }

            /*if (Input.GetKeyDown(KeyCode.UpArrow)) { currentTick += 10; Debug.LogWarning("+10"); }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { currentTick -= 10; Debug.LogWarning("-10"); }*/
        }

        [Client]
        private void DoClientTick()
        {
            // grounded ?? --> no
            if (Input.GetKey(KeyCode.Space) && physicsMovement.JumpAbility.CanPerform(this))
            {
                physicsMovement.JumpAbility.AddRequestTick(currentTick);
                CmdSendJumpTick(currentTick);
            }

            // COOLDOWN !!!!!
            if (Input.GetKey(KeyCode.LeftShift) && physicsMovement.DashAbility.CanPerform(this))
            {
                physicsMovement.DashAbility.AddRequestTick(currentTick);
                CmdSendDashTick(currentTick);
            }



            int inputBufferIndex = currentTick % BUFFER_SIZE;

            if (Application.isFocused) 
            {
                w = forward.IsHeld;
                a = left.IsHeld;
                s = backward.IsHeld;
                d = right.IsHeld;
            }

            inputBuffer[inputBufferIndex].SetValues(w, a, s, d, mouseLook.RotationX, mouseLook.RotationY, currentTick);

            Move(inputBuffer[inputBufferIndex], true);

            stateBuffer[inputBufferIndex].SetValues(transform.position, rb.linearVelocity, inputBuffer[inputBufferIndex]);

            /*if (!clientDropMessage && !Input.GetKey(KeyCode.E)) { CmdSendInputMessageToServer(inputBuffer[ringBufferIndex]); }
            else { OnCheatsChanged?.Invoke("not sending ..."); clientDropMessage = false; }*/

            CmdSendInputMessageToServer(inputBuffer[inputBufferIndex]);

            // we do it here because then the first is 0.
            OnCurrentTickChanged?.Invoke(currentTick);
            currentTick++;
        }

        /// <summary>
        /// Process player on the server.
        /// </summary>
        /// <returns>stateBufferIndex</returns>
        [Server]
        public int DoServerTick()
        {
            //TryApplyEffect();
            OnPendingBufferChanged?.Invoke(hasReceivedFirstMessage, pendingInputMessages.Count);

            InputMessage inputMessageToProcess;

            bufferHasTicks = false;

            if (pendingInputMessages.Count > 0)
            {
                inputMessageToProcess = pendingInputMessages[0];
                pendingInputMessages.RemoveAt(0);

                if (inputMessageToProcess.tick < 0)
                {
                    inputMessageToProcess = GetDefaultedInputMessage();
                }
                else
                {
                    bufferHasTicks = true;
                    hasReceivedFirstMessage = true;
                }
            }
            else
            {
                inputMessageToProcess = GetDefaultedInputMessage();
            }

            int stateBufferIndex = inputMessageToProcess.tick % BUFFER_SIZE;

            previousEyePos = eyes.position;
            Move(inputMessageToProcess, false);

            stateBuffer[stateBufferIndex].SetValues(transform.position, rb.linearVelocity, inputMessageToProcess);
            //TargetSendAuthState(connectionToClient, stateBuffer[stateBufferIndex]);
            //RpcSendStateMessageToClients(transform.position, transform.rotation, eyes.localRotation);

            processedTick = inputMessageToProcess.tick;

            physicsMovement.CleanTicks(processedTick);

            // ??
            if (previousInputMessage.tick != processedTick - 1) 
            { 
                Debug.LogWarning($"we made a tick jump on the input tick --> if ({previousInputMessage.tick} != {processedTick - 1})");
            }

            previousInputMessage = inputMessageToProcess;

            currentTick++;

            return stateBufferIndex;
        }

        /// <summary>
        /// This needs to be here because of the difference
        /// in ordering of Physics.Simulate between the server and client.
        /// </summary>
        [Server]
        public void SendAuthStateToClient(int stateBufferIndex) 
        {
            // REFRESH THE POS AND VEL TO MAKE IT CORRECT.
            stateBuffer[stateBufferIndex].position = transform.position;
            stateBuffer[stateBufferIndex].velocity = rb.linearVelocity;
            TargetSendStateMessageToClient(connectionToClient, stateBuffer[stateBufferIndex]);
        }

        /// <summary>
        /// This is because afte the simulation step
        /// the velocity is unstable, so we limit it.
        /// </summary>
        public void LimitSpeed() => physicsMovement.LimitSpeed();

        private InputMessage GetDefaultedInputMessage()
        {
            InputMessage inputMessage = previousInputMessage;
            inputMessage.tick++;
            return inputMessage;
        }

        [Command]
        private void CmdSendJumpTick(int tick) 
        {
            physicsMovement.JumpAbility.AddRequestTick(tick);
        }

        [Command]
        private void CmdSendDashTick(int tick)
        {
            physicsMovement.DashAbility.AddRequestTick(tick);
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSendInputMessageToServer(InputMessage inputMessage)
        {
            // MAKE SURE THIS IS CORRECT.
            if (inputMessage.tick < 0 || inputMessage.tick <= receivedTick)
                return;

            if (inputMessage.tick > receivedTick + 1 && hasReceivedFirstMessage && bufferHasTicks)
            {
                for (int i = 0; i < inputMessage.tick - receivedTick - 1; i++)
                {
                    InputMessage clone = inputMessage;
                    clone.tick -= i + 1;

                    // if we already defaulted this, then there's no point !
                    if (clone.tick > previousInputMessage.tick)
                        pendingInputMessages.Add(clone);
                }
            }

            receivedTick = inputMessage.tick;

            pendingInputMessages.Add(inputMessage);

            // remove if too many pending ...
            while (pendingInputMessages.Count > MAX_PENDING_INPUT_COUNT)
            {
                pendingInputMessages.RemoveAt(pendingInputMessages.Count - 1);
            }
        }

        [TargetRpc(channel = Channels.Unreliable)]
        private void TargetSendStateMessageToClient(NetworkConnectionToClient conn, StateMessage stateMessage)
        {
            if (stateMessage.tick > currentTick - 1)
            {
                Debug.LogWarning("we have to return here since the positions are stored in a ringbuffer and otherwise would wrap around and completely break the reconsile.");
                return;
            }

            // we might need to remove this if we use server ticks.
            if (stateMessage.tick <= mostRecentServerStateMessage.tick)
            {
                Debug.LogWarning("we have to return here since we already reconsiled on this tick and we cant do it twice, yes this means we have the possibility of missing reconsiles but that's acceptable since we get the next message next.");
                return;
            }

            mostRecentServerStateMessage = stateMessage;
            OnReceiveAuthoritativeState?.Invoke(mostRecentServerStateMessage);

            physicsMovement.CleanTicks(mostRecentServerStateMessage.tick);

            TryReconsiliation();
        }

        [Client]
        private void TryReconsiliation()
        {
            int serverStateBufferIndex = mostRecentServerStateMessage.tick % BUFFER_SIZE;

            bool shouldReconsile = Vector3.Distance(mostRecentServerStateMessage.position, stateBuffer[serverStateBufferIndex].position) > TOLERANCE;

            if (shouldReconsile) { DoReconsile(serverStateBufferIndex); }

            OnReconsileStateChanged?.Invoke(shouldReconsile);
        }

        [Client]
        private void DoReconsile(int serverStateBufferIndex)
        {
            if (Application.isFocused)
                onReconsile?.Invoke();

            Debug.LogWarning($"We have to reconcile for {mostRecentServerStateMessage.tick} | if ({mostRecentServerStateMessage.position} != {stateBuffer[serverStateBufferIndex].position}).");
            //Debug.LogWarning(Vector3.Distance(mostRecentServerStateMessage.position, stateBuffer[serverStateBufferIndex].position).ToString());
            
            //Teleport(mostRecentServerStateMessage.position, mostRecentServerStateMessage.velocity);
            physicsMovement.Teleport(mostRecentServerStateMessage.position, mostRecentServerStateMessage.velocity);

            stateBuffer[serverStateBufferIndex] = mostRecentServerStateMessage;

            int tickToProcess = mostRecentServerStateMessage.tick + 1;

            while (tickToProcess < currentTick)
            {
                int stateBufferIndex = tickToProcess % BUFFER_SIZE;

                Vector3 prev = eyes.position;

                Move(inputBuffer[stateBufferIndex], false);

                interpolation.Interject(eyes.position, prev, rb.linearVelocity);

                stateBuffer[stateBufferIndex].SetValues(transform.position, rb.linearVelocity, inputBuffer[stateBufferIndex]);

                tickToProcess++;
            }
        }

        private void Move(InputMessage input, bool assignToCamera)
        {
            mouseLook.SetAsRotation(input.xRotation, input.yRotation);
            physicsMovement.Move(input.GetVerticalInput(), input.GetHorizontalInput(), standardInterval, input.tick);

            if (isClient)
            {
                Physics.Simulate(standardInterval);
                LimitSpeed();
            }

            if (assignToCamera)
                interpolation.Assign(eyes.position, rb.linearVelocity);
        }
    }
}