using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class FillBar 
    {
        private Image fill;
        private float min, max;
        
        public void Initialize(string name, Transform transform, float min, float max)
        {
            this.min = min;
            this.max = max;
            Image[] images = transform.GetComponentsInChildren<Image>();
            fill = images.ToList().Where(x => x.name == name).FirstOrDefault();
            if (fill == null)
                Debug.LogError($"Image with the name of '{name}' not found in {transform.name}!");
        }

        public void Set(float value)
        {
            value = Mathf.Clamp(value, min, max);
            fill.fillAmount = value / max;
        }
    }
}
