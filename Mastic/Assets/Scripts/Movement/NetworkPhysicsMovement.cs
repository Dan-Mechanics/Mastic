using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mastic
{
    public class NetworkPhysicsMovement : NetworkBehaviour
    {
        public int CurrentTick => currentTick;
        public bool HasReceivedFirstMessage => hasReceivedFirstMessage;

        public event Action<int> OnBeforeServerTick;
        public event Action<int> OnCurrentTickChanged;
        public event Action<string> OnCheatsChanged;
        public event Action<bool> OnReconsileStateChanged;

        // this is not best practice.

        [HideInInspector] public int processedTick;
        [HideInInspector] public uint id;
        [HideInInspector] public Vector3 previousEyePos;

        // ---

        [Header("References")]

        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private Transform eyes = null;
        [SerializeField] private MouseMovement mouseMovement = null;
        [SerializeField] private PhysicsMovement physicsMovement = null;
        [SerializeField] private GameObject authGraphicPrefab = null;
        [SerializeField] private UnityEvent onSlosh = null;

        private int currentTick;

        private int clientPacketMultiplier;
        //private bool clientDropMessage;
        private Transform serverAuthGraphic;
        private CameraHandler cameraHandler;

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

        private bool w, a, s, d;
        private float timer;

        private void Awake()
        {
            cameraHandler = GameObject.FindWithTag("MainCamera").GetComponent<CameraHandler>();
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
            if (!isLocalPlayer) { return; }

            // if (Input.GetKeyDown(KeyCode.Mouse3)) { clientDropMessage = true; }

            if (Input.GetKey(KeyCode.Mouse4)) { clientPacketMultiplier = 2; }
            else if (Input.GetKey(KeyCode.Mouse2)) { clientPacketMultiplier = 0; }
            else { clientPacketMultiplier = 1; }
            
            

            timer += Time.deltaTime;

            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;

                OnCheatsChanged?.Invoke(clientPacketMultiplier.ToString());

                for (int i = 0; i < clientPacketMultiplier; i++)
                {
                    DoClientTick();
                }
            }

            /*if (Input.GetKeyDown(KeyCode.UpArrow)) { currentTick += 10; Debug.LogWarning("+10"); }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { currentTick -= 10; Debug.LogWarning("-10"); }*/



            // this should not be here I think --> should be in the game closer or something like that.
            if (Input.GetKeyDown(KeyCode.Q)) { connectionToServer.Disconnect(); }
        }

        #region Ticks

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



            int ringBufferIndex = currentTick % BUFFER_SIZE;

            if (Application.isFocused) 
            {
                w = Input.GetKey(KeyCode.W);
                a = Input.GetKey(KeyCode.A);
                s = Input.GetKey(KeyCode.S);
                d = Input.GetKey(KeyCode.D);
            }

            inputBuffer[ringBufferIndex].SetValues(w, a, s, d, mouseMovement.rotation.y, mouseMovement.rotation.x, currentTick);

            Move(inputBuffer[ringBufferIndex], true);

            stateBuffer[ringBufferIndex].SetValues(transform.position, rb.linearVelocity, inputBuffer[ringBufferIndex]);

            /*if (!clientDropMessage && !Input.GetKey(KeyCode.E)) { CmdSendInputMessageToServer(inputBuffer[ringBufferIndex]); }
            else { OnCheatsChanged?.Invoke("not sending ..."); clientDropMessage = false; }*/

            CmdSendInputMessageToServer(inputBuffer[ringBufferIndex]);

            // we do it here because then the first is 0.
            OnCurrentTickChanged?.Invoke(currentTick);
            currentTick++;
        }

        [Server]
        public int DoServerMovementTick()
        {
            //TryApplyEffect();
            OnBeforeServerTick?.Invoke(pendingInputMessages.Count);

            InputMessage inputMessageToProcess;

            bufferHasTicks = false;

            if (pendingInputMessages.Count > 0)
            {
                inputMessageToProcess = pendingInputMessages[0];
                pendingInputMessages.RemoveAt(0);

                if (inputMessageToProcess.tick < 0)
                {
                    inputMessageToProcess = GiveDefaultedTick();
                }
                else
                {
                    bufferHasTicks = true;
                    hasReceivedFirstMessage = true;
                }
            }
            else
            {
                inputMessageToProcess = GiveDefaultedTick();

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

        public void Send(int stateBufferIndex) 
        {
            stateBuffer[stateBufferIndex].position = transform.position;
            stateBuffer[stateBufferIndex].velocity = rb.linearVelocity;

            TargetSendAuthState(connectionToClient, stateBuffer[stateBufferIndex]);
        }

        public void CapVelocity() 
        {
            rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, physicsMovement.SpeedCap);
        }

        private InputMessage GiveDefaultedTick()
        {
            InputMessage inputMessageToProcess = previousInputMessage;
            inputMessageToProcess.tick++;

            return inputMessageToProcess;
        }

        #endregion

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
            if (inputMessage.tick < 0) { return; }

            if (!(inputMessage.tick > receivedTick)) { return; }

            if (inputMessage.tick > receivedTick + 1 && hasReceivedFirstMessage && bufferHasTicks)
            {
                for (int i = 0; i < inputMessage.tick - receivedTick - 1; i++)
                {
                    InputMessage clone = inputMessage;
                    clone.tick -= i + 1;

                    // if we already defaulted this, then there's no point !
                    if (clone.tick > previousInputMessage.tick) { pendingInputMessages.Add(clone); }
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
        private void TargetSendAuthState(NetworkConnectionToClient conn, StateMessage stateMessage)
        {
            if (stateMessage.tick > currentTick - 1)
            {
                Debug.LogWarning("we have to return here since the positions are stored in a ringbuffer and otherwise would wrap around and completely break the reconsile.");
                return;

            }

            // we might need to remove this if we use server ticks.
            if (!(stateMessage.tick > mostRecentServerStateMessage.tick))
            {
                Debug.LogWarning("we have to return here since we already reconsiled on this tick and we cant do it twice, yes this means we have the possibility of missing reconsiles but that's acceptable since we get the next message next.");
                return;
            }

            mostRecentServerStateMessage = stateMessage;

            physicsMovement.CleanTicks(mostRecentServerStateMessage.tick);

            TryReconsiliation();
        }

        [ClientRpc(channel = Channels.Unreliable)]
        public void RpcSendStateMessageToClients(Vector3 pos, Quaternion rot, Quaternion eyeRot) 
        {
            //this.id = id;

            if (isLocalPlayer)
            {
                if (serverAuthGraphic != null)
                {
                    serverAuthGraphic.SetPositionAndRotation(pos + (Vector3.up * 2.5f), rot);
                }
            }
            else
            {
                // TODO: add lerp.
                
                transform.SetPositionAndRotation(pos, rot);
                eyes.localRotation = eyeRot;
            }
        }

        #region Reconsiliation

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
            if (Application.isFocused) { onSlosh?.Invoke(); }

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

                cameraHandler.Interject(eyes.position, prev, rb.linearVelocity);

                stateBuffer[stateBufferIndex].SetValues(transform.position, rb.linearVelocity, inputBuffer[stateBufferIndex]);

                tickToProcess++;
            }
        }

        #endregion

        private void Move(InputMessage input, bool lerp)
        {
            // this is very important.
            transform.rotation = input.CalculateLeftRightRotation();
            eyes.localRotation = input.CalculateUpDownRotation();

            physicsMovement.Move(MasticNetworkManager.STANDARD_FIXED_DELTA_TIME, input);

            if (!isServerOnly)
            {
                Physics.Simulate(MasticNetworkManager.STANDARD_FIXED_DELTA_TIME);

                CapVelocity();
            }

            if (lerp) { cameraHandler.Assign(eyes.position, rb.linearVelocity); }
        }

        public struct InputMessage
        {
            public bool w;
            public bool a;
            public bool s;
            public bool d;
            //public bool space;

            public float yRotation;
            public float xRotation;

            public int tick;

            public void SetValues(bool w, bool a, bool s, bool d, float yRotation, float xRotation, int tick)
            {
                this.w = w;
                this.a = a;
                this.s = s;
                this.d = d;
                //this.space = space;
                this.yRotation = yRotation;
                this.xRotation = Mathf.Clamp(xRotation, -MouseMovement.MAX_CAM_ANGLE, MouseMovement.MAX_CAM_ANGLE);

                this.tick = tick;
            }

            public float CalculateVerticalInput()
            {
                float result = 0f;

                if (w) { result++; }
                if (s) { result--; }

                return result;
            }

            public float CalculateHorizontalInput()
            {
                float result = 0f;

                if (d) { result++; }
                if (a) { result--; }

                return result;
            }

            /// <summary>
            /// ! D.R.Y --> achieved
            /// </summary>
            public Quaternion CalculateLeftRightRotation() => Quaternion.AngleAxis(yRotation, Vector3.up);
            public Quaternion CalculateUpDownRotation() => Quaternion.AngleAxis(xRotation, Vector3.right);
        }

        public struct StateMessage
        {
            public Vector3 position;
            public Vector3 velocity;
            public float yRotation;
            public float xRotation;

            public int tick;

            public void SetValues(Vector3 position, Vector3 velocity, float yRotation, float xRotation, int tick)
            {
                this.position = position;
                this.velocity = velocity;

                this.yRotation = yRotation;
                this.xRotation = xRotation;

                this.tick = tick;
            }

            public void SetValues(Vector3 position, Vector3 velocity, InputMessage inputMessage)
            {
                this.position = position;
                this.velocity = velocity;

                yRotation = inputMessage.yRotation;
                xRotation = inputMessage.xRotation;
                tick = inputMessage.tick;
            }

            /*public Quaternion CalculateLeftRightRotation() => Quaternion.AngleAxis(yRotation, Vector3.up);
            public Quaternion CalculateUpDownRotation() => Quaternion.AngleAxis(xRotation, Vector3.right);*/
        }
    }
}