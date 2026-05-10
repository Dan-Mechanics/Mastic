using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mastic
{
    public class NetworkMovement : NetworkBehaviour
    {
        public int MovementTick => currentTick;

        public event Action OnPlayReconsileSound;
        public event Action<int> OnDisplayTick;
        public event Action<string> OnDisplayCheats;
        public event Action<bool> OnDisplayReconsile;
        public event Action<StateMessage> OnDisplayServerState;

        [SerializeField] private EasyBinding forward = default;
        [SerializeField] private EasyBinding left = default;
        [SerializeField] private EasyBinding backward = default;
        [SerializeField] private EasyBinding right = default;
        [SerializeField] private MovementSettings settings = default;
        [SerializeField] private int bufferSize = default;
        [SerializeField] private float tolerance = default;
        [SerializeField] private int maxPendingInputMessages = default;
        [SerializeField] private byte standardMovementIndex = default;

        private Rigidbody rb;
        private Transform eyes;
        private MouseLook mouseLook;
        private AdaptiveTickrate adaptiveTickrate;
        private ICameraInterpolation interpolation;
        private IMovement movement;
        private byte movementIndex;
        private List<IMovementAbility> movementAbilities;
        private IMovement[] movements;
        private List<InputMessage> pendingInputMessages;
        private StateMessage[] stateBuffer;
        private InputMessage[] inputBuffer;
        private StateMessage serverStateMessage;
        private InputMessage previousInputMessage;

        private bool hasReceivedFirstMessage; 
        private bool bufferHasTicks; 
        private int currentTick;
        private int receivedTick;
        private float standardInterval;
        private bool w, a, s, d;
        private float timer;

        public void Initialize(int standardTickrate, ICameraInterpolation interpolation)
        {
            standardInterval = 1f / standardTickrate;
            this.interpolation = interpolation;

            movements = GetComponents<IMovement>();
            for (int i = 0; i < movements.Length; i++)
            {
                movements[i].Index = (byte)i;
            }

            movementAbilities = GetComponents<IMovementAbility>().ToList();
            SetMovement(standardMovementIndex);
            rb = GetComponent<Rigidbody>();
            mouseLook = GetComponent<MouseLook>();
            eyes = transform.Find("eyes");
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();

            // IN THEORY YOU COULD OMIT SOME OF THESE
            // DEPENDING ON IF LOCAL OR SERVER ETC.
            pendingInputMessages = new List<InputMessage>();
            stateBuffer = new StateMessage[bufferSize];
            inputBuffer = new InputMessage[bufferSize];
            receivedTick = -1;
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            previousInputMessage.tick = -1; 
            for (int i = 0; i < stateBuffer.Length; i++)
            {
                stateBuffer[i].position = transform.position;
            }
        }

        [Client]
        public int DoLocalUpdate()
        {
            int ticks = 0;
            int clientPacketMultiplier = 1;
            if (Input.GetKey(KeyCode.Mouse4)) { clientPacketMultiplier = 2; }
            else if (Input.GetKey(KeyCode.Mouse2)) { clientPacketMultiplier = 0; }

            timer += Time.deltaTime;
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;

                OnDisplayCheats?.Invoke($"cheats: {clientPacketMultiplier}");
                for (int i = 0; i < clientPacketMultiplier; i++)
                {
                    movementAbilities.ForEach(x => x.DoLocalTick(currentTick, movement));
                    DoLocalTick();
                    ticks++;
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow)) { currentTick += 10; Debug.LogWarning("+10"); }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { currentTick -= 10; Debug.LogWarning("-10"); }

            return ticks;
        }

        public void SetMovement(byte index)
        {
            movement = movements[index];
            movementIndex = index;
        }

        [Client]
        private void DoLocalTick()
        {
            // MAKE IT SO THAT IF YOU ALT+TAB YOU KEEP MOVING.
            if (Application.isFocused) 
            {
                w = forward.IsHeld;
                a = left.IsHeld;
                s = backward.IsHeld;
                d = right.IsHeld;
            }

            int index = currentTick % bufferSize;
            inputBuffer[index].SetValues(w, a, s, d, mouseLook.RotationX, mouseLook.RotationY, currentTick);
            Move(inputBuffer[index], true);

            stateBuffer[index].SetValues(transform.position, rb.linearVelocity, movementIndex, inputBuffer[index]);
            CmdSendInputMessageToServer(inputBuffer[index]);

            OnDisplayTick?.Invoke(currentTick);
            currentTick++;
        }

        [Server]
        public int DoServerTick()
        {
            adaptiveTickrate.ApplyTimeDilation(hasReceivedFirstMessage, pendingInputMessages.Count);
            bufferHasTicks = false;

            InputMessage inputMessage = GetNextInputMessage();
            int stateBufferIndex = inputMessage.tick % bufferSize;
            Move(inputMessage, false);
            stateBuffer[stateBufferIndex].SetValues(transform.position, rb.linearVelocity, movementIndex, inputMessage);

            movementAbilities.ForEach(x => x.CleanTicks(inputMessage.tick));

            if (previousInputMessage.tick != inputMessage.tick - 1)
            {
                Debug.LogWarning($"We have skipped a tick on the server ...");
                Debug.LogWarning($"if (previousInputMessage.tick != inputMessage.tick - 1) || if ({previousInputMessage.tick} != {inputMessage.tick - 1})");
                Debug.LogWarning("This is acceptable for spawn because the buffer is very empty");
            }

            previousInputMessage = inputMessage;
            currentTick++;
            return stateBufferIndex;
        }

        private InputMessage GetNextInputMessage()
        {
            InputMessage inputMessage;
            if (pendingInputMessages.Count > 0)
            {
                inputMessage = pendingInputMessages[0];
                pendingInputMessages.RemoveAt(0);
                if (inputMessage.tick >= 0)
                {
                    hasReceivedFirstMessage = true;
                    bufferHasTicks = true;
                }
                else
                {
                    inputMessage = GetDefaultInputMessage();
                }
            }
            else
            {
                inputMessage = GetDefaultInputMessage();
            }

            return inputMessage;
        }

        private InputMessage GetDefaultInputMessage()
        {
            InputMessage inputMessage = previousInputMessage;
            inputMessage.tick++;
            return inputMessage;
        }

        /// <summary>
        /// This needs to be here because of the difference
        /// in ordering of Physics.Simulate between the server and client.
        /// </summary>
        [Server]
        public void SendStateMessageToClient(int stateBufferIndex) 
        {
            stateBuffer[stateBufferIndex].position = transform.position;
            stateBuffer[stateBufferIndex].velocity = rb.linearVelocity;
            TargetSendStateMessageToClient(connectionToClient, stateBuffer[stateBufferIndex]);
        }

        /// <summary>
        /// This is because afte the simulation step, the velocity is unstable. 
        /// We limit it to make sure it doesn't cause reconsiles.
        /// </summary>
        public void LimitSpeed() => rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, settings.topSpeed);
        public void AddForce(Vector3 velocityChange) => movement.AddForce(velocityChange);

        /// <summary>
        /// Consider making it so that the InputMessage is inserted 
        /// where it is according to the order. Alternitively, you 
        /// could also sort the pending on DoServerTick() , and fill in the gaps ??
        /// </summary>
        [Command(channel = Channels.Unreliable)]
        private void CmdSendInputMessageToServer(InputMessage inputMessage)
        {
            // MAKE SURE MESSAGES ARE NOT OUT OF ORDER OR INCORRECT.
            if (inputMessage.tick < 0 || inputMessage.tick <= receivedTick)
                return;

            if (inputMessage.tick > receivedTick + 1 && hasReceivedFirstMessage && bufferHasTicks)
            {
                for (int i = 0; i < inputMessage.tick - receivedTick - 1; i++)
                {
                    InputMessage clone = inputMessage;
                    clone.tick -= i + 1;

                    // IF WE ALREADY DEFAULTED THIS, THEN THERE'S NO POINT.
                    if (clone.tick > previousInputMessage.tick)
                        pendingInputMessages.Add(clone);
                }
            }

            receivedTick = inputMessage.tick;
            pendingInputMessages.Add(inputMessage);

            // REMOVE IF TOO MANY.
            while (pendingInputMessages.Count > maxPendingInputMessages)
            {
                pendingInputMessages.RemoveAt(pendingInputMessages.Count - 1);
            }
        }

        [TargetRpc(channel = Channels.Unreliable)]
        private void TargetSendStateMessageToClient(NetworkConnectionToClient conn, StateMessage stateMessage)
        {
            // MAKE SURE MESSAGES ARE NOT OUT OF ORDER.
            if (stateMessage.tick > currentTick - 1)
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
            movementAbilities.ForEach(x => x.CleanTicks(serverStateMessage.tick));
            OnDisplayServerState?.Invoke(serverStateMessage);
            CheckReconsiliation();
        }

        [Client]
        private void CheckReconsiliation()
        {
            int serverStateBufferIndex = serverStateMessage.tick % bufferSize;
            float distance = Vector3.Distance(serverStateMessage.position, stateBuffer[serverStateBufferIndex].position);
            bool reconsile = distance > tolerance;
            if (reconsile)
            {
                Debug.LogWarning($"We have to reconcile for {serverStateMessage.tick} | if ({serverStateMessage.position} != {stateBuffer[serverStateBufferIndex].position}).");
                Debug.LogWarning($"Distance: {distance}, in actual: {distance / Time.fixedDeltaTime}.");
                if (Application.isFocused)
                    OnPlayReconsileSound?.Invoke();

                DoReconsile(serverStateBufferIndex);
            }

            OnDisplayReconsile?.Invoke(reconsile);
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
            while (tickToProcess < currentTick)
            {
                int index = tickToProcess % bufferSize;

                Vector3 prev = eyes.position;
                Move(inputBuffer[index], false);

                interpolation.Interject(eyes.position, prev, rb.linearVelocity);
                stateBuffer[index].SetValues(transform.position, rb.linearVelocity, movementIndex, inputBuffer[index]);

                tickToProcess++;
            }
        }

        private void Move(InputMessage input, bool assignToCamera)
        {
            // RECREATE THE MOVEMENT OF THE PLAYER IN THIS MOMENT.
            mouseLook.SetAsRotation(input.xRotation, input.yRotation);
            movement.Move(input.GetVerticalInput(), input.GetHorizontalInput(), standardInterval);

            if (isServer)
            {
                movementAbilities.ForEach(x => x.CheckAgainstTickServer(input.tick, movement, currentTick));
            }
            else
            {
                movementAbilities.ForEach(x => x.CheckAgainstTickClient(input.tick, movement));
            }

            // APPLY CHANGES.
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