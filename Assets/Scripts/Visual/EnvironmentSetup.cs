using UnityEngine;

namespace WizardChess.Visual
{
    /// <summary>
    /// Configures the dark magical hall environment at runtime:
    /// atmospheric lighting, fog, ambient glow, and a floor plane.
    /// Attach to any GameObject in the scene (e.g. GameManager).
    /// </summary>
    public class EnvironmentSetup : MonoBehaviour
    {
        [Header("Ambient Lighting")]
        [SerializeField] private Color ambientColor = new Color(0.4f, 0.4f, 0.45f);
        [SerializeField] private float ambientIntensity = 0.8f;

        [Header("Directional Light")]
        [SerializeField] private Color mainLightColor = new Color(1f, 0.95f, 0.9f);
        [SerializeField] private float mainLightIntensity = 1.2f;
        [SerializeField] private Vector3 mainLightRotation = new Vector3(50f, -30f, 0f);

        [Header("Fill Light (soft glow from below)")]
        [SerializeField] private Color fillLightColor = new Color(0.5f, 0.5f, 0.6f);
        [SerializeField] private float fillLightIntensity = 1.0f;

        [Header("Fog")]
        [SerializeField] private bool enableFog = false;
        [SerializeField] private Color fogColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private float fogDensity = 0.01f;

        [Header("Skybox / Background")]
        [SerializeField] private Color backgroundColor = new Color(0.25f, 0.25f, 0.3f);

        [Header("Floor")]
        [SerializeField] private bool createFloor = true;
        [SerializeField] private float floorSize = 30f;
        [SerializeField] private Color floorColor = new Color(0.3f, 0.28f, 0.32f);

        [Header("Table")]
        [SerializeField] private bool createTable = true;
        [SerializeField] private Color tableTopColor = new Color(0.45f, 0.28f, 0.15f);
        [SerializeField] private Color tableLegColor = new Color(0.35f, 0.2f, 0.1f);

        private GameObject _mainLight;
        private GameObject _fillLight;
        private GameObject _floor;
        private GameObject _table;

        private void Start()
        {
            SetupAmbientLighting();
            SetupFog();
            SetupSkybox();
            CreateMainLight();
            CreateFillLight();

            if (createFloor)
                CreateFloorPlane();

            if (createTable)
                CreateTable();
        }

        /// <summary>
        /// Configures ambient lighting for a dark magical atmosphere.
        /// </summary>
        private void SetupAmbientLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor * ambientIntensity;
        }

        /// <summary>
        /// Enables exponential fog for depth and atmosphere.
        /// </summary>
        private void SetupFog()
        {
            RenderSettings.fog = enableFog;
            if (enableFog)
            {
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
            }
        }

        /// <summary>
        /// Sets the camera background to a near-black dark purple.
        /// Clears the default skybox for a closed-hall feel.
        /// </summary>
        private void SetupSkybox()
        {
            RenderSettings.skybox = null;

            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = backgroundColor;
            }
        }

        /// <summary>
        /// Creates the main directional light — cool-toned, angled from above.
        /// Simulates moonlight or magical ambient light filtering into the hall.
        /// </summary>
        private void CreateMainLight()
        {
            _mainLight = new GameObject("MainLight_MagicHall");
            _mainLight.transform.SetParent(transform);
            _mainLight.transform.eulerAngles = mainLightRotation;

            var light = _mainLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = mainLightColor;
            light.intensity = mainLightIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
        }

        /// <summary>
        /// Creates a subtle upward fill light to softly illuminate piece undersides
        /// and add a magical glow from the floor.
        /// </summary>
        private void CreateFillLight()
        {
            _fillLight = new GameObject("FillLight_MagicGlow");
            _fillLight.transform.SetParent(transform);
            _fillLight.transform.position = new Vector3(4f, -2f, 4f);
            _fillLight.transform.eulerAngles = new Vector3(-90f, 0f, 0f);

            var light = _fillLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = fillLightColor;
            light.intensity = fillLightIntensity;
            light.shadows = LightShadows.None;
        }

        /// <summary>
        /// Creates a large dark floor plane beneath the board to ground the scene.
        /// </summary>
        private void CreateFloorPlane()
        {
            _floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _floor.name = "Floor_MagicHall";
            _floor.transform.SetParent(transform);
            _floor.transform.position = new Vector3(4f, -0.01f, 4f);
            _floor.transform.localScale = Vector3.one * (floorSize / 10f);

            var renderer = _floor.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                var mat = new Material(shader);
                mat.name = "FloorMaterial";
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", floorColor);
                else
                    mat.color = floorColor;
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", 0.3f);
                if (mat.HasProperty("_Metallic"))
                    mat.SetFloat("_Metallic", 0.0f);
                renderer.material = mat;
            }

            // Disable collider so it doesn't interfere with board raycasting
            var collider = _floor.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }

        private void OnDestroy()
        {
            if (_mainLight != null) Destroy(_mainLight);
            if (_fillLight != null) Destroy(_fillLight);
            if (_floor != null) Destroy(_floor);
            if (_table != null) Destroy(_table);
        }

        private Material CreateURPMaterial(Color color, float smoothness = 0.5f, float metallic = 0f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);
            return mat;
        }

        private void CreateTable()
        {
            _table = new GameObject("Table");
            _table.transform.SetParent(transform);

            float boardCenter = 3.5f;
            float tableTopY = -0.15f;
            float tableWidth = 11f;
            float tableDepth = 11f;
            float tableTopThickness = 0.5f;
            float legHeight = 1.5f;
            float legSize = 0.5f;

            // Table top
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "TableTop";
            top.transform.SetParent(_table.transform);
            top.transform.position = new Vector3(boardCenter, tableTopY - tableTopThickness / 2f, boardCenter);
            top.transform.localScale = new Vector3(tableWidth, tableTopThickness, tableDepth);
            top.GetComponent<MeshRenderer>().material = CreateURPMaterial(tableTopColor, 0.4f, 0.05f);
            var topCol = top.GetComponent<Collider>();
            if (topCol != null) topCol.enabled = false;

            // 4 legs
            float inset = 0.8f;
            float legTopY = tableTopY - tableTopThickness;
            Vector3[] legPositions = {
                new Vector3(boardCenter - tableWidth / 2f + inset, legTopY - legHeight / 2f, boardCenter - tableDepth / 2f + inset),
                new Vector3(boardCenter + tableWidth / 2f - inset, legTopY - legHeight / 2f, boardCenter - tableDepth / 2f + inset),
                new Vector3(boardCenter - tableWidth / 2f + inset, legTopY - legHeight / 2f, boardCenter + tableDepth / 2f - inset),
                new Vector3(boardCenter + tableWidth / 2f - inset, legTopY - legHeight / 2f, boardCenter + tableDepth / 2f - inset),
            };

            var legMat = CreateURPMaterial(tableLegColor, 0.3f, 0.05f);
            for (int i = 0; i < 4; i++)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"TableLeg_{i}";
                leg.transform.SetParent(_table.transform);
                leg.transform.position = legPositions[i];
                leg.transform.localScale = new Vector3(legSize, legHeight, legSize);
                leg.GetComponent<MeshRenderer>().material = legMat;
                var legCol = leg.GetComponent<Collider>();
                if (legCol != null) legCol.enabled = false;
            }
        }
    }
}
