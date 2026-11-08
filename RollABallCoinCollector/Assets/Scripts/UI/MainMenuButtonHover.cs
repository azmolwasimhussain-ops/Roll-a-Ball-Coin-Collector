using UnityEngine;
using UnityEngine.EventSystems;

namespace RollABallCoinCollector
{
    /// <summary>Adds a restrained scale response to the main menu Play button.</summary>
    public sealed class MainMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Range(1f, 1.12f)] private float hoverScale = 1.035f;
        [SerializeField, Min(1f)] private float returnSpeed = 14f;

        private RectTransform buttonRect;
        private Vector3 baseScale;
        private bool isHovered;

        private void Awake()
        {
            buttonRect = GetComponent<RectTransform>();
            if (buttonRect != null)
                baseScale = buttonRect.localScale;
        }

        private void Update()
        {
            if (buttonRect == null)
                return;

            float scale = isHovered ? hoverScale : 1f;
            Vector3 targetScale = baseScale * scale;
            buttonRect.localScale = Vector3.Lerp(
                buttonRect.localScale,
                targetScale,
                1f - Mathf.Exp(-returnSpeed * Time.unscaledDeltaTime));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }

        private void OnDisable()
        {
            isHovered = false;
            if (buttonRect != null)
                buttonRect.localScale = baseScale;
        }
    }
}
