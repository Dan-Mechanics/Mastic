using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mastic
{
    public class NetworkMovement : NetworkBehaviour
    {
        /// <summary>
        /// Server view, latest processed client movement input tick.
        /// </summary>
        public int ProcessedTick { get; private set; }

        /// <summary>
        /// Counter for the local player, ID for movement ticks.
        /// </summary>
        public int InputTick { get; private set; }
        
        /// <summary>
        /// Unique counter for this player on the server.
        /// </summary>
        public int ServerTick { get; private set; }

        public event Action OnPlayReconsileSound;
        public event Action<int> OnDisplayTick;
        public event Action<bool> OnDisplayReconsile;
        public event Action<StateMessage> OnDisplayServerState;

        [SerializeField] private EasyBinding forward = default;
        [SerializeField] private EasyBinding left = default;
        [SerializeField] private EasyBinding backward = default;
        [SerializeField] private EasyBinding right = default;
        [SerializeField, Min(1)] private int bufferSize = default;
        [SerializeField, Min(1)] private int maxPendingInputMessages = default;

        private Rigidbody rb;
        private Transform eyes;
        private MouseLook mouseLook;
        private AdaptiveTickrate adaptiveTickrate;
        private ICameraInterpolation cameraInterpolation;

        private byte movementIndex;
        private IMovement movement;
        private IMovement[] movements;
        private List<IMovementAbility> movementAbilities;
        private List<InputMessage> pendingInputMessages;

        private StateMessage[] stateBuffer;
        private InputMessage[] inputBuffer;
        private StateMessage serverStateMessage;
        private InputMessage previousInputMessage;

        private Vector3 prevEyePos;
        private float tolerance;
        private bool firstInputMessageReceived; 
        private bool hasInputMessages;
        private int stateBufferIndex;
        private int receivedTick;
        private float standardInterval;
        private float topSpeed;
        private bool w, a, s, d;

        public void Initialize(int standardTickrate, ICameraInterpolation cameraInterpolation)
        {
            this.cameraInterpolation = cameraInterpolation;
            movementAbilities = GetComponents<IMovementAbility>().ToList();
            standardInterval = 1f / standardTickrate;

            movements = GetComponents<IMovement>();
            for (int i = 0; i < movements.Length; i++)
            {
                movements[i].Index = (byte)i;
            }

            SetMovement(default);
            rb = GetComponent<Rigidbody>();
            mouseLook = GetComponent<MouseLook>();
            eyes = transform.Find("eyes");
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();

            EasySettings easySettings = EasySettings.Current;
            tolerance = easySettings.Get<float>(nameof(tolerance));
            topSpeed = easySettings.Get<float>(nameof(topSpeed));

            prevEyePos = eyes.position;
            receivedTick = -1;
            previousInputMessage.tick = -1;
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            inputBuffer = new InputMessage[bufferSize];
            stateBuffer = new StateMessage[bufferSize];
            for (int i = 0; i < stateBuffer.Length; i++)
            {
                stateBuffer[i].position = transform.position;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            // inputBuffer = new InputMessage[bufferSize];
            // YOU CAN REMOVE THIS FOR MORE SERVER RAM SPACE.
            stateBuffer = new StateMessage[bufferSize];
            pendingInputMessages = new List<InputMessage>();
        }

        [Client]
        public void DoLocalTick(int rollbackTick)
        {
            movementAbilities.ForEach(x => x.DoLocalTick(InputTick, movement));

            // MAKE IT SO THAT IF YOU ALT+TAB YOU KEEP MOVING.
            if (Application.isFocused) 
            {
                w = forward.IsHeld;
                a = left.IsHeld;
                s = backward.IsHeld;
                d = right.IsHeld;
            }

            int index = InputTick % bufferSize;
            inputBuffer[index].SetValues(w, a, s, d, mouseLook.RotationX, mouseLook.RotationY,
                cameraInterpolation.LerpValue, rollbackTick, InputTick);
            Move(inputBuffer[index], true);

            stateBuffer[index].SetValues(transform.position, rb.linearVelocity, movementIndex, inputBuffer[index]);
            CmdSendInputMessageToServer(inputBuffer[index]);

            OnDisplayTick?.Invoke(InputTick);
            InputTick++;
        }

        [Server]
        public void DoServerTick()
        {
            adaptiveTickrate.ApplyTimeDilation(firstInputMessageReceived, pendingInputMessages.Count);

            InputMessage inputMessage = GetNextInputMessage();
            Move(inputMessage, false);

            stateBufferIndex = inputMessage.tick % bufferSize;
            stateBuffer[stateBufferIndex].SetValues(transform.position, rb.linearVelocity, movementIndex, inputMessage);

            if (previousInputMessage.tick != inputMessage.tick - 1)
            {
                Debug.LogWarning($"We have skipped a tick on the server ...");
                Debug.LogWarning($"if (previousInputMessage.tick != inputMessage.tick - 1) || if ({previousInputMessage.tick} != {inputMessage.tick - 1})");
                Debug.LogWarning("This is acceptable for spawn because the buffer is very empty");
            }

            ProcessedTick = inputMessage.tick;
            previousInputMessage = inputMessage;
        }

        public InputMessage GetPreviousInputMessage() => previousInputMessage;

        private InputMessage GetNextInputMessage()
        {
            hasInputMessages = false;
            InputMessage inputMessage;
            if (pendingInputMessages.Count > 0)
            {
                inputMessage = pendingInputMessages[0];
                pendingInputMessages.RemoveAt(0);
                if (inputMessage.tick >= 0)
                {
                    firstInputMessageReceived = true;
                    hasInputMessages = true;
                }
                else
                {
                    inputMessage = GetRepeatInputMessage();
                }
            }
            else
            {
                inputMessage = GetRepeatInputMessage();
            }

            return inputMessage;
        }

        private InputMessage GetRepeatInputMessage()
        {
            InputMessage inputMessage = previousInputMessage;
            inputMessage.rollbackTick++;
            inputMessage.tick++;
            return inputMessage;
        }

        public void SetMovement(byte index)
        {
            movement = movements[index];
            movementIndex = index;
        }

        /// <summary>
        /// This needs to be here because of the difference
        /// in ordering of Physics.Simulate between the server and client.
        /// </summary>
        [Server]
        public void SendStateMessageToClient() 
        {
            LimitSpeed();
            stateBuffer[stateBufferIndex].position = transform.position;
            stateBuffer[stateBufferIndex].velocity = rb.linearVelocity;

            TargetSendStateMessageToClient(connectionToClient, stateBuffer[stateBufferIndex]);
        }

        [Client]
        public void DebugAlterInputTick(int offset)
        {
            InputTick += offset;
            Debug.LogWarning($"{nameof(InputTick)} changed by {offset}.");
        }

        /// <summary>
        /// This is because afte the simulation step, the velocity is unstable. 
        /// We limit it to make sure it doesn't cause reconsiles.
        /// </summary>
        public void LimitSpeed() => rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, topSpeed);

        [Command(channel = Channels.Unreliable)]
        private void CmdSendInputMessageToServer(InputMessage inputMessage)
        {
            // ALLOW DEFAULTED TICKS TO BE CORRECTED.
            for (int i = 0; i < pendingInputMessages.Count; i++)
            {
                if (pendingInputMessages[i].tick == inputMessage.tick)
                    pendingInputMessages[i] = inputMessage;
            }

            // VALIDATE INCOMING MESSAGES.
            if (inputMessage.tick < 0 || inputMessage.tick <= receivedTick)
                return;

            // FILL GAPS BETWEEN PACKETS, BECAUSE OF PACKET LOSS.
            if (inputMessage.tick > receivedTick + 1 && firstInputMessageReceived && hasInputMessages)
            {
                int packetsAdded = 0;
                int packetsMissing = inputMessage.tick - receivedTick - 1;
                for (int i = packetsMissing - 1; i >= 0; i--)
                {
                    InputMessage clone = inputMessage;
                    clone.tick -= i + 1;

                    if (clone.tick <= previousInputMessage.tick)
                        continue;

                    pendingInputMessages.Add(clone);
                    packetsAdded++;
                    if (packetsAdded >= maxPendingInputMessages)
                        break;
                }
            }

            pendingInputMessages.Add(inputMessage);
            while (pendingInputMessages.Count > maxPendingInputMessages)
            {
                pendingInputMessages.RemoveAt(pendingInputMessages.Count - 1);
            }

            receivedTick = pendingInputMessages[^1].tick;
        }

        [TargetRpc(channel = Channels.Unreliable)]
        private void TargetSendStateMessageToClient(NetworkConnectionToClient conn, StateMessage stateMessage)
        {
            // MAKE SURE MESSAGES ARE NOT OUT OF ORDER.
            if (stateMessage.tick > InputTick - 1)
            {
                Debug.LogWarning("We have to return here since the positions are stored in a ringbuffer and otherwise would wrap around and completely break the reconsile.");
                return;
            }

            if (stateMessage.tick <= serverStateMessage.tick)
            {
                Debug.LogWarning("We have to return here since we already reconsiled on this tick and we cant do it twice, yes this means we have the possibility of missing reconsiles but that's acceptable since we get the next message next.");
                return;
            }

            serverStateMessage = stateMessage;
            movementAbilities.ForEach(x => x.CleanPendingRequests(serverStateMessage.tick));
            OnDisplayServerState?.Invoke(serverStateMessage);
            CheckReconsiliation();
        }

        [Client]
        private void CheckReconsiliation()
        {
            int serverStateBufferIndex = serverStateMessage.tick % bufferSize;
            float distance = Vector3.Distance(serverStateMessage.position, stateBuffer[serverStateBufferIndex].position);
            if (distance <= tolerance)
            {
                OnDisplayReconsile?.Invoke(false);
                return;
            }

            Debug.LogWarning($"We have to reconcile for {serverStateMessage.tick} | if ({serverStateMessage.position} != {stateBuffer[serverStateBufferIndex].position}).");
            Debug.LogWarning($"Distance: {distance}, in actual: {distance / Time.fixedDeltaTime}.");
            // if (Application.isFocused)
            OnPlayReconsileSound?.Invoke();
            OnDisplayReconsile?.Invoke(true);

            DoReconsile(serverStateBufferIndex);
        }

        [Client]
        private void DoReconsile(int stateBufferIndex)
        {
            // TELEPORT.
            transform.position = serverStateMessage.position;
            rb.linearVelocity = serverStateMessage.velocity;
            SetMovement(serverStateMessage.movementIndex);

            stateBuffer[stateBufferIndex] = serverStateMessage;

            int tickToProcess = serverStateMessage.tick + 1;
            while (tickToProcess < InputTick)
            {
                int index = tickToProcess % bufferSize;

                Vector3 prev = eyes.position;
                Move(inputBuffer[index], false);

                cameraInterpolation.Interject(eyes.position, prev, rb.linearVelocity);
                stateBuffer[index].SetValues(transform.position, rb.linearVelocity, movementIndex, inputBuffer[index]);

                tickToProcess++;
            }
        }

        [Server]
        public void DoServerMovementAbilities()
        {
            // RECREATE CAM POSITION IN THIS MOMENT.
            mouseLook.SetRotationDirectly(previousInputMessage.xRotation, previousInputMessage.yRotation);
            cameraInterpolation.Interject(eyes.position, prevEyePos, rb.linearVelocity);
            cameraInterpolation.SetValue(previousInputMessage.lerpValue);
            movementAbilities.ForEach(x => x.CheckAgainstTickServer(previousInputMessage.tick, movement, ServerTick));
            prevEyePos = eyes.position;

            movementAbilities.ForEach(x => x.CleanPendingRequests(previousInputMessage.tick));
            ServerTick++;
        }

        public void AddForce(Vector3 velocityChange) => movement?.AddForce(velocityChange);

        private void Move(InputMessage input, bool applyToInterpolation)
        {
            // RECREATE THE MOVEMENT OF THE PLAYER IN THIS MOMENT.
            mouseLook.SetRotationTemporarily(input.xRotation, input.yRotation);
            movement.Move(input.GetVerticalInput(), input.GetHorizontalInput(), standardInterval);
            if (isLocalPlayer)
            {
                movementAbilities.ForEach(x => x.CheckAgainstTickClient(input.tick, movement));
                EventManager<int>.RaiseEvent(Occasion.DoUnlocalMovementAbilities, input.tick);

                // APPLY CHANGES.
                Physics.Simulate(standardInterval);
                LimitSpeed();
            }
            // THE SERVER MOVEMENT ABILITIES ARE BATCHED FOR ALL PLAYERS.
            // BECAUSE OTHERWISE YOU GET OUT-OF-ORDER BUGS.

            if (applyToInterpolation)
                cameraInterpolation.Apply(eyes.position, rb.linearVelocity);
        }
    }
}