using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Mastic
{
    public class HitMarker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform[] arms = null;
        [SerializeField] private AudioSource source = null;
        [SerializeField] private AudioClip clip = null;

        [Header("Settings")]
        [SerializeField] private float sizeDecreasePerSecond = 0f;
        [SerializeField] private float damageToSizeConversion = 0f;
        [SerializeField] private float sizeToZeroThreshold = 0f;

        [Tooltip("Does not update in runtime.")]
        [SerializeField] private float width = 0f;

        [SerializeField] private bool playSound = false;

        private float size;
        private Vector3 scale;

        private void Start()
        {
            scale = new Vector3(width, size, 1f);
        }

        private void Update()
        {
            size -= sizeDecreasePerSecond * size * Time.deltaTime;
            //size -= sizeDecreasePerSecond * Time.deltaTime;

            if (size < sizeToZeroThreshold) { size = 0f; }

            scale.y = size;

            for (int i = 0; i < arms.Length; i++)
            {
                arms[i].localScale = scale;
            }
        }

        public void Damage(float amount) 
        {
            //if (amount <= 0f) { return; }

            size += amount * damageToSizeConversion;

            if (playSound) { source.PlayOneShot(clip); }
        }
    }
}