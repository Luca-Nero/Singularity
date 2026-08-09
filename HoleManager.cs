using MelonLoader;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

namespace Singularity
{
    internal class GravityWell
    {
        public Vector3 Position;
        public float Lifetime;         
        public float MaxLifetime;   
        public GameObject Visuals;     
        public bool Dead;
        public int AffectedCount;      

        public readonly bool Rotating;

        public readonly Quaternion DiskTilt;
        public readonly Vector3 SpinAxis;

        public GravityWell(Vector3 pos, float maxLifetime, bool rotating, Quaternion diskTilt)
        {
            Position = pos;
            MaxLifetime = maxLifetime;
            Lifetime = maxLifetime;
            Rotating = rotating;
            DiskTilt = diskTilt;
            SpinAxis = diskTilt * Vector3.up;
            Dead = false;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // HoleManager — orchestrates singularity deployment and physics
    // ══════════════════════════════════════════════════════════════════════════════
    internal static class HoleManager
    {
        private static readonly List<GravityWell> _wells = new List<GravityWell>();

        private static Rigidbody _playerRb;
        private static int _pollPlayerCountdown;

        private static readonly List<Rigidbody> _cache = new List<Rigidbody>();

        // ── Deployment ──────────────────────────────────────────────────────────

        public static void TryDeploy()
        {
            var cam = Camera.main;
            if (cam == null) return;

            Ray aimRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            Vector3 placePos;
            bool rayHit = false;
            if (Physics.Raycast(aimRay, out hit, Config.SpawnRange))
            {
                placePos = hit.point + Vector3.up * 1f;
                rayHit = true;
            }
            else
            {
                placePos = aimRay.origin + aimRay.direction * (Config.SpawnRange * 0.5f);
            }

            if (Config.Dbg1)
            {
                MelonLogger.Msg($"[Singularity][LOG] Camera pos={cam.transform.position:F1} dir={aimRay.direction:F1}");
                MelonLogger.Msg($"[Singularity][LOG] Raycast {(rayHit ? "HIT at " + hit.point.ToString("F1") : "MISS (fallback)")} → placePos={placePos:F1}");
            }

            var well = new GravityWell(placePos, Config.Lifetime, Config.SpawnRotating,
                HoleVFX.RandomDiskTilt());
            _wells.Add(well);

            // Create VFX for this well
            well.Visuals = HoleVFX.Create(well);

            if (Config.Dbg1)
            {
                if (well.Visuals != null)
                    MelonLogger.Msg($"[Singularity][LOG] VFX root pos after Create={well.Visuals.transform.position:F1}");
                else
                    MelonLogger.Msg($"[Singularity][LOG] VFX root is NULL!");
            }
        }

        // ── Main update loops ───────────────────────────────────────────────────

        public static void Update(float dt)
        {
            for (int i = _wells.Count - 1; i >= 0; i--)
            {
                if (_wells[i].Dead)
                {
                    _wells.RemoveAt(i);
                }
            }

            foreach (var w in _wells)
            {
                if (w.Dead) continue;

                if (w.Visuals == null)
                {
                    HoleVFX.Forget(w);
                    w.Dead = true;
                    continue;
                }

                w.Lifetime -= dt;
                if (w.Lifetime <= 0f)
                {
                    Collapse(w);
                    continue;
                }

                HoleVFX.Update(w);
            }
        }

        public static void FixedUpdate(float fixedDt)
        {
            foreach (var w in _wells)
            {
                if (w.Dead) continue;
                if (w.Visuals == null) continue;   // retired next Update, don't pull meanwhile
                ApplyForces(w, fixedDt);
            }
        }

        public static void ClearAll()
        {
            foreach (var w in _wells)
            {
                if (w.Visuals != null) HoleVFX.Destroy(w.Visuals);
                else HoleVFX.Forget(w);
                w.Visuals = null;
                w.Dead = true;
            }
            _wells.Clear();

            HoleVFX.ResetForNewScene();

            _playerRb = null;
            _pollPlayerCountdown = 0;
        }

        // ── Physics ─────────────────────────────────────────────────────────────

        private static void ApplyForces(GravityWell well, float dt)
        {
            _cache.Clear();

            var colliders = Physics.OverlapSphere(well.Position, Config.PullRadius);
            foreach (var col in colliders)
            {
                var rb = col.attachedRigidbody;
                if (rb == null) continue;

                if (rb.isKinematic) continue;
                if (rb.mass > Config.MassLimit) continue;

                // Skip player
                if (TryGetPlayerRb() && rb == _playerRb) continue;
                if (IsPlayerChild(rb)) continue;

                _cache.Add(rb);
            }

            well.AffectedCount = _cache.Count;

            float radiusSq = Config.PullRadius * Config.PullRadius;

            foreach (var rb in _cache)
            {
                var toWell = well.Position - rb.position;
                float distSq = toWell.sqrMagnitude;

                if (distSq > radiusSq) continue;

                float dist = Mathf.Sqrt(distSq);
                if (dist < 0.01f) continue;

                Vector3 dir = toWell / dist; // normalized direction toward the well

                float edgeFactor = 1f - (dist / Config.PullRadius);   // 1 at center, 0 at edge
                float falloff = Mathf.Pow(edgeFactor, Mathf.Max(0.01f, Config.PullFalloff));

                float radialForce = Config.PullForce * falloff;
                Vector3 radialVec = dir * radialForce;

                Vector3 right = Vector3.Cross(well.SpinAxis, dir);
                if (right.sqrMagnitude < 1e-4f)
                {
                    right = Vector3.Cross(dir, Vector3.up);
                    if (right.sqrMagnitude < 1e-4f) right = Vector3.Cross(dir, Vector3.right);
                }
                right.Normalize();

                float spinMag = well.Rotating ? Config.SpinForce * falloff : 0f;
                Vector3 spinVec = right * spinMag;

                Vector3 force = radialVec + spinVec;

                force.y += Config.PullUpward * radialForce * 0.3f;

                float massFactor = Mathf.Clamp(rb.mass / 10f, 0.2f, 1f);
                force /= massFactor;

                rb.AddForce(force, ForceMode.Force);
                float accretionRadius = Mathf.Max(0.05f,
                    Config.AccretionThreshold * HoleVFX.VisibleDiskRadius);
                if (dist < accretionRadius)
                {
                    Vector3 crush = dir * Config.AccretionForce * (1f - dist / accretionRadius);
                    rb.AddForce(crush, ForceMode.Impulse);

                    Vector3 tear = well.SpinAxis * (Mathf.Sin(Time.time * 10f) * 2f);
                    tear += right * (Mathf.Cos(Time.time * 7f) * 2f);
                    rb.AddForce(tear * Config.AccretionForce * 0.5f, ForceMode.Force);

                    Vector3 torque = Vector3.Cross(dir, well.SpinAxis) * Config.AccretionForce * 3f;
                    rb.AddTorque(torque, ForceMode.Force);
                }
            }
        }

        // ── Collapse animation / cleanup ────────────────────────────────────────

        private static void Collapse(GravityWell well)
        {
            well.Dead = true;
            HoleVFX.Collapse(well);

            if (Config.Dbg1) MelonLogger.Msg($"[Singularity] Collapsed at {well.Position:F1}");
        }

        // ── Player detection ────────────────────────────────────────────────────

        private static bool TryGetPlayerRb()
        {
            if (_playerRb != null) return true;
            if (--_pollPlayerCountdown > 0) return false;
            _pollPlayerCountdown = 120;

            var pelvis = Object.FindObjectOfType<Il2CppActiveRagdoll.Scripts.Pelvis>(true);
            if (pelvis != null)
                _playerRb = pelvis.m_rb;

            return _playerRb != null;
        }

        private static bool IsPlayerChild(Rigidbody rb)
        {
            if (_playerRb == null) return false;
            var t = rb.transform;
            while (t != null)
            {
                if (t == _playerRb.transform) return true;
                t = t.parent;
            }
            return false;
        }

        public static int ActiveCount => _wells.Count;
        public static int AffectedRbs()
        {
            int total = 0;
            foreach (var w in _wells) total += w.AffectedCount;
            return total;
        }
    }
}
