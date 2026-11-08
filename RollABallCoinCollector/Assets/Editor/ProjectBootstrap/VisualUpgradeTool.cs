using System;
using System.IO;
using System.Collections.Generic;
using RollABallCoinCollector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RollABallCoinCollector.EditorTools
{
    /// <summary>
    /// Applies lightweight Phase 1 art direction to the existing arena without replacing its gameplay objects.
    /// The pass is safe to run more than once and is also called by the scene generator.
    /// </summary>
    [InitializeOnLoad]
    public static class VisualUpgradeTool
    {
        private const string ScenePath = "Assets/Scenes/RollABallMain.unity";
        private const string MaterialPath = "Assets/Materials";
        private const string SettingsPath = "Assets/Settings";
        private const string TextureFolder = "Assets/Textures";
        private const string GrassTexturePath = TextureFolder + "/RBC_GrassPattern.png";
        private const string VolumeProfilePath = SettingsPath + "/RollABallGlobalVolume.asset";
        private const string SkyboxPath = MaterialPath + "/RBC_AtmosphereSkybox.mat";
        private const string PendingUpgradeKey = "RollABallCoinCollector.PendingPhase1VisualUpgrade";

        static VisualUpgradeTool()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Roll-a-Ball Coin Collector/Apply Phase 1 Visual Upgrade")]
        public static void UpgradeExistingScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorPrefs.SetBool(PendingUpgradeKey, true);
                EditorApplication.isPlaying = false;
                Debug.Log("[Roll-a-Ball] Play Mode is ending; the Phase 1 visual upgrade will run in Edit Mode.");
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

            Debug.Log("[Roll-a-Ball] Phase 1 visual upgrade applied to the existing playable scene.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !EditorPrefs.GetBool(PendingUpgradeKey, false))
                return;

            EditorPrefs.DeleteKey(PendingUpgradeKey);
            EditorApplication.delayCall += UpgradeExistingScene;
        }

        /// <summary>Shared pass used by the in-place upgrade command and the new-scene generator.</summary>
        public static void ApplyToScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ArgumentException("The target scene must be loaded before visual upgrades are applied.", nameof(scene));

            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Settings");
            EnsureFolder("Assets", "Textures");

            Texture2D grassTexture = GetOrCreateGrassTexture();
            Material grass = ConfigureLitMaterial("RBC_Grass", Color.white, 0.06f, 0.48f, Color.black);
            grass.SetTexture("_BaseMap", grassTexture);
            grass.SetTextureScale("_BaseMap", new Vector2(3f, 4f));

            Material deepTeal = ConfigureLitMaterial("RBC_DeepTeal", new Color32(12, 35, 64, 255), 0.32f, 0.74f,
                new Color(0.008f, 0.035f, 0.085f));
            Material mint = ConfigureLitMaterial("RBC_Mint", new Color32(91, 231, 191, 255), 0.12f, 0.72f,
                new Color(0.12f, 0.85f, 0.48f));
            Material trim = ConfigureLitMaterial("RBC_Trim", new Color32(190, 237, 232, 255), 0.24f, 0.62f,
                new Color(0.08f, 0.3f, 0.32f));
            Material playerMaterial = ConfigureLitMaterial("RBC_PlayerBlue", new Color32(36, 139, 255, 255), 0.28f, 0.96f,
                new Color(0.015f, 0.11f, 0.42f));
            Material coinMaterial = ConfigureLitMaterial("RBC_CoinGold", new Color32(255, 188, 47, 255), 0.84f, 0.94f,
                new Color(0.72f, 0.25f, 0.012f));
            Material coinAccent = ConfigureLitMaterial("RBC_CoinLight", new Color32(255, 224, 115, 255), 0.62f, 0.9f,
                new Color(0.38f, 0.12f, 0.005f));
            Material hazard = ConfigureLitMaterial("RBC_HazardRed", new Color32(232, 55, 76, 255), 0.26f, 0.82f,
                new Color(0.38f, 0.018f, 0.032f));
            Material glow = GetOrCreateGlowMaterial("RBC_GlowFX");

            Transform level = FindSceneTransform(scene, "Level Geometry");
            if (level == null)
                throw new InvalidOperationException("The scene does not contain its expected 'Level Geometry' root.");

            ApplySceneMaterials(level, grass, deepTeal, mint, trim, playerMaterial, coinMaterial, coinAccent, hazard);
            ApplyPlayerTrail(level, glow);
            ApplyCoinHalos(level, glow);
            ApplyHazardAccents(level, glow);
            ApplyDecorativeDressing(level, mint, coinAccent, glow);
            ApplyCameraPresentation(scene, level);
            ApplyLightingAndAtmosphere(scene);

            EditorUtility.SetDirty(grass);
            EditorUtility.SetDirty(deepTeal);
            EditorUtility.SetDirty(mint);
            EditorUtility.SetDirty(trim);
            EditorUtility.SetDirty(playerMaterial);
            EditorUtility.SetDirty(coinMaterial);
            EditorUtility.SetDirty(coinAccent);
            EditorUtility.SetDirty(hazard);
            EditorUtility.SetDirty(level.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
        }

        private static void ApplySceneMaterials(
            Transform root,
            Material grass,
            Material deepTeal,
            Material mint,
            Material trim,
            Material player,
            Material coin,
            Material coinAccent,
            Material hazard)
        {
            foreach (Transform item in Enumerate(root))
            {
                Material material = null;
                string name = item.name;

                if (name == "Main Platform") material = grass;
                else if (name.Contains("Safety Rail")) material = deepTeal;
                else if (name.Contains("Platform Accent") || name == "Finish Zone") material = mint;
                else if (name.StartsWith("Edge Marker", StringComparison.Ordinal)) material = trim;
                else if (name == "Player Ball") material = player;
                else if (name == "Coin") material = coin;
                else if (name == "Coin Center") material = coinAccent;
                else if (name == "Start Zone") material = coinAccent;
                else if (name == "Decorative Beacon") material = deepTeal;
                else if (name == "Beacon Cap") material = mint;
                else if (name.StartsWith("Gate ", StringComparison.Ordinal) || name.StartsWith("Moving Sweeper", StringComparison.Ordinal))
                    material = hazard;

                if (material == null)
                    continue;

                Renderer renderer = item.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = material;
            }
        }

        private static void ApplyPlayerTrail(Transform root, Material glow)
        {
            Transform player = FindDescendant(root, "Player Ball");
            if (player == null)
                return;

            TrailRenderer trail = player.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = player.gameObject.AddComponent<TrailRenderer>();

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.1f, 0.8f, 1.8f, 1f), 0f),
                    new GradientColorKey(new Color(0.14f, 0.42f, 1.3f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.72f, 0f),
                    new GradientAlphaKey(0.38f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });

            trail.sharedMaterial = glow;
            trail.colorGradient = gradient;
            trail.time = 0.32f;
            trail.widthMultiplier = 0.34f;
            trail.minVertexDistance = 0.1f;
            trail.numCornerVertices = 4;
            trail.numCapVertices = 4;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            EditorUtility.SetDirty(trail);
        }

        private static void ApplyCoinHalos(Transform root, Material glow)
        {
            foreach (Transform coin in Enumerate(root))
            {
                if (coin.name != "Coin" || coin.GetComponent<CoinPickup>() == null)
                    continue;

                Transform halo = coin.Find("Visual Glow Ring");
                if (halo == null)
                {
                    GameObject ringObject = new GameObject("Visual Glow Ring");
                    ringObject.transform.SetParent(coin, false);
                    halo = ringObject.transform;
                }

                LineRenderer line = halo.GetComponent<LineRenderer>();
                if (line == null)
                    line = halo.gameObject.AddComponent<LineRenderer>();

                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = 49;
                line.widthMultiplier = 0.026f;
                line.numCornerVertices = 4;
                line.numCapVertices = 4;
                line.textureMode = LineTextureMode.Stretch;
                line.sharedMaterial = glow;
                line.startColor = new Color(2.1f, 1.05f, 0.16f, 0.74f);
                line.endColor = line.startColor;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;

                const float radius = 0.56f;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = i * Mathf.PI * 2f / (line.positionCount - 1);
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                }

                EditorUtility.SetDirty(line);
            }
        }

        private static void ApplyHazardAccents(Transform root, Material glow)
        {
            foreach (Transform hazard in Enumerate(root))
            {
                if (!hazard.name.StartsWith("Gate ", StringComparison.Ordinal) &&
                    !hazard.name.StartsWith("Moving Sweeper", StringComparison.Ordinal))
                    continue;

                Transform accent = hazard.Find("Visual Hazard Stripe");
                if (accent == null)
                {
                    GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stripe.name = "Visual Hazard Stripe";
                    stripe.transform.SetParent(hazard, false);
                    Collider collider = stripe.GetComponent<Collider>();
                    if (collider != null)
                        UnityEngine.Object.DestroyImmediate(collider);
                    accent = stripe.transform;
                }

                accent.localPosition = new Vector3(0f, 0.43f, 0f);
                accent.localRotation = Quaternion.identity;
                accent.localScale = new Vector3(0.92f, 0.035f, 0.78f);
                Renderer renderer = accent.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = glow;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }

        private static void ApplyDecorativeDressing(Transform level, Material mint, Material gold, Material glow)
        {
            Transform dressing = level.Find("Visual Dressing");
            if (dressing == null)
            {
                GameObject root = new GameObject("Visual Dressing");
                root.transform.SetParent(level, false);
                dressing = root.transform;
            }

            CreateAmbientOrb(dressing, "Atmosphere Orb 01", new Vector3(-12.5f, 3.2f, -8f), 0.72f, mint);
            CreateAmbientOrb(dressing, "Atmosphere Orb 02", new Vector3(12.7f, 4.4f, -4.8f), 0.48f, gold);
            CreateAmbientOrb(dressing, "Atmosphere Orb 03", new Vector3(-13.4f, 5.1f, 1.2f), 0.46f, gold);
            CreateAmbientOrb(dressing, "Atmosphere Orb 04", new Vector3(12.8f, 3.1f, 5.4f), 0.78f, mint);
            CreateAmbientOrb(dressing, "Atmosphere Orb 05", new Vector3(-11.9f, 4.4f, 12.5f), 0.4f, gold);
            CreateAmbientOrb(dressing, "Atmosphere Orb 06", new Vector3(11.8f, 5.2f, 13.4f), 0.54f, mint);

            Transform sparkleObject = dressing.Find("Atmospheric Sparkles");
            if (sparkleObject == null)
            {
                GameObject sparkles = new GameObject("Atmospheric Sparkles");
                sparkles.transform.SetParent(dressing, false);
                sparkles.transform.localPosition = new Vector3(0f, 4.2f, 0f);
                sparkleObject = sparkles.transform;
            }

            ParticleSystem system = sparkleObject.GetComponent<ParticleSystem>();
            if (system == null)
                system = sparkleObject.gameObject.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 1.1f, 1.35f, 0.5f));
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 2.2f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(25f, 2.8f, 32f);

            ParticleSystemRenderer particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = glow;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            EditorUtility.SetDirty(system);
            EditorUtility.SetDirty(particleRenderer);
        }

        private static void CreateAmbientOrb(Transform parent, string name, Vector3 position, float diameter, Material material)
        {
            Transform orb = parent.Find(name);
            if (orb == null)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = name;
                Collider collider = sphere.GetComponent<Collider>();
                if (collider != null)
                    UnityEngine.Object.DestroyImmediate(collider);
                sphere.transform.SetParent(parent, false);
                orb = sphere.transform;
            }

            orb.position = position;
            orb.localScale = Vector3.one * diameter;
            Renderer renderer = orb.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static void ApplyCameraPresentation(Scene scene, Transform root)
        {
            Transform player = FindDescendant(root, "Player Ball");
            Transform cameraTransform = FindSceneTransform(scene, "Main Camera");
            if (player == null || cameraTransform == null)
                return;

            Camera camera = cameraTransform.GetComponent<Camera>();
            if (camera != null)
            {
                camera.fieldOfView = 52f;
                camera.farClipPlane = 200f;
                camera.allowHDR = true;
                camera.backgroundColor = new Color32(28, 49, 90, 255);
                EditorUtility.SetDirty(camera);
            }

            CameraFollow follow = cameraTransform.GetComponent<CameraFollow>();
            if (follow != null)
            {
                follow.Configure(player, new Vector3(0f, 9.1f, -10.3f));
                EditorUtility.SetDirty(follow);
            }
        }

        private static void ApplyLightingAndAtmosphere(Scene scene)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color32(95, 141, 198, 255);
            RenderSettings.ambientEquatorColor = new Color32(86, 144, 126, 255);
            RenderSettings.ambientGroundColor = new Color32(20, 30, 55, 255);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.9f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color32(35, 55, 91, 255);
            RenderSettings.fogDensity = 0.0022f;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
                if (skybox == null)
                {
                    skybox = new Material(skyShader) { name = "RBC_AtmosphereSkybox" };
                    AssetDatabase.CreateAsset(skybox, SkyboxPath);
                }

                if (skybox.shader != skyShader)
                    skybox.shader = skyShader;
                SetMaterialColorIfPresent(skybox, "_SkyTint", new Color32(124, 175, 237, 255));
                SetMaterialColorIfPresent(skybox, "_GroundColor", new Color32(22, 40, 73, 255));
                SetMaterialFloatIfPresent(skybox, "_AtmosphereThickness", 1.15f);
                SetMaterialFloatIfPresent(skybox, "_SunSize", 0.035f);
                SetMaterialFloatIfPresent(skybox, "_SunSizeConvergence", 7f);
                SetMaterialFloatIfPresent(skybox, "_Exposure", 0.85f);
                RenderSettings.skybox = skybox;
                EditorUtility.SetDirty(skybox);
            }
            else
            {
                Debug.LogWarning("[Roll-a-Ball] Procedural skybox shader was unavailable; the existing skybox is preserved.");
            }

            Light sun = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light.type == LightType.Directional)
                    {
                        sun = light;
                        break;
                    }
                }
                if (sun != null)
                    break;
            }

            if (sun != null)
            {
                sun.color = new Color(1f, 0.88f, 0.72f);
                sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.76f;
                RenderSettings.sun = sun;
                EditorUtility.SetDirty(sun);
            }

            Volume volume = FindComponentInScene<Volume>(scene);
            if (volume == null)
            {
                GameObject volumeObject = new GameObject("Global Volume");
                SceneManager.MoveGameObjectToScene(volumeObject, scene);
                volume = volumeObject.AddComponent<Volume>();
            }

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "RollABallGlobalVolume";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            profile = ConfigurePostProcessing(profile);
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volume);
        }

        private static VolumeProfile ConfigurePostProcessing(VolumeProfile profile)
        {
            Tonemapping tone = GetOrAddOverride<Tonemapping>(profile);
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.ACES;

            ColorAdjustments color = GetOrAddOverride<ColorAdjustments>(profile);
            color.saturation.overrideState = true;
            color.saturation.value = 12f;
            color.contrast.overrideState = true;
            color.contrast.value = 10f;
            color.postExposure.overrideState = true;
            color.postExposure.value = 0.05f;

            Bloom bloom = GetOrAddOverride<Bloom>(profile);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.92f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.52f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.68f;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = false;
            bloom.maxIterations.overrideState = true;
            bloom.maxIterations.value = 5;

            Vignette vignette = GetOrAddOverride<Vignette>(profile);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.12f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.48f;
            vignette.rounded.overrideState = true;
            vignette.rounded.value = true;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T GetOrAddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T component))
                return component;
            return profile.Add<T>(true);
        }

        private static Texture2D GetOrCreateGrassTexture()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
            if (existing != null)
                return existing;

            const int size = 256;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)(size - 1);
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float broad = PeriodicNoise(u, v, 6, 101);
                    float medium = PeriodicNoise(u, v, 16, 307);
                    float fine = PeriodicNoise(u, v, 40, 911);
                    float value = Mathf.Clamp01(broad * 0.56f + medium * 0.3f + fine * 0.14f);
                    float blade = Mathf.SmoothStep(0.86f, 1f, fine) * 0.055f;
                    float light = Mathf.Lerp(0.76f, 1.18f, value) + blade;
                    Color color = new Color(0.09f, 0.34f, 0.19f, 1f) * light;
                    color.r += value * 0.026f;
                    color.b += value * 0.018f;
                    pixels[y * size + x] = color;
                }
            }

            Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, true, false)
            {
                name = "RBC_GrassPattern",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            generated.SetPixels32(pixels);
            generated.Apply(true, false);

            string absolutePath = Path.GetFullPath(GrassTexturePath);
            File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(GrassTexturePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(GrassTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;
                importer.maxTextureSize = 512;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(GrassTexturePath);
        }

        private static float PeriodicNoise(float u, float v, int cells, int seed)
        {
            float x = u * cells;
            float y = v * cells;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = (x0 + 1) % cells;
            int y1 = (y0 + 1) % cells;
            x0 %= cells;
            y0 %= cells;

            float tx = x - Mathf.Floor(x);
            float ty = y - Mathf.Floor(y);
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);

            float a = Mathf.Lerp(HashNoise(x0, y0, seed), HashNoise(x1, y0, seed), tx);
            float b = Mathf.Lerp(HashNoise(x0, y1, seed), HashNoise(x1, y1, seed), tx);
            return Mathf.Lerp(a, b, ty);
        }

        private static float HashNoise(int x, int y, int seed)
        {
            uint hash = unchecked((uint)x * 374761393u ^ (uint)y * 668265263u ^ (uint)seed * 1442695041u);
            hash = unchecked((hash ^ (hash >> 13)) * 1274126177u);
            hash ^= hash >> 16;
            return (hash & 0x00ffffffu) / 16777215f;
        }

        private static Material ConfigureLitMaterial(string name, Color baseColor, float metallic, float smoothness, Color emission)
        {
            string path = $"{MaterialPath}/{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("The URP Lit shader is unavailable. Wait for URP to finish importing, then retry.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name, enableInstancing = true };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;

            if (material.HasProperty("_ClearCoatMask"))
            {
                material.SetFloat("_ClearCoatMask", name == "RBC_PlayerBlue" ? 0.25f : 0f);
                material.SetFloat("_ClearCoatSmoothness", 0.96f);
            }

            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetOrCreateGlowMaterial(string name)
        {
            string path = $"{MaterialPath}/{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("No supported URP unlit shader was found for the visual glow effects.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name, enableInstancing = true };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            SetMaterialColorIfPresent(material, "_BaseColor", Color.white);
            SetMaterialColorIfPresent(material, "_Color", Color.white);
            SetMaterialFloatIfPresent(material, "_Surface", 1f);
            SetMaterialFloatIfPresent(material, "_Blend", 0f);
            SetMaterialFloatIfPresent(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetMaterialFloatIfPresent(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetMaterialFloatIfPresent(material, "_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetMaterialColorIfPresent(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
                material.SetColor(property, value);
        }

        private static void SetMaterialFloatIfPresent(Material material, string property, float value)
        {
            if (material.HasProperty(property))
                material.SetFloat(property, value);
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folder);
        }

        private static Transform FindSceneTransform(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform found = FindDescendant(root.transform, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name)
                return root;
            foreach (Transform child in root)
            {
                Transform found = FindDescendant(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static IEnumerable<Transform> Enumerate(Transform root)
        {
            yield return root;
            foreach (Transform child in root)
            {
                foreach (Transform descendant in Enumerate(child))
                    yield return descendant;
            }
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }
    }
}
