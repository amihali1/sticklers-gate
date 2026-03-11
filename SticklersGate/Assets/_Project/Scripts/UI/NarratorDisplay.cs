using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SticklersGate.UI
{
    /// <summary>
    /// Top-of-screen narrator text overlay with typewriter animation.
    /// Queues multiple messages and displays them in sequence.
    /// </summary>
    public class NarratorDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Text _narratorText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Settings")]
        [SerializeField] private float _typewriterSpeed = 0.03f;
        [SerializeField] private float _displayDuration = 2.5f;
        [SerializeField] private float _fadeDuration = 0.5f;

        private readonly Queue<string> _messageQueue = new Queue<string>();
        private bool _isDisplaying;

        /// <summary>
        /// Queue a narrator message for display.
        /// </summary>
        public void ShowMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _messageQueue.Enqueue(message);
            if (!_isDisplaying)
            {
                StartCoroutine(DisplayLoop());
            }
        }

        private IEnumerator DisplayLoop()
        {
            _isDisplaying = true;

            while (_messageQueue.Count > 0)
            {
                string message = _messageQueue.Dequeue();

                // Fade in
                _narratorText.text = "";
                yield return FadeCanvasGroup(0f, 1f, _fadeDuration * 0.5f);

                // Typewriter effect
                for (int i = 0; i <= message.Length; i++)
                {
                    _narratorText.text = message.Substring(0, i);
                    yield return new WaitForSeconds(_typewriterSpeed);
                }

                // Hold
                yield return new WaitForSeconds(_displayDuration);

                // Fade out
                yield return FadeCanvasGroup(1f, 0f, _fadeDuration);
            }

            _isDisplaying = false;
        }

        private IEnumerator FadeCanvasGroup(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }
    }
}
