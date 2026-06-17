using UnityEngine;

namespace Mastic
{
    public class UniqueToElement : MonoBehaviour
    {
        [SerializeField] private string element = default;
        [SerializeField] private GameObject fpsArmsPrefab = default;
        private GameObject fpsArms;

        public void InitializeLocalPlayer()
        {
            Transform cam = GameObject.FindWithTag("MainCamera").transform;
            fpsArms = Instantiate(fpsArmsPrefab);
            fpsArms.transform.SetParent(cam);
            fpsArms.transform.localPosition = fpsArmsPrefab.transform.position;
            fpsArms.transform.localRotation = fpsArmsPrefab.transform.rotation;
            Material mat = Resources.Load<Material>($"Materials/{element}");
            if (mat != null)
                fpsArms.GetComponentInChildren<SkinnedMeshRenderer>().material = mat;
        }

        private void OnDestroy()
            => Destroy(fpsArms);
    }
}
