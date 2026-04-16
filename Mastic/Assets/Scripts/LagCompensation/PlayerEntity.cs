using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// https://docs.google.com/document/d/1ruX-Pqfwd8WIK8eIo0dRAs86pEsc5Z398IOfDLNskYc/edit?tab=t.0#bookmark=id.3wz3pq4fu5jg
    /// </summary>
    public class PlayerEntity : NetworkBehaviour, IEntity
    {
        [SerializeField] private CharacterController controller = default;
        [SerializeField] private GameObject hitbox = default;

        private readonly List<PlayerFrame> recording = new List<PlayerFrame>();
        private PlayerFrame present;
        private bool hasSaved;

        public override void OnStartServer()
        {
            base.OnStartServer();
            LagCompensation.instance.Register(this);
        }

        private void SetAsFrame(PlayerFrame frame) => transform.position = frame.pos;
        public int GetNetId() => (int)netId;

        [Server]
        public void RecordFrame(int tick, int maxRecordingLength)
        {
            recording.Add(new PlayerFrame(transform.position, tick));
            while (recording.Count > maxRecordingLength)
            {
                recording.RemoveAt(0);
            }
        }

        [Server]
        public void SavePresent()
        {
            present.pos = transform.position;
            hasSaved = true;
        }

        [Server]
        public void RewindTime(int tick)
        {
            for (int i = 0; i < recording.Count; i++)
            {
                if (recording[i].tick != tick) 
                    continue;

                controller.enabled = false;
                SetAsFrame(recording[i]);
                return;
            }

            if (recording.Count <= 0 || tick < recording[0].tick)
                hitbox.SetActive(false);

            // IF IT IS IN THE FUTURE, WE WANT THE MOST RECENT
            // FRAME POSSIBLE, SO WE DO NOTHING.
        }

        [Server]
        public void ReturnToPresent()
        {
            if (!hasSaved)
                return;

            SetAsFrame(present);
            controller.enabled = true;
            hitbox.SetActive(true);
            hasSaved = false;
        }

    }
}