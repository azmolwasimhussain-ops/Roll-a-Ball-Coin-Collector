using System;
using System.Collections.Generic;
using TMPro;
using RollABallCoinCollector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollABallCoinCollector.EditorTools
{
    /// <summary>Repairs the existing main menu in place without replacing gameplay UI or button references.</summary>
    [InitializeOnLoad]
    public static class MainMenuLayoutFix
    {
        private const string ScenePath = "Assets/Scenes/RollABallMain.unity";
        private const string PendingFixKey = "RollABallCoinCollector.PendingMainMenuLayoutFix";

        private static readonly Vector2[] ValidationResolutions =
        {
            new Vector2(1920f, 1080f),
            new Vector2(1366f, 768f),
            new Vector2(1280f, 720f),
            new Vector2(1366f, 1024f),
            new Vector2(1024f, 768f),
            new Vector2(768f, 1024f)
        };

        static MainMenuLayoutFix()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Roll-a-Ball Coin Collector/Fix Main Menu Layout")]
        public static void FixCurrentMainMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorPrefs.SetBool(PendingFixKey, true);
                EditorApplication.isPlaying = false;
                Debug.Log("[Roll-a-Ball] Play Mode is ending; the main menu layout fix will run in Edit Mode.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException($"The playable scene was not found at {ScenePath}.");

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            ApplyToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Roll-a-Ball] Main menu layout repaired and saved.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !EditorPrefs.GetBool(PendingFixKey, false))
                return;

            EditorPrefs.DeleteKey(PendingFixKey);
            EditorApplication.delayCall += FixCurrentMainMenu;
        }

        /// <summary>Shared implementation used by the in-place Editor command and scene generator.</summary>
        public static void ApplyToScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ArgumentException("The target scene must be loaded before the menu can be repaired.", nameof(scene));

            Transform canvasTransform = FindTransform(scene, "Game HUD");
            Transform panelTransform = FindTransform(scene, "Main Menu Panel");
            Transform cardTransform = FindTransform(scene, "Main Menu Card");
            if (canvasTransform == null || panelTransform == null || cardTransform == null)
                throw new InvalidOperationException("The existing Game HUD, Main Menu Panel, or Main Menu Card is missing.");

            Canvas canvas = canvasTransform.GetComponent<Canvas>();
            CanvasScaler scaler = canvasTransform.GetComponent<CanvasScaler>();
            if (canvas == null || scaler == null)
                throw new InvalidOperationException("The existing Game HUD Canvas or Canvas Scaler is missing.");

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (canvasTransform.GetComponent<GraphicRaycaster>() == null)
                canvasTransform.gameObject.AddComponent<GraphicRaycaster>();

            Image overlay = panelTransform.GetComponent<Image>();
            if (overlay == null)
                throw new InvalidOperationException("The existing Main Menu Panel has no background Image.");

            overlay.color = new Color(0.012f, 0.035f, 0.075f, 0.48f);
            overlay.raycastTarget = true;

            CanvasGroup canvasGroup = panelTransform.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = panelTransform.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            MainMenuFadeIn fadeIn = panelTransform.GetComponent<MainMenuFadeIn>();
            if (fadeIn == null)
                fadeIn = panelTransform.gameObject.AddComponent<MainMenuFadeIn>();

            RectTransform cardRect = cardTransform.GetComponent<RectTransform>();
            if (cardRect == null)
                throw new InvalidOperationException("The existing Main Menu Card has no RectTransform.");

            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(760f, 620f);

            VerticalLayoutGroup layout = cardTransform.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = cardTransform.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(56, 56, 32, 32);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.reverseArrangement = false;

            TMP_Text eyebrow = GetMenuText(cardTransform, "Main Menu Eyebrow");
            TMP_Text title = GetMenuText(cardTransform, "Main Menu Title");
            TMP_Text subtitle = GetMenuText(cardTransform, "Main Menu Subtitle");
            TMP_Text description = GetMenuText(cardTransform, "Main Menu Description");
            Transform instructionsTransform = cardTransform.Find("Main Menu Instructions");
            if (description == null && instructionsTransform != null)
            {
                instructionsTransform.name = "Main Menu Description";
                description = instructionsTransform.GetComponent<TMP_Text>();
            }

            Transform buttonTransform = cardTransform.Find("Play Button");
            UnityEngine.UI.Button playButton = buttonTransform != null
                ? buttonTransform.GetComponent<UnityEngine.UI.Button>()
                : null;

            if (eyebrow == null || title == null || subtitle == null || description == null || playButton == null)
                throw new InvalidOperationException("One or more existing main menu text elements or the Play Button are missing.");

            TMP_Text controls = GetMenuText(cardTransform, "Main Menu Controls");
            if (controls == null)
                controls = CreateMenuText(cardTransform, "Main Menu Controls", title.font);
            controls.transform.SetSiblingIndex(4);

            SetText(eyebrow, "A SUNNY LITTLE OBSTACLE COURSE", 18f, 15f, 18f,
                new Color32(132, 232, 205, 255), FontStyles.Bold);
            SetText(title, "ROLL-A-BALL", 72f, 46f, 76f, Color.white, FontStyles.Bold);
            SetText(subtitle, "COIN COLLECTOR", 34f, 26f, 36f,
                new Color32(255, 208, 111, 255), FontStyles.Bold);
            SetText(description,
                "Roll through the gates, dodge the moving obstacles, and collect all 12 golden coins.",
                24f, 19f, 24f, new Color32(235, 243, 249, 255), FontStyles.Normal);
            SetText(controls, "Move with WASD or the Arrow Keys.", 19f, 16f, 20f,
                new Color32(170, 230, 219, 255), FontStyles.Normal);

            SetLayoutElement(eyebrow, 648f, 30f, 1f);
            SetLayoutElement(title, 648f, 94f, 1f);
            SetLayoutElement(subtitle, 648f, 54f, 1f);
            SetLayoutElement(description, 648f, 106f, 1f);
            SetLayoutElement(controls, 648f, 30f, 1f);

            TMP_Text buttonLabel = buttonTransform.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
            {
                SetText(buttonLabel, "PLAY", 28f, 24f, 30f, Color.white, FontStyles.Bold);
                buttonLabel.margin = new Vector4(18f, 6f, 18f, 6f);

                // Restore full-stretch geometry if an earlier repair pass set the child
                // label to a zero-height top anchor.
                RectTransform labelRect = buttonLabel.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.anchoredPosition = Vector2.zero;
                labelRect.sizeDelta = Vector2.zero;
            }

            LayoutElement buttonLayout = buttonTransform.GetComponent<LayoutElement>();
            if (buttonLayout == null)
                buttonLayout = buttonTransform.gameObject.AddComponent<LayoutElement>();
            buttonLayout.minWidth = 280f;
            buttonLayout.preferredWidth = 300f;
            buttonLayout.flexibleWidth = 0f;
            buttonLayout.minHeight = 74f;
            buttonLayout.preferredHeight = 74f;
            buttonLayout.flexibleHeight = 0f;

            RectTransform buttonRect = buttonTransform.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                SetLayoutAnchors(buttonRect);
                buttonRect.sizeDelta = new Vector2(300f, 74f);
                buttonRect.anchoredPosition = Vector2.zero;
            }

            MainMenuButtonHover hover = buttonTransform.GetComponent<MainMenuButtonHover>();
            if (hover == null)
                hover = buttonTransform.gameObject.AddComponent<MainMenuButtonHover>();

            TMP_Text[] menuTexts = { eyebrow, title, subtitle, description, controls, buttonLabel };
            foreach (TMP_Text text in menuTexts)
            {
                if (text == null)
                    continue;

                RectTransform rect = text.GetComponent<RectTransform>();
                // The direct menu rows are controlled by the VerticalLayoutGroup.
                // Leave the Play button's nested label stretched over its parent button.
                if (rect != null && text.transform.parent == cardTransform)
                {
                    SetLayoutAnchors(rect);
                    rect.sizeDelta = Vector2.zero;
                    rect.anchoredPosition = Vector2.zero;
                }

                EditorUtility.SetDirty(text);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
            ValidateResponsiveBounds(cardRect, scaler);
            ValidateMenuChildren(cardRect, layout, menuTexts);

            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(scaler);
            EditorUtility.SetDirty(overlay);
            EditorUtility.SetDirty(canvasGroup);
            EditorUtility.SetDirty(fadeIn);
            EditorUtility.SetDirty(layout);
            EditorUtility.SetDirty(playButton);
            EditorUtility.SetDirty(hover);
            EditorUtility.SetDirty(cardRect);
            EditorUtility.SetDirty(cardTransform.gameObject);
            EditorUtility.SetDirty(panelTransform.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static TMP_Text GetMenuText(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static TMP_Text CreateMenuText(Transform parent, string name, TMP_FontAsset font)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            SetLayoutAnchors(rect);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.raycastTarget = false;
            return text;
        }

        private static void SetText(TMP_Text text, string value, float fontSize, float minSize, float maxSize,
            Color color, FontStyles style)
        {
            text.text = value;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = style;
            text.fontSize = fontSize;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.enableAutoSizing = true;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
            text.richText = false;
            text.paragraphSpacing = 0f;
        }

        private static void SetLayoutElement(TMP_Text text, float preferredWidth, float preferredHeight, float flexibleWidth)
        {
            LayoutElement element = text.GetComponent<LayoutElement>();
            if (element == null)
                element = text.gameObject.AddComponent<LayoutElement>();

            element.minWidth = 0f;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.minHeight = preferredHeight;
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = 0f;
            element.ignoreLayout = false;
            EditorUtility.SetDirty(element);
        }

        private static void SetLayoutAnchors(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void ValidateResponsiveBounds(RectTransform cardRect, CanvasScaler scaler)
        {
            float requiredHeight = layoutHeightTotal(cardRect);
            if (requiredHeight > cardRect.rect.height + 0.01f)
                throw new InvalidOperationException(
                    $"Main menu content needs {requiredHeight:0}px vertically but the card is only {cardRect.rect.height:0}px high.");

            Vector2 reference = scaler.referenceResolution;
            foreach (Vector2 resolution in ValidationResolutions)
            {
                float widthScale = resolution.x / reference.x;
                float heightScale = resolution.y / reference.y;
                float scale = Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(widthScale, 2f), Mathf.Log(heightScale, 2f), scaler.matchWidthOrHeight));
                float availableWidth = resolution.x / scale;
                float availableHeight = resolution.y / scale;
                if (cardRect.rect.width > availableWidth || cardRect.rect.height > availableHeight)
                    throw new InvalidOperationException(
                        $"Main menu card does not fit the {resolution.x:0}x{resolution.y:0} Game View.");

                Debug.Log($"[Roll-a-Ball UI] {resolution.x:0}x{resolution.y:0}: card fits; scale {scale:0.000}, available canvas {availableWidth:0}x{availableHeight:0}.");
            }
        }

        private static void ValidateMenuChildren(RectTransform cardRect, VerticalLayoutGroup layout, TMP_Text[] texts)
        {
            Rect allowed = cardRect.rect;
            allowed.xMin += layout.padding.left;
            allowed.xMax -= layout.padding.right;
            allowed.yMin += layout.padding.bottom;
            allowed.yMax -= layout.padding.top;

            List<Rect> childBounds = new List<Rect>();
            for (int i = 0; i < cardRect.childCount; i++)
            {
                RectTransform child = cardRect.GetChild(i) as RectTransform;
                if (child == null)
                    continue;

                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element == null || element.ignoreLayout)
                    continue;

                Vector3[] corners = new Vector3[4];
                child.GetWorldCorners(corners);
                float minX = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;
                float minY = float.PositiveInfinity;
                float maxY = float.NegativeInfinity;
                foreach (Vector3 corner in corners)
                {
                    Vector3 local = cardRect.InverseTransformPoint(corner);
                    minX = Mathf.Min(minX, local.x);
                    maxX = Mathf.Max(maxX, local.x);
                    minY = Mathf.Min(minY, local.y);
                    maxY = Mathf.Max(maxY, local.y);
                }

                Rect bounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
                if (bounds.xMin < allowed.xMin - 1f || bounds.xMax > allowed.xMax + 1f ||
                    bounds.yMin < allowed.yMin - 1f || bounds.yMax > allowed.yMax + 1f)
                    throw new InvalidOperationException($"'{child.name}' extends outside the Main Menu Card's padded content area.");

                childBounds.Add(bounds);
            }

            childBounds.Sort((a, b) => b.yMax.CompareTo(a.yMax));
            for (int i = 1; i < childBounds.Count; i++)
            {
                float gap = childBounds[i - 1].yMin - childBounds[i].yMax;
                if (gap < layout.spacing - 1f)
                    throw new InvalidOperationException($"Main menu rows overlap or have too little space ({gap:0.0}px gap).");
            }

            foreach (TMP_Text text in texts)
            {
                if (text == null || text.transform == cardRect)
                    continue;

                RectTransform textRect = text.rectTransform;
                float textWidth = textRect.rect.width;
                float textHeight = textRect.rect.height;
                if (textWidth <= 0f || textHeight <= 0f)
                {
                    RectTransform parentRect = textRect.parent as RectTransform;
                    if (parentRect != null)
                    {
                        textWidth = parentRect.rect.width > 0f
                            ? parentRect.rect.width
                            : LayoutUtility.GetPreferredWidth(parentRect);
                        textHeight = parentRect.rect.height > 0f
                            ? parentRect.rect.height
                            : LayoutUtility.GetPreferredHeight(parentRect);
                    }
                }

                if (textWidth <= 0f || textHeight <= 0f)
                    throw new InvalidOperationException($"'{text.name}' and its parent have zero-sized RectTransforms.");

                Vector2 preferred = text.GetPreferredValues(text.text, textWidth, Mathf.Infinity);
                // GetPreferredValues includes TMP margins in its returned size.
                if (preferred.y > textHeight + 1f)
                    throw new InvalidOperationException(
                        $"'{text.name}' needs {preferred.y:0}px of height but has {textHeight:0}px.");
            }

            Debug.Log($"[Roll-a-Ball UI] {texts.Length} menu text boxes fit; {childBounds.Count} vertical rows are inside the card with no overlap.");
        }

        private static float layoutHeightTotal(RectTransform cardRect)
        {
            VerticalLayoutGroup layout = cardRect.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                return float.PositiveInfinity;

            float total = layout.padding.top + layout.padding.bottom;
            int childCount = 0;
            for (int i = 0; i < cardRect.childCount; i++)
            {
                RectTransform child = cardRect.GetChild(i) as RectTransform;
                if (child == null)
                    continue;

                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element != null && element.ignoreLayout)
                    continue;

                float preferredHeight = LayoutUtility.GetPreferredHeight(child);
                if (preferredHeight <= 0f)
                    continue;

                total += preferredHeight;
                childCount++;
            }

            if (childCount > 1)
                total += layout.spacing * (childCount - 1);
            return total;
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindTransform(root.transform, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Transform FindTransform(Transform root, string name)
        {
            if (root.name == name)
                return root;

            foreach (Transform child in root)
            {
                Transform found = FindTransform(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
