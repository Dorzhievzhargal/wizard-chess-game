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
        [SerializeField] private Color ambientColor = new Color(0.08f, 0.06f, 0.12f);
        [SerializeField] private float ambientIntensity = 0.4f;

        [Header("Directional Light")]
        [SerializeField] private Color mainLightColor = new Color(0.6f, 0.55f, 0.7f);
        [SerializeField] private float mainLightIntensity = 0.6f;
        [SerializeField] private Vector3 mainLightRotation = new Vector3(50f, -30f, 0f);

        [Header("Fill Light (soft glow from below)")]
        [SerializeField] private Color fillLightColor = new Color(0.15f, 0.1f, 0.25f);
        [SerializeField] private float fillLightIntensity = 0.3f;

        [Header("Fog")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private Color fogColor = new Color(0.05f, 0.03f, 0.08f);
        [SerializeField] private float fogDensity = 0.04f;

        [Header("Skybox / Background")]
        [SerializeField] private Color backgroundColor = new Color(0.02f, 0.01f, 0.04f);

        [Header("Floor")]
        [SerializeField] private bool createFloor = true;
        [SerializeField] private float floorSize = 30f;
        [SerializeField] private Color floorColor = new Color(0.06f, 0.04f, 0.08f);

        private GameObject _mainLight;
        private GameObject _fillLight;
        private GameObject _floor;

        private void Start()
        {
            SetupAmbientLighting();
            SetupFog();
            SetupSkybox();
            CreateMainLight();
            CreateFillLight();

            if (createFloor)
                CreateFloorPlane();
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
                var mat = new Material(Shader.Find("Standard"));
                mat.name = "FloorMaterial";
                mat.color = floorColor;
                mat.SetFloat("_Glossiness", 0.3f);
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
        }
    }
}
