using UnityEngine;

namespace Mastic
{
    public class Lerp : MonoBehaviour
    {
        [SerializeField] private Vector3 aPos = default;
        [SerializeField] private Vector3 bPos = default;
        [SerializeField] private Quaternion aRot = default;
        [SerializeField] private Quaternion bRot = default;
        [SerializeField] private Vector3 aScale = default;
        [SerializeField] private Vector3 bScale = default;
        [SerializeField] private float lerpSpeed = default;
        [SerializeField] private bool sync = default;
        private float lerpValue;
        private bool endLerped;

        private void Update()
        {
            lerpValue += (endLerped ? 1f : -1f) * Time.deltaTime * lerpSpeed;
            transform.localPosition = Vector3.Lerp(aPos, bPos, lerpValue);
            transform.localRotation = Quaternion.Lerp(aRot, bRot, lerpValue);
            transform.localScale = Vector3.Lerp(aScale, bScale, lerpValue);
        }

        public void SetAs(bool endLerped)
        {
            this.endLerped = endLerped;
            lerpValue = (endLerped ? 0f : 1f);
        }

        private void OnValidate()
        {
            if (!sync)
                return;

            aPos = transform.localPosition;
            bPos = aPos;
            aRot = transform.localRotation;
            bRot = aRot;
            aScale = transform.localScale;
            bScale = aScale;
            sync = false;
        }
    }
}
