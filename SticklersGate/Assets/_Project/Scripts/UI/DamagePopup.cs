using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SticklersGate.UI
{
    /// <summary>
    /// Floating text popup for damage, healing, and status messages.
    /// Self-destructs after animation completes.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text _text;

        [Header("Animation")]
        [SerializeField] private float _floatSpeed = 50f;
        [SerializeField] private float _duration = 1.2f;
        [SerializeField] private float _fadeStartTime = 0.6f;

        private float _elapsed;
        private Color _startColor;
        private float _unitsPerPixel = 1f; // screen-space overlay: 1; camera-space canvas: world units per pixel

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

            // Convert world position to a position on the canvas
            if (cam != null)
            {
                var screenPos = cam.WorldToScreenPoint(worldPosition);
                var canvas = GetComponentInParent<Canvas>();

                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay &&
                    RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        (RectTransform)canvas.transform, screenPos, canvas.worldCamera, out var canvasPoint))
                {
                    // Camera-space canvas (e.g. autoplay rendering to a texture)
                    transform.position = canvasPoint;
                    _unitsPerPixel = canvas.transform.lossyScale.y;
                }
                else
                {
                    transform.position = screenPos;
                }
            }

            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            while (_elapsed < _duration)
            {
                _elapsed += Time.deltaTime;

                // Float upward
                transform.position += Vector3.up * _floatSpeed * _unitsPerPixel * Time.deltaTime;

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
