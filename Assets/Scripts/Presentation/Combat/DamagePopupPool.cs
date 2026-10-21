using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DwarfsCrypt.Presentation.Combat
{
    /// <summary>
    /// Scene-scoped object pool for <see cref="DamagePopup"/>. Building a TextMeshPro at runtime and
    /// destroying it every hit is expensive and churns the GC; the pool reuses a small set of popups
    /// instead. Auto-created on first use (mirrors <see cref="Boosters.BuffTracker"/>).
    /// </summary>
    public class DamagePopupPool : MonoBehaviour
    {
        private static DamagePopupPool _instance;

        public static DamagePopupPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DamagePopupPool>();
                    if (_instance == null)
                        _instance = new GameObject(nameof(DamagePopupPool)).AddComponent<DamagePopupPool>();
                }
                return _instance;
            }
        }

        private readonly Queue<DamagePopup> _idle = new();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>Fetches a popup from the pool (or creates one), positions it, and plays it.</summary>
        public void Spawn(
            Vector3 worldPos, string label, Color color, float fontSize,
            float lifetime, float riseSpeed, float horizontalDrift,
            string sortingLayerName, int sortingOrder, TMP_FontAsset font)
        {
            DamagePopup popup = Get();
            popup.transform.position = worldPos;
            popup.gameObject.SetActive(true);
            popup.Play(label, color, fontSize, lifetime, riseSpeed, horizontalDrift,
                       sortingLayerName, sortingOrder, font);
        }

        private DamagePopup Get()
        {
            // Skip any entries destroyed with a previous scene.
            while (_idle.Count > 0)
            {
                DamagePopup pooled = _idle.Dequeue();
                if (pooled != null) return pooled;
            }
            return Create();
        }

        private DamagePopup Create()
        {
            var go = new GameObject("DamagePopup");
            go.transform.SetParent(transform, false);
            go.AddComponent<TextMeshPro>();
            var popup = go.AddComponent<DamagePopup>();
            popup.Finished += Release;
            go.SetActive(false);
            return popup;
        }

        private void Release(DamagePopup popup)
        {
            if (popup == null) return;
            popup.gameObject.SetActive(false);
            _idle.Enqueue(popup);
        }
    }
}
