using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SticklersGate.UI
{
    /// <summary>
    /// Floating text popup for damage, healing, and status messages.
    /// Self-destructs after animation completes.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text _text;

        [Header("Animation")]
        [SerializeField] private float _floatSpeed = 50f;
        [SerializeField] private float _duration = 1.2f;
        [SerializeField] private float _fadeStartTime = 0.6f;

        private float _elapsed;
        private Color _startColor;

        /// <summary>
        /// Initialize the popup with text, color, and position.
        /// </summary>
        public void Setup(string message, Color color, Vector3 worldPosition, Camera cam)
        {
            if (_text != null)
            {
                _text.text = message;
                _text.color = color;
                _startColor = color;
            }

            // Convert world position to screen position
            if (cam != null)
            {
                var screenPos = cam.WorldToScreenPoint(worldPosition);
                transform.position = screenPos;
            }

            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            while (_elapsed < _duration)
            {
                _elapsed += Time.deltaTime;

                // Float upward
                transform.position += Vector3.up * _floatSpeed * Time.deltaTime;

                // Fade out after fadeStartTime
                if (_elapsed > _fadeStartTime && _text != null)
                {
                    float fadeProgress = (_elapsed - _fadeStartTime) / (_duration - _fadeStartTime);
                    var c = _startColor;
                    c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                    _text.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
