using System;
using System.IO;
using System.Linq;
using RollABallCoinCollector;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RollABallCoinCollector.EditorTools
{
    /// <summary>
    /// Creates the complete playable scene from built-in shapes and saves it as a normal Unity scene.
    /// Run from Tools > Roll-a-Ball Coin Collector > Generate Playable Scene.
    /// </summary>
    public static class RollABallSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/RollABallMain.unity";
        private const string MaterialsPath = "Assets/Materials";
        private const string ProfilesPath = "Assets/Settings";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        private static bool waitingForTmpImport;
        private static double tmpImportDeadline;

        [MenuItem("Tools/Roll-a-Ball Coin Collector/Generate Playable Scene")]
        public static void GeneratePlayableScene()
        {
            EnsureProjectFolders();

            if (File.Exists(TmpSettingsPath))
            {
                BuildAndSaveScene();
                return;
            }

            if (waitingForTmpImport)
            {
                Debug.Log("[Roll-a-Ball] TextMeshPro resources are already importing.");
                return;
            }

            UnityEditor.PackageManager.PackageInfo uGuiPackage = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .FirstOrDefault(package => package.name == "com.unity.ugui");
            if (uGuiPackage == null)
                throw new InvalidOperationException("Unity UI (com.unity.ugui) is not installed.");

            string essentialsPackage = Path.Combine(
                uGuiPackage.resolvedPath,
                "Package Resources",
                "TMP Essential Resources.unitypackage");
            if (!File.Exists(essentialsPackage))
                throw new FileNotFoundException("The TextMeshPro essential resources were not found in the installed uGUI package.", essentialsPackage);

            waitingForTmpImport = true;
            tmpImportDeadline = EditorApplication.timeSinceStartup + 120d;
            AssetDatabase.importPackageCompleted += OnTmpResourcesImported;
            EditorApplication.update += WaitForTmpResources;

            // This is the same non-interactive AssetPackage import used by TMP's built-in resource importer.
            UnityEditor.AssetPackage.Package.Import(essentialsPackage, false);
            Debug.Log("[Roll-a-Ball] Importing TextMeshPro essential resources before building the scene.");
        }

        private static void OnTmpResourcesImported(string packageName)
        {
            if (packageName != "TMP Essential Resources")
                return;

            AssetDatabase.importPackageCompleted -= OnTmpResourcesImported;
            EditorApplication.update -= WaitForTmpResources;
            waitingForTmpImport = false;
            EditorApplication.delayCall += BuildAndSaveScene;
        }

        private static void WaitForTmpResources()
        {
            if (File.Exists(TmpSettingsPath))
            {
                AssetDatabase.importPackageCompleted -= OnTmpResourcesImported;
                EditorApplication.update -= WaitForTmpResources;
                waitingForTmpImport = false;
                EditorApplication.delayCall += BuildAndSaveScene;
                return;
            }

            if (EditorApplication.timeSinceStartup > tmpImportDeadline)
            {
                AssetDatabase.importPackageCompleted -= OnTmpResourcesImported;
                EditorApplication.update -= WaitForTmpResources;
                waitingForTmpImport = false;
                Debug.LogError("[Roll-a-Ball] Timed out waiting for TMP essentials. Import them from Window > TextMeshPro > Import TMP Essential Resources, then run Generate Playable Scene again.");
            }
        }

        private static void BuildAndSaveScene()
        {
            EnsureProjectFolders();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new InvalidOperationException($"Scene already exists at {ScenePath}; it was not overwritten.");

            UniversalRenderPipelineAsset activePipeline = UniversalRenderPipeline.asset;
            if (activePipeline == null)
                throw new InvalidOperationException("URP is not active. Open this project with the Universal 3D template settings.");
            if (!activePipeline.supportsHDR)
                Debug.LogWarning("[Roll-a-Ball] HDR is disabled on the active URP asset; bloom will be subtle.");

            TMP_FontAsset font = GetDefaultFont();
            if (font == null)
                throw new InvalidOperationException("TextMeshPro's default font was not imported. Import TMP Essential Resources and try again.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "RollABallMain";

            Material grassMaterial = GetOrCreateMaterial("RBC_Grass", new Color32(39, 139, 104, 255), 0.12f, 0.32f);
            Material edgeMaterial = GetOrCreateMaterial("RBC_DeepTeal", new Color32(18, 63, 81, 255), 0.35f, 0.48f);
            Material accentMaterial = GetOrCreateMaterial("RBC_Mint", new Color32(105, 218, 184, 255), 0.08f, 0.5f);
            Material playerMaterial = GetOrCreateMaterial("RBC_PlayerBlue", new Color32(35, 139, 255, 255), 0.6f, 0.82f);
            Material coinMaterial = GetOrCreateMaterial("RBC_CoinGold", new Color32(255, 190, 39, 255), 0.82f, 0.88f, new Color(1f, 0.48f, 0.05f));
            Material coinAccentMaterial = GetOrCreateMaterial("RBC_CoinLight", new Color32(255, 225, 112, 255), 0.72f, 0.9f);
            Material hazardMaterial = GetOrCreateMaterial("RBC_HazardRed", new Color32(227, 58, 68, 255), 0.28f, 0.62f, new Color(0.6f, 0.03f, 0.03f));
            Material trimMaterial = GetOrCreateMaterial("RBC_Trim", new Color32(227, 245, 244, 255), 0.18f, 0.44f);

            CreateLightingAndVolume();

            GameObject worldRoot = new GameObject("Level Geometry");
            CreateArena(worldRoot.transform, grassMaterial, edgeMaterial, accentMaterial, trimMaterial);

            GameObject gameManagerObject = new GameObject("Game Manager");
            GameManager gameManager = gameManagerObject.AddComponent<GameManager>();
            GameAudio gameAudio = gameManagerObject.AddComponent<GameAudio>();
            AudioSource effectsSource = gameManagerObject.AddComponent<AudioSource>();
            AudioSource musicSource = gameManagerObject.AddComponent<AudioSource>();
            gameAudio.ConfigureSources(effectsSource, musicSource);

            Transform playerTransform = CreatePlayer(worldRoot.transform, playerMaterial, out Rigidbody playerBody);
            GameObject cameraObject = CreateCamera(playerTransform, out Camera mainCamera);
            PlayerMovement movement = playerTransform.GetComponent<PlayerMovement>();
            movement.Configure(playerBody, mainCamera);
            playerTransform.GetComponent<FallGuard>().Configure(
                gameManager,
                new Vector3(0f, 0.78f, -11f),
                new Vector2(-9.2f, 9.2f),
                new Vector2(-12.2f, 12.2f));

            CreateCourse(worldRoot.transform, gameManager, coinMaterial, coinAccentMaterial, hazardMaterial);
            GameHUD hud = CreateHud(font, gameManager);
            gameManager.Configure(hud, gameAudio);

            VisualUpgradeTool.ApplyToScene(scene);
            MainMenuLayoutFix.ApplyToScene(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = cameraObject;
            Debug.Log($"[Roll-a-Ball] Playable scene generated and added to Build Settings: {ScenePath}");
        }

        private static void EnsureProjectFolders()
        {
            CreateFolderIfMissing("Assets", "Materials");
            CreateFolderIfMissing("Assets", "Scripts");
            CreateFolderIfMissing("Assets", "Scenes");
            CreateFolderIfMissing("Assets", "Settings");
        }

        private static void CreateFolderIfMissing(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        private static TMP_FontAsset GetDefaultFont()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font != null)
                return font;

            string[] fontGuids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset");
            return fontGuids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontGuids[0]));
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float metallic, float smoothness, Color? emission = null)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null)
                throw new InvalidOperationException("The URP Lit shader was not found. Confirm that URP finished compiling.");

            Material material = new Material(litShader)
            {
                name = name
            };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateLightingAndVolume()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color32(171, 219, 236, 255);
            RenderSettings.ambientEquatorColor = new Color32(135, 195, 171, 255);
            RenderSettings.ambientGroundColor = new Color32(40, 63, 73, 255);
            RenderSettings.reflectionIntensity = 0.85f;

            GameObject lightObject = new GameObject("Sun Light");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.94f, 0.82f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.78f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            string profilePath = $"{ProfilesPath}/RollABallGlobalVolume.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "RollABallGlobalVolume";
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            Tonemapping tone = GetOrAddOverride<Tonemapping>(profile);
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.ACES;

            ColorAdjustments color = GetOrAddOverride<ColorAdjustments>(profile);
            color.saturation.overrideState = true;
            color.saturation.value = 8f;
            color.contrast.overrideState = true;
            color.contrast.value = 8f;
            color.postExposure.overrideState = true;
            color.postExposure.value = 0.15f;

            Bloom bloom = GetOrAddOverride<Bloom>(profile);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.35f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.68f;

            Vignette vignette = GetOrAddOverride<Vignette>(profile);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.16f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.45f;

            GameObject volumeObject = new GameObject("Global Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
        }

        private static T GetOrAddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
                return component;
            return profile.Add<T>(true);
        }

        private static void CreateArena(Transform parent, Material ground, Material edge, Material accent, Material trim)
        {
            CreatePrimitive("Main Platform", PrimitiveType.Cube, parent, new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 26f), ground);

            CreatePrimitive("Left Safety Rail", PrimitiveType.Cube, parent, new Vector3(-10f, 0.82f, 0f), new Vector3(0.7f, 1.64f, 27.2f), edge);
            CreatePrimitive("Right Safety Rail", PrimitiveType.Cube, parent, new Vector3(10f, 0.82f, 0f), new Vector3(0.7f, 1.64f, 27.2f), edge);
            CreatePrimitive("Near Safety Rail", PrimitiveType.Cube, parent, new Vector3(0f, 0.82f, -13f), new Vector3(20.7f, 1.64f, 0.7f), edge);
            CreatePrimitive("Far Safety Rail", PrimitiveType.Cube, parent, new Vector3(0f, 0.82f, 13f), new Vector3(20.7f, 1.64f, 0.7f), edge);

            CreatePrimitive("Front Platform Accent", PrimitiveType.Cube, parent, new Vector3(0f, 0.012f, -12.2f), new Vector3(18.4f, 0.025f, 0.12f), accent);
            CreatePrimitive("Back Platform Accent", PrimitiveType.Cube, parent, new Vector3(0f, 0.012f, 12.2f), new Vector3(18.4f, 0.025f, 0.12f), accent);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int row = 0; row < 11; row++)
                {
                    float z = -10.5f + row * 2.1f;
                    CreatePrimitive($"Edge Marker {side}_{row}", PrimitiveType.Cube, parent,
                        new Vector3(side * 9.45f, 0.02f, z), new Vector3(0.09f, 0.04f, 0.58f), trim);
                }
            }

            CreatePylon(parent, new Vector3(-8.8f, 0f, -11.4f), edge, accent);
            CreatePylon(parent, new Vector3(8.8f, 0f, -11.4f), edge, accent);
            CreatePylon(parent, new Vector3(-8.8f, 0f, 11.4f), edge, accent);
            CreatePylon(parent, new Vector3(8.8f, 0f, 11.4f), edge, accent);
        }

        private static void CreatePylon(Transform parent, Vector3 position, Material baseMaterial, Material capMaterial)
        {
            GameObject pillar = CreatePrimitive("Decorative Beacon", PrimitiveType.Cylinder, parent,
                position + Vector3.up * 0.48f, new Vector3(0.34f, 0.48f, 0.34f), baseMaterial);
            RemoveCollider(pillar);
            GameObject cap = CreatePrimitive("Beacon Cap", PrimitiveType.Cylinder, parent,
                position + Vector3.up * 1.02f, new Vector3(0.56f, 0.1f, 0.56f), capMaterial);
            RemoveCollider(cap);
        }

        private static Transform CreatePlayer(Transform parent, Material material, out Rigidbody body)
        {
            GameObject player = CreatePrimitive("Player Ball", PrimitiveType.Sphere, parent,
                new Vector3(0f, 0.78f, -11f), Vector3.one * 1.5f, material);
            player.tag = "Player";

            body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.linearDamping = 0.35f;
            body.angularDamping = 0.05f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            player.AddComponent<PlayerMovement>();
            player.AddComponent<FallGuard>();
            return player.transform;
        }

        private static GameObject CreateCamera(Transform player, out Camera camera)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 180f;
            camera.allowHDR = true;
            camera.backgroundColor = new Color32(165, 213, 234, 255);

            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = ~0;
            cameraObject.AddComponent<AudioListener>();

            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();
            follow.Configure(player, new Vector3(0f, 8f, -9f));
            cameraObject.transform.position = player.position + new Vector3(0f, 8f, -9f);
            cameraObject.transform.rotation = Quaternion.LookRotation(player.position + Vector3.up - cameraObject.transform.position);
            return cameraObject;
        }

        private static void CreateCourse(
            Transform parent,
            GameManager gameManager,
            Material coinMaterial,
            Material coinAccentMaterial,
            Material hazardMaterial)
        {
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(-6f, 1.05f, -9f));
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(0f, 1.05f, -9f));
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(6f, 1.05f, -9f));

            CreateBarrier(parent, gameManager, hazardMaterial, "Gate One Left", new Vector3(-5.8f, 0.57f, -5f), new Vector3(8.4f, 1.14f, 0.65f));
            CreateBarrier(parent, gameManager, hazardMaterial, "Gate One Right", new Vector3(5.8f, 0.57f, -5f), new Vector3(8.4f, 1.14f, 0.65f));

            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(-6f, 1.05f, -2f));
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(0f, 1.05f, -2f));
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(6f, 1.05f, -2f));

            CreateBarrier(parent, gameManager, hazardMaterial, "Gate Two Near", new Vector3(-7.3f, 0.57f, 2f), new Vector3(5.4f, 1.14f, 0.65f));
            CreateBarrier(parent, gameManager, hazardMaterial, "Gate Two Far", new Vector3(4.2f, 0.57f, 2f), new Vector3(11.6f, 1.14f, 0.65f));

            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(-6f, 1.05f, 5f));
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(0f, 1.05f, 5f));
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(6f, 1.05f, 5f));

            CreateMovingHazard(parent, gameManager, hazardMaterial, "Moving Sweeper One", new Vector3(0f, 0.55f, -0.8f),
                new Vector3(2.5f, 1.1f, 0.85f), Vector3.right, 3.4f, 1.25f);
            CreateMovingHazard(parent, gameManager, hazardMaterial, "Moving Sweeper Two", new Vector3(0f, 0.55f, 6.5f),
                new Vector3(2.5f, 1.1f, 0.85f), Vector3.right, 3.8f, 1.05f);

            CreateBarrier(parent, gameManager, hazardMaterial, "Gate Three Left", new Vector3(-5.8f, 0.57f, 8f), new Vector3(8.4f, 1.14f, 0.65f));
            CreateBarrier(parent, gameManager, hazardMaterial, "Gate Three Right", new Vector3(5.8f, 0.57f, 8f), new Vector3(8.4f, 1.14f, 0.65f));

            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(-4f, 1.05f, 10.5f));
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(0f, 1.05f, 10.5f));
            CreateCoin(parent, gameManager, coinMaterial, coinAccentMaterial, new Vector3(4f, 1.05f, 10.5f));

            // Small floor pads make the route easier to read without adding colliders.
            CreatePrimitive("Start Zone", PrimitiveType.Cube, parent, new Vector3(0f, 0.01f, -10.9f), new Vector3(3.8f, 0.025f, 0.18f), coinAccentMaterial);
            CreatePrimitive("Finish Zone", PrimitiveType.Cube, parent, new Vector3(0f, 0.01f, 11.8f), new Vector3(6f, 0.025f, 0.18f), coinAccentMaterial);
        }

        private static void CreateCoin(Transform parent, GameManager manager, Material gold, Material highlight, Vector3 position)
        {
            GameObject coin = CreatePrimitive("Coin", PrimitiveType.Cylinder, parent, position, new Vector3(0.86f, 0.11f, 0.86f), gold);
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Collider coinCollider = coin.GetComponent<Collider>();
            coinCollider.isTrigger = true;
            CoinPickup pickup = coin.AddComponent<CoinPickup>();
            pickup.Configure(manager);

            GameObject center = CreatePrimitive("Coin Center", PrimitiveType.Cylinder, coin.transform,
                Vector3.zero, new Vector3(0.49f, 0.025f, 0.49f), highlight);
            center.transform.localPosition = Vector3.zero;
            center.transform.localRotation = Quaternion.identity;
            RemoveCollider(center);
        }

        private static void CreateBarrier(Transform parent, GameManager manager, Material material, string name, Vector3 position, Vector3 scale)
        {
            GameObject barrier = CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, material);
            barrier.AddComponent<DangerousObstacle>().Configure(manager);
        }

        private static void CreateMovingHazard(
            Transform parent,
            GameManager manager,
            Material material,
            string name,
            Vector3 position,
            Vector3 scale,
            Vector3 axis,
            float distance,
            float speed)
        {
            GameObject hazard = CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, material);
            Rigidbody body = hazard.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            hazard.AddComponent<DangerousObstacle>().Configure(manager);
            hazard.AddComponent<MovingObstacle>().Configure(axis, distance, speed);
        }

        private static GameHUD CreateHud(TMP_FontAsset font, GameManager manager)
        {
            GameObject canvasObject = new GameObject("Game HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject eventSystemObject = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            GameObject gameplayHud = new GameObject("Gameplay HUD", typeof(RectTransform));
            gameplayHud.transform.SetParent(canvasObject.transform, false);
            Stretch((RectTransform)gameplayHud.transform);

            GameObject coinCard = CreateImagePanel("Coin Counter Card", gameplayHud.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 82f), new Vector2(42f, -38f),
                new Color(0.035f, 0.11f, 0.16f, 0.91f), true);
            TMP_Text coinsText = CreateText("Coin Counter", coinCard.transform, font, "COINS   00 / 12", 29f,
                Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            GameObject livesCard = CreateImagePanel("Lives Card", gameplayHud.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, 82f), new Vector2(366f, -38f),
                new Color(0.035f, 0.11f, 0.16f, 0.91f), true);
            TMP_Text livesText = CreateText("Lives Counter", livesCard.transform, font, "LIVES   3", 29f,
                new Color32(255, 225, 218, 255), TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            GameObject levelCard = CreateImagePanel("Level Card", gameplayHud.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(340f, 82f), new Vector2(-42f, -38f),
                new Color(0.035f, 0.11f, 0.16f, 0.91f), true);
            CreateText("Level Label", levelCard.transform, font, "LEVEL 01   •   MEADOW RUN", 24f,
                Color.white, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            CreateText("Controls Hint", gameplayHud.transform, font, "WASD / ARROWS TO ROLL     •     COLLECT EVERY COIN",
                22f, new Color32(237, 250, 246, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(690f, 48f), new Vector2(0f, 32f));

            GameObject menuPanel = CreateImagePanel("Main Menu Panel", canvasObject.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                new Color(0.012f, 0.035f, 0.075f, 0.48f), false);
            GameObject menuCard = CreateImagePanel("Main Menu Card", menuPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760f, 620f), Vector2.zero,
                new Color32(20, 55, 75, 250), true);
            CreateText("Main Menu Eyebrow", menuCard.transform, font, "A SUNNY LITTLE OBSTACLE COURSE", 18f,
                new Color32(121, 226, 193, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(648f, 30f), Vector2.zero);
            CreateText("Main Menu Title", menuCard.transform, font, "ROLL-A-BALL", 72f,
                Color.white, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(648f, 94f), Vector2.zero);
            CreateText("Main Menu Subtitle", menuCard.transform, font, "COIN COLLECTOR", 34f,
                new Color32(255, 206, 82, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(648f, 54f), Vector2.zero);
            CreateText("Main Menu Description", menuCard.transform,
                font,
                "Roll through the gates, dodge the moving obstacles, and collect all 12 golden coins.",
                24f, new Color32(235, 243, 249, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(648f, 106f), Vector2.zero);
            CreateText("Main Menu Controls", menuCard.transform, font, "Move with WASD or the Arrow Keys.", 19f,
                new Color32(170, 230, 219, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(648f, 30f), Vector2.zero);
            UnityEngine.UI.Button playButton = CreateButton("Play Button", "PLAY", menuCard.transform, font,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 74f), Vector2.zero,
                new Color32(34, 186, 145, 255), new Color32(77, 219, 178, 255));

            GameObject resultPanel = CreateImagePanel("Result Panel", canvasObject.transform,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                new Color(0.012f, 0.045f, 0.08f, 0.78f), false);
            GameObject resultCard = CreateImagePanel("Result Card", resultPanel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620f, 440f), Vector2.zero,
                new Color32(20, 55, 75, 250), true);
            TMP_Text resultTitle = CreateText("Result Title", resultCard.transform, font, "YOU WIN!", 58f,
                new Color32(255, 211, 93, 255), TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(550f, 86f), new Vector2(0f, -64f));
            TMP_Text resultDescription = CreateText("Result Description", resultCard.transform, font, "Every coin collected!", 26f,
                Color.white, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(550f, 120f), new Vector2(0f, 30f));
            UnityEngine.UI.Button restartButton = CreateButton("Restart Button", "RESTART", resultCard.transform, font,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(260f, 70f), new Vector2(0f, 130f),
                new Color32(34, 186, 145, 255), new Color32(77, 219, 178, 255));
            UnityEngine.UI.Button mainMenuButton = CreateButton("Main Menu Button", "MAIN MENU", resultCard.transform, font,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(260f, 70f), new Vector2(0f, 48f),
                new Color32(44, 94, 121, 255), new Color32(63, 129, 158, 255));

            menuPanel.SetActive(true);
            resultPanel.SetActive(false);
            gameplayHud.SetActive(false);

            GameHUD hud = canvasObject.AddComponent<GameHUD>();
            hud.Configure(manager, coinsText, livesText, resultTitle, resultDescription,
                gameplayHud, menuPanel, resultPanel, playButton, restartButton, null, mainMenuButton);
            return hud;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType primitiveType, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return gameObject;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);
        }

        private static GameObject CreateImagePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size,
            Vector2 position,
            Color color,
            bool addShadow)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            RectTransform rect = (RectTransform)panel.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            UnityEngine.UI.Image image = panel.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = true;
            if (addShadow)
            {
                UnityEngine.UI.Shadow shadow = panel.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
                shadow.effectDistance = new Vector2(0f, -6f);
            }

            return panel;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2 position)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)textObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static UnityEngine.UI.Button CreateButton(
            string name,
            string label,
            Transform parent,
            TMP_FontAsset font,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2 position,
            Color normalColor,
            Color highlightColor)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin + anchorMax) * 0.5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();
            image.color = normalColor;
            UnityEngine.UI.Button button = buttonObject.GetComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            ColorBlock colorBlock = button.colors;
            colorBlock.normalColor = normalColor;
            colorBlock.highlightedColor = highlightColor;
            colorBlock.selectedColor = highlightColor;
            colorBlock.pressedColor = Color.Lerp(normalColor, Color.black, 0.18f);
            colorBlock.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.5f);
            button.colors = colorBlock;

            TMP_Text buttonLabel = CreateText("Label", buttonObject.transform, font, label, 27f, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            buttonLabel.fontStyle = FontStyles.Bold;
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
