using System.Collections;
using UnityEngine;

namespace RollABallCoinCollector
{
    /// <summary>Fades the main menu overlay in whenever it becomes active.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class MainMenuFadeIn : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0.05f)] private float duration = 0.45f;

        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                return;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeIn());
        }

        private void OnDisable()
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = null;
        }

        private IEnumerator FadeIn()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            float fadeDuration = Mathf.Max(0.05f, duration);
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            fadeRoutine = null;
        }
    }
}
