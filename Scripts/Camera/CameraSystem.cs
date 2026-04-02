using System.Collections;
using UnityEngine;
using WizardChess.Interfaces;

namespace WizardChess.CameraModule
{
    /// <summary>
    /// Manages gameplay and battle camera views with smooth transitions and camera shake.
    /// Gameplay view: angled top-down for mobile readability.
    /// Battle view: close-up on combatants during captures.
    /// </summary>
    public class CameraSystem : MonoBehaviour, ICameraSystem
    {
        [Header("Gameplay View")]
        [SerializeField] private Vector3 gameplayPosition = new Vector3(4f, 10f, -2f);
        [SerializeField] private Vector3 gameplayRotation = new Vector3(65f, 0f, 0f);

        [Header("Battle View")]
        [SerializeField] private float battleCameraDistance = 3f;
        [SerializeField] private float battleCameraHeight = 2f;
        [SerializeField] private float battleCameraAngle = 30f;

        [Header("Transitions")]
        [SerializeField] private float transitionDuration = 0.6f;

        [Header("Camera Shake")]
        [SerializeField] private float defaultShakeIntensity = 0.15f;
        [SerializeField] private float defaultShakeDuration = 0.25f;

        private UnityEngine.Camera _camera;
        private Vector3 _shakeOffset;
        private Coroutine _shakeCoroutine;
        private bool _isTransitioning;

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (_camera == null)
                _camera = UnityEngine.Camera.main;
        }

        private void Start()
        {
            SetGameplayView();
        }

        /// <summary>
        /// Sets the camera to the gameplay view: angled top-down, readable on mobile.
        /// </summary>
        public void SetGameplayView()
        {
            StopShake();
            transform.position = gameplayPosition;
            transform.eulerAngles = gameplayRotation;
            _shakeOffset = Vector3.zero;
        }

        /// <summary>
        /// Smoothly transitions the camera to a close-up battle view between attacker and defender.
        /// Camera positions itself to the side, looking at the midpoint of the two combatants.
        /// </summary>
        public IEnumerator TransitionToBattleView(Vector3 attackerPos, Vector3 defenderPos)
        {
            _isTransitioning = true;
            StopShake();

            Vector3 midpoint = (attackerPos + defenderPos) * 0.5f;
            Vector3 direction = (defenderPos - attackerPos).normalized;

            // Camera offset perpendicular to the battle axis, elevated
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
            if (perpendicular.sqrMagnitude < 0.01f)
                perpendicular = Vector3.right;

            Vector3 targetPos = midpoint + perpendicular * battleCameraDistance
                                         + Vector3.up * battleCameraHeight;

            Quaternion targetRot = Quaternion.LookRotation(midpoint - targetPos + Vector3.down * 0.3f);

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = targetRot;
            _isTransitioning = false;
        }

        /// <summary>
        /// Smoothly returns the camera to the gameplay view after a battle.
        /// </summary>
        public IEnumerator ReturnToGameplayView()
        {
            _isTransitioning = true;
            StopShake();

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = Quaternion.Euler(gameplayRotation);

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                transform.position = Vector3.Lerp(startPos, gameplayPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            transform.position = gameplayPosition;
            transform.eulerAngles = gameplayRotation;
            _shakeOffset = Vector3.zero;
            _isTransitioning = false;
        }

        /// <summary>
        /// Applies a camera shake effect (used during battle impacts).
        /// </summary>
        public void ApplyCameraShake(float intensity, float duration)
        {
            if (intensity <= 0f || duration <= 0f)
                return;

            StopShake();
            _shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        /// <summary>
        /// Convenience overload using default shake parameters.
        /// </summary>
        public void ApplyDefaultCameraShake()
        {
            ApplyCameraShake(defaultShakeIntensity, defaultShakeDuration);
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            Vector3 basePosition = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - Mathf.Clamp01(elapsed / duration);
                float currentIntensity = intensity * decay;

                _shakeOffset = new Vector3(
                    Random.Range(-currentIntensity, currentIntensity),
                    Random.Range(-currentIntensity, currentIntensity),
                    Random.Range(-currentIntensity, currentIntensity)
                );

                transform.position = basePosition + _shakeOffset;
                yield return null;
            }

            // Restore exact position
            transform.position = basePosition;
            _shakeOffset = Vector3.zero;
            _shakeCoroutine = null;
        }

        private void StopShake()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;

                // Remove any residual shake offset
                transform.position -= _shakeOffset;
                _shakeOffset = Vector3.zero;
            }
        }
    }
}
