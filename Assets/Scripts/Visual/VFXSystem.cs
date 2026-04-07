using System.Collections;
using UnityEngine;

namespace WizardChess.Visual
{
    /// <summary>
    /// Types of death VFX mapped to piece death effects.
    /// </summary>
    public enum DeathVFXType
    {
        StoneFragments,   // Rook, Pawn, Knight
        MagicSparkles,    // Bishop
        CombinedDebris,   // Queen
        KingShockwave     // King
    }

    /// <summary>
    /// Creates and manages runtime particle effects for battle sequences.
    /// All ParticleSystem components are created programmatically — no prefabs.
    /// </summary>
    public class VFXSystem : MonoBehaviour
    {
        // ── Impact Burst ───────────────────────────────────────────────

        /// <summary>
        /// Spawns a burst of particles at the given position (e.g. on hit impact).
        /// Desktop: 12 particles. Mobile: 6.
        /// </summary>
        public void SpawnImpactBurst(Vector3 position, Color color, int particleCount = 12)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
                return;

            bool mobile = MaterialFactory.IsMobile();
            int count = mobile ? particleCount / 2 : particleCount;

            var go = new GameObject("VFX_ImpactBurst");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            if (ps == null) { Debug.LogWarning("VFXSystem: Failed to add ParticleSystem for ImpactBurst"); return; }

            // Stop default playback so we can configure first
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startColor = color;
            main.maxParticles = count;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Emission: single burst
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            // Shape: sphere for omnidirectional burst
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            // Collision (desktop only)
            var collision = ps.collision;
            collision.enabled = !mobile;

            // Renderer — use default particle material
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.material = CreateParticleMaterial(color);

            ps.Play();

            float lifetime = 0.8f + 0.1f; // max particle lifetime + buffer
            StartCoroutine(DestroyAfterLifetime(go, lifetime));
        }

        // ── Magic Projectile ───────────────────────────────────────────

        /// <summary>
        /// Spawns a magic projectile trail that moves from attacker to defender.
        /// Desktop: 20 trail particles. Mobile: 10.
        /// Returns the projectile GameObject.
        /// </summary>
        public GameObject SpawnMagicProjectile(Vector3 from, Vector3 to, float duration, Color color)
        {
            if (duration <= 0f) return null;

            bool mobile = MaterialFactory.IsMobile();
            int trailCount = mobile ? 10 : 20;

            var go = new GameObject("VFX_MagicProjectile");
            go.transform.position = from;

            var ps = go.AddComponent<ParticleSystem>();
            if (ps == null) { Debug.LogWarning("VFXSystem: Failed to add ParticleSystem for MagicProjectile"); return null; }

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.startLifetime = duration;
            main.startSize = 0.08f;
            main.startSpeed = 0f; // particles stay relative to emitter
            main.startColor = color;
            main.maxParticles = trailCount;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Emission: steady stream over the travel duration
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = trailCount / duration;

            // Shape: point emitter
            var shape = ps.shape;
            shape.enabled = false;

            // Collision disabled on mobile
            var collision = ps.collision;
            collision.enabled = !mobile;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.material = CreateParticleMaterial(color);

            ps.Play();

            // Move the emitter from → to over duration
            StartCoroutine(MoveProjectile(go, from, to, duration));
            StartCoroutine(DestroyAfterLifetime(go, duration + 0.5f));

            return go;
        }

        // ── Death Debris ───────────────────────────────────────────────

        /// <summary>
        /// Spawns debris particles appropriate to the death type.
        /// </summary>
        public void SpawnDeathDebris(Vector3 position, DeathVFXType type, Color color)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
                return;

            switch (type)
            {
                case DeathVFXType.StoneFragments:
                    SpawnStoneFragments(position, color);
                    break;
                case DeathVFXType.MagicSparkles:
                    SpawnMagicSparkles(position, color);
                    break;
                case DeathVFXType.CombinedDebris:
                    SpawnStoneFragments(position, color);
                    SpawnMagicSparkles(position, color);
                    break;
                case DeathVFXType.KingShockwave:
                    SpawnStoneFragments(position, color);
                    SpawnShockwave(position);
                    break;
            }
        }

        // ── Shockwave ──────────────────────────────────────────────────

        /// <summary>
        /// Spawns an expanding ring shockwave for King death.
        /// </summary>
        public void SpawnShockwave(Vector3 position, float radius = 2f, float duration = 0.5f)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
                return;

            var go = new GameObject("VFX_Shockwave");
            go.transform.position = position;

            // Create a ring using a torus-like approach with a flattened cylinder
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ShockwaveRing";
            ring.transform.SetParent(go.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(0f, 0.02f, 0f); // start at zero radius

            // Remove collider from the ring primitive
            var col = ring.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // Semi-transparent material
            var mat = CreateParticleMaterial(new Color(1f, 1f, 1f, 0.5f));
            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = mat;

            // Also add a ParticleSystem to the root so it qualifies as a VFX object
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = duration;
            main.maxParticles = 1;
            main.loop = false;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            ps.Play();

            StartCoroutine(ExpandShockwave(ring.transform, radius, duration));
            StartCoroutine(DestroyAfterLifetime(go, duration + 0.1f));
        }

        // ── Private: Stone Fragments ───────────────────────────────────

        private void SpawnStoneFragments(Vector3 position, Color color)
        {
            bool mobile = MaterialFactory.IsMobile();
            int count = mobile ? 8 : 15;

            var go = new GameObject("VFX_StoneFragments");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            if (ps == null) return;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
            main.startColor = color;
            main.maxParticles = count;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var collision = ps.collision;
            collision.enabled = !mobile;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.material = CreateParticleMaterial(color);

            ps.Play();

            StartCoroutine(DestroyAfterLifetime(go, 1.0f + 0.1f));
        }

        // ── Private: Magic Sparkles ────────────────────────────────────

        private void SpawnMagicSparkles(Vector3 position, Color color)
        {
            bool mobile = MaterialFactory.IsMobile();
            int count = mobile ? 10 : 20;

            var go = new GameObject("VFX_MagicSparkles");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            if (ps == null) return;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
            main.startColor = color;
            main.maxParticles = count;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var collision = ps.collision;
            collision.enabled = !mobile;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.material = CreateParticleMaterial(color);

            ps.Play();

            StartCoroutine(DestroyAfterLifetime(go, 0.8f + 0.1f));
        }

        // ── Coroutines ─────────────────────────────────────────────────

        /// <summary>
        /// Destroys a VFX GameObject after the specified lifetime.
        /// </summary>
        private IEnumerator DestroyAfterLifetime(GameObject vfxObj, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            if (vfxObj != null)
                Destroy(vfxObj);
        }

        /// <summary>
        /// Moves the projectile emitter from start to end over duration.
        /// </summary>
        private IEnumerator MoveProjectile(GameObject projectile, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (projectile == null) yield break;
                float t = elapsed / duration;
                projectile.transform.position = Vector3.Lerp(from, to, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (projectile != null)
                projectile.transform.position = to;
        }

        /// <summary>
        /// Expands the shockwave ring from zero to target radius.
        /// </summary>
        private IEnumerator ExpandShockwave(Transform ring, float radius, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (ring == null) yield break;
                float t = elapsed / duration;
                float currentRadius = Mathf.Lerp(0f, radius, t);
                ring.localScale = new Vector3(currentRadius, 0.02f, currentRadius);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (ring != null)
                ring.localScale = new Vector3(radius, 0.02f, radius);
        }

        // ── Helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Creates a simple unlit particle material with the given color.
        /// </summary>
        private static Material CreateParticleMaterial(Color color)
        {
            // Try particle shaders, fall back to standard
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Mobile/Particles/Additive");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = "VFXParticleMat";

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;

            return mat;
        }
    }
}
