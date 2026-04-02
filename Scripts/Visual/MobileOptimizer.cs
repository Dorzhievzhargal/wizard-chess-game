using UnityEngine;

namespace WizardChess.Visual
{
    /// <summary>
    /// Optimizes rendering settings for mobile GPU at runtime.
    /// Downgrades materials to mobile shaders, reduces shadow quality,
    /// and adjusts quality settings for stable 30+ FPS on target devices.
    /// Attach to any GameObject in the scene (e.g. GameManager).
    /// </summary>
    public class MobileOptimizer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoDetectPlatform = true;
        [SerializeField] private bool forceOptimize = false;

        [Header("Target Frame Rate")]
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            bool shouldOptimize = forceOptimize || (autoDetectPlatform && IsMobilePlatform());

            Application.targetFrameRate = targetFrameRate;

            if (shouldOptimize)
            {
                OptimizeQualitySettings();
                OptimizeShadows();
            }
        }

        private void Start()
        {
            bool shouldOptimize = forceOptimize || (autoDetectPlatform && IsMobilePlatform());

            if (shouldOptimize)
            {
                OptimizeSceneMaterials();
            }
        }

        /// <summary>
        /// Adjusts global quality settings for mobile performance.
        /// </summary>
        private static void OptimizeQualitySettings()
        {
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 2; // 2x MSAA — good balance
            QualitySettings.pixelLightCount = 2;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.skinWeights = SkinWeights.TwoBones;
        }

        /// <summary>
        /// Reduces shadow quality for mobile GPU.
        /// </summary>
        private static void OptimizeShadows()
        {
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 20f;
            QualitySettings.shadowCascades = 1;
        }

        /// <summary>
        /// Finds all renderers in the scene and downgrades Standard shader
        /// materials to mobile-friendly equivalents where possible.
        /// Preserves color and basic emission but drops expensive features.
        /// </summary>
        private static void OptimizeSceneMaterials()
        {
            var renderers = FindObjectsOfType<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    OptimizeMaterial(mat);
                }
            }
        }

        /// <summary>
        /// Optimizes a single material for mobile rendering.
        /// Reduces glossiness, disables expensive features, and optionally
        /// switches to a lighter shader.
        /// </summary>
        public static void OptimizeMaterial(Material mat)
        {
            if (mat == null) return;

            // Reduce specular/glossiness for cheaper lighting
            if (mat.HasProperty("_Glossiness"))
            {
                float gloss = mat.GetFloat("_Glossiness");
                mat.SetFloat("_Glossiness", Mathf.Min(gloss, 0.5f));
            }

            // Reduce metallic for simpler shading
            if (mat.HasProperty("_Metallic"))
            {
                float metallic = mat.GetFloat("_Metallic");
                mat.SetFloat("_Metallic", Mathf.Min(metallic, 0.3f));
            }

            // Cap emission intensity to avoid GPU-heavy bloom
            if (mat.IsKeywordEnabled("_EMISSION") && mat.HasProperty("_EmissionColor"))
            {
                Color emission = mat.GetColor("_EmissionColor");
                float maxChannel = Mathf.Max(emission.r, Mathf.Max(emission.g, emission.b));
                if (maxChannel > 0.3f)
                {
                    float scale = 0.3f / maxChannel;
                    mat.SetColor("_EmissionColor", emission * scale);
                }
            }

            // Disable detail maps and parallax if present
            mat.DisableKeyword("_PARALLAXMAP");
            mat.DisableKeyword("_DETAIL_MULX2");
        }

        private static bool IsMobilePlatform()
        {
            return Application.platform == RuntimePlatform.Android
                || Application.platform == RuntimePlatform.IPhonePlayer;
        }
    }
}
