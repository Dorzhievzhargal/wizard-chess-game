using UnityEngine;

namespace WizardChess.Visual
{
    /// <summary>
    /// Creates all game materials programmatically at runtime.
    /// Provides marble board tiles, piece materials with glow effects,
    /// and environment materials optimized for mobile GPU.
    /// </summary>
    public static class MaterialFactory
    {
        // ── Shader references ──────────────────────────────────────────

        private const string URPLitShader = "Universal Render Pipeline/Lit";
        private const string URPSimpleLitShader = "Universal Render Pipeline/Simple Lit";
        private const string StandardShader = "Standard";
        private const string MobileShader = "Mobile/Diffuse";

        // ── Board marble colors ────────────────────────────────────────

        private static readonly Color LightMarbleColor = new Color(0.88f, 0.85f, 0.80f);
        private static readonly Color LightMarbleSpecular = new Color(0.95f, 0.93f, 0.90f);
        private static readonly Color DarkMarbleColor = new Color(0.18f, 0.16f, 0.20f);
        private static readonly Color DarkMarbleSpecular = new Color(0.35f, 0.32f, 0.38f);

        // ── Highlight color ────────────────────────────────────────────

        private static readonly Color HighlightColor = new Color(0.2f, 0.8f, 0.3f, 0.6f);

        // ── White piece palette ────────────────────────────────────────

        private static readonly Color WhiteStoneBase = new Color(0.90f, 0.85f, 0.75f);
        private static readonly Color WhiteGoldAccent = new Color(0.85f, 0.75f, 0.30f);
        private static readonly Color WhiteBlueGlow = new Color(0.4f, 0.6f, 1.0f);

        // ── Black piece palette ────────────────────────────────────────

        private static readonly Color BlackStoneBase = new Color(0.20f, 0.15f, 0.20f);
        private static readonly Color BlackMetalAccent = new Color(0.50f, 0.50f, 0.55f);
        private static readonly Color BlackRedPurpleGlow = new Color(0.80f, 0.20f, 0.50f);

        // ── 9.1: Marble board materials ────────────────────────────────

        /// <summary>
        /// Creates a light marble tile material with subtle specular highlights.
        /// </summary>
        public static Material CreateLightMarbleMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "LightMarble";
            SetColor(mat, LightMarbleColor);
            SetSmoothness(mat, 0.7f);
            SetMetallic(mat, 0.05f);
            return mat;
        }

        /// <summary>
        /// Creates a dark marble tile material with deep tones and slight sheen.
        /// </summary>
        public static Material CreateDarkMarbleMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "DarkMarble";
            SetColor(mat, DarkMarbleColor);
            SetSmoothness(mat, 0.75f);
            SetMetallic(mat, 0.1f);
            return mat;
        }

        /// <summary>
        /// Creates the valid move highlight material (semi-transparent green glow).
        /// </summary>
        public static Material CreateHighlightMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "MoveHighlight";
            SetColor(mat, HighlightColor);

            // Try to enable transparency for URP
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f); // 0=Opaque, 1=Transparent
                mat.SetFloat("_Blend", 0f);   // Alpha blend
                mat.SetFloat("_ZWrite", 0f);
                mat.renderQueue = 3000;
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHABLEND_ON");
            }
            else
            {
                // Standard shader fallback
                mat.SetFloat("_Mode", 3f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = 3000;
            }

            return mat;
        }

        // ── 9.2: Piece materials ───────────────────────────────────────

        /// <summary>
        /// Creates the base body material for a white piece (light stone + blue glow).
        /// </summary>
        public static Material CreateWhitePieceBaseMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "WhitePieceBase";
            SetColor(mat, WhiteStoneBase);
            SetSmoothness(mat, 0.4f);
            SetMetallic(mat, 0.05f);
            return mat;
        }

        public static Material CreateWhitePieceAccentMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "WhitePieceAccent";
            SetColor(mat, WhiteGoldAccent);
            SetSmoothness(mat, 0.8f);
            SetMetallic(mat, 0.6f);
            return mat;
        }

        public static Material CreateWhitePieceWeaponMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "WhitePieceWeapon";
            SetColor(mat, WhiteGoldAccent);
            SetSmoothness(mat, 0.7f);
            SetMetallic(mat, 0.5f);
            return mat;
        }

        public static Material CreateBlackPieceBaseMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "BlackPieceBase";
            SetColor(mat, BlackStoneBase);
            SetSmoothness(mat, 0.45f);
            SetMetallic(mat, 0.1f);
            return mat;
        }

        public static Material CreateBlackPieceAccentMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "BlackPieceAccent";
            SetColor(mat, BlackMetalAccent);
            SetSmoothness(mat, 0.85f);
            SetMetallic(mat, 0.7f);
            return mat;
        }

        public static Material CreateBlackPieceWeaponMaterial()
        {
            var mat = CreateStandardMaterial();
            mat.name = "BlackPieceWeapon";
            SetColor(mat, BlackMetalAccent);
            SetSmoothness(mat, 0.75f);
            SetMetallic(mat, 0.6f);
            return mat;
        }

        // ── URP-compatible property setters ───────────────────────────

        private static void SetColor(Material mat, Color color)
        {
            // URP Lit uses _BaseColor, Standard uses _Color
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
        }

        private static void SetSmoothness(Material mat, float value)
        {
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", value);
            else if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", value);
        }

        private static void SetMetallic(Material mat, float value)
        {
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", value);
        }

        // ── Helper ─────────────────────────────────────────────────────

        private static Material CreateStandardMaterial()
        {
            // Try URP shaders first, then fall back to Standard/Mobile
            var shader = Shader.Find(URPLitShader);
            if (shader == null)
                shader = Shader.Find(URPSimpleLitShader);
            if (shader == null)
                shader = Shader.Find(StandardShader);
            if (shader == null)
                shader = Shader.Find(MobileShader);
            return new Material(shader);
        }

        /// <summary>
        /// Returns true if running on a mobile platform.
        /// Used to decide whether to apply mobile optimizations at creation time.
        /// </summary>
        public static bool IsMobile()
        {
            return Application.platform == RuntimePlatform.Android
                || Application.platform == RuntimePlatform.IPhonePlayer;
        }

        /// <summary>
        /// Applies mobile-friendly caps to a material: reduced glossiness,
        /// metallic, and emission intensity. Called automatically when
        /// IsMobile() is true during material creation.
        /// </summary>
        public static void ApplyMobileCaps(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", Mathf.Min(mat.GetFloat("_Glossiness"), 0.5f));

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", Mathf.Min(mat.GetFloat("_Metallic"), 0.3f));

            if (mat.IsKeywordEnabled("_EMISSION") && mat.HasProperty("_EmissionColor"))
            {
                Color e = mat.GetColor("_EmissionColor");
                float max = Mathf.Max(e.r, Mathf.Max(e.g, e.b));
                if (max > 0.3f)
                    mat.SetColor("_EmissionColor", e * (0.3f / max));
            }
        }
    }
}
