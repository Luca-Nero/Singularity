using Il2CppInterop.Runtime;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Color = UnityEngine.Color;

namespace Singularity
{
    internal static class HoleVFX
    {
        private const int QDarken = 2990;
        private const int QGlow   = 3000;
        private const int QMotes  = 3005;
        private const int QDisk   = 3010;
        private const int QSwirl  = 3020;
        private const int QArcs   = 3030;
        private const int QPhoton = 3040;
        private static Shader _unlitShader;

        private static Shader UnlitShader
        {
            get
            {
                if (_unlitShader != null) return _unlitShader;
                _unlitShader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                return _unlitShader;
            }
        }
        private static Shader _opaqueShader;

        private static Shader OpaqueShader
        {
            get
            {
                if (_opaqueShader != null) return _opaqueShader;
                _opaqueShader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                return _opaqueShader;
            }
        }

        // ── VFX state dictionary ────────────────────────────────────────────────

        private static readonly Dictionary<GameObject, SingularityInfo> _vfxInfos =
            new Dictionary<GameObject, SingularityInfo>();

        // ── Cached main camera (for billboarding) ──────────────────────────────

        private static Camera _cachedCam;
        private static int _camPollCountdown;

        private static Camera GetCamera()
        {
            if (_cachedCam != null) return _cachedCam;
            if (--_camPollCountdown > 0) return null;
            _camPollCountdown = 30;
            _cachedCam = Camera.main;
            return _cachedCam;
        }

        private static readonly Dictionary<string, Mesh> _ringMeshes = new Dictionary<string, Mesh>();

        private static Mesh GetRingMesh(string key, float innerRatio, int radialSegs, int ringSegs,
            float radialPow, System.Func<float, float, Color> colorFn, float bulgeAmplitude = 0f)
        {
            if (_ringMeshes.TryGetValue(key, out var cached) && cached != null)
                return cached;

            int vertsPerRing = ringSegs + 1;
            int totalVerts = vertsPerRing * radialSegs;

            var vertices = new List<Vector3>(totalVerts);
            var normals = new List<Vector3>(totalVerts);
            var uvs = new List<Vector2>(totalVerts);
            var colors = new List<Color>(totalVerts);
            var indices = new List<int>(ringSegs * (radialSegs - 1) * 6);

            int vertIdx = 0;
            for (int i = 0; i < radialSegs; i++)
            {
                float t = radialSegs == 1 ? 0f : (float)i / (radialSegs - 1);
                float r = Mathf.Lerp(innerRatio, 1f, Mathf.Pow(t, radialPow));


                float bulge = bulgeAmplitude * Mathf.Sin(Mathf.PI * t);

                for (int j = 0; j <= ringSegs; j++)
                {
                    float angleDeg = j * 360f / ringSegs;
                    float angleRad = angleDeg * Mathf.Deg2Rad;
                    float cosA = Mathf.Cos(angleRad);
                    float sinA = Mathf.Sin(angleRad);

                    vertices.Add(new Vector3(cosA * r, bulge, sinA * r));
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(r, angleDeg / 360f));
                    colors.Add(colorFn(r, angleDeg));


                    if (i < radialSegs - 1 && j < ringSegs)
                    {
                        int a = vertIdx;
                        int b = vertIdx + 1;
                        int c = vertIdx + vertsPerRing;
                        int d = vertIdx + vertsPerRing + 1;

                        indices.Add(a); indices.Add(c); indices.Add(b);
                        indices.Add(b); indices.Add(c); indices.Add(d);
                    }
                    vertIdx++;
                }
            }

            var mesh = new Mesh();
            mesh.name = key;
            mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices.ToArray());
            mesh.SetNormals(normals.ToArray());
            mesh.SetUVs(0, uvs.ToArray());
            mesh.SetColors(colors.ToArray());
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();

            _ringMeshes[key] = mesh;
            return mesh;
        }

        // ── Colour / profile helpers ────────────────────────────────────────────

        private static float Gauss(float x, float sigma)
        {
            float k = x / sigma;
            return Mathf.Exp(-0.5f * k * k);
        }

        private static Color DiskRamp(float u)
        {
            u = Mathf.Clamp01(u);
            Color c;
            if (u < 0.14f)
                c = Color.Lerp(new Color(1.00f, 0.99f, 0.97f), new Color(1.00f, 0.94f, 0.78f), u / 0.14f);
            else if (u < 0.36f)
                c = Color.Lerp(new Color(1.00f, 0.94f, 0.78f), new Color(1.00f, 0.78f, 0.42f), (u - 0.14f) / 0.22f);
            else if (u < 0.66f)
                c = Color.Lerp(new Color(1.00f, 0.78f, 0.42f), new Color(0.94f, 0.50f, 0.14f), (u - 0.36f) / 0.30f);
            else
                c = Color.Lerp(new Color(0.94f, 0.50f, 0.14f), new Color(0.42f, 0.15f, 0.02f), (u - 0.66f) / 0.34f);

            c.a = Mathf.Pow(1f - u, 2.3f);
            return c;
        }

        // ── Layer geometry constants (all in units of the shadow radius, eh) ────

        private const float PhotonOuter    = 1.35f; 
        private const float PhotonInner    = 0.70f;  
        private const float PhotonPeak     = 1.012f; 
        private const float PhotonSigma    = 0.030f;

        private const float ArcOuter       = 2.70f;
        private const float ArcInner       = 0.33f;

        private const float GlowOuter      = 3.40f;
        private const float GlowInner      = 0.26f;

        private const float DarkenOuterMin = 1.50f;
        private static float DiskInnerEdge => Mathf.Clamp(Config.DiskInnerScale, 1.02f, 3.5f);
        public static float ShadowRadius => Config.CoreScale * 0.6f;
        public static float VisibleDiskRadius =>
            ShadowRadius * Mathf.Max(DiskInnerEdge + 0.5f, Config.DiskOuterScale);

        // ── Per-vertex colour functions ─────────────────────────────────────────
        private static Color PhotonRingColor(float rNorm, float angleDeg)
        {
            float rr = rNorm * PhotonOuter;
            float core = Gauss(rr - PhotonPeak, PhotonSigma);
            float skirt = Gauss(rr - (PhotonPeak + 0.01f), 0.13f) * 0.20f;

            var c = new Color(1.00f, 0.985f, 0.955f);
            c.a = Mathf.Clamp01(core + skirt);
            return c;
        }

        private static Color ArcColor(float rNorm, float angleDeg)
        {
            float rr = rNorm * ArcOuter;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float band = Gauss(rr - 1.02f, 0.52f);

            float s = Mathf.Sin(angleRad);
            float vert = Mathf.Pow(Mathf.Abs(s), 0.55f);

            float bias = s >= 0f ? 1.00f : 0.86f;

            var c = DiskRamp(Mathf.Clamp01((rr - 1.0f) / 1.9f));
            c.a = Mathf.Clamp01(c.a * band * vert * bias);
            return c;
        }

        private static Color GlowColor(float rNorm, float angleDeg)
        {
            float rr = rNorm * GlowOuter;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(Gauss(rr - 1.05f, 0.62f)));
        }

        private static System.Func<float, float, Color> MakeDarkenColorFn(float innerRatio)
        {
            return (rNorm, angleDeg) =>
            {
                float u = Mathf.Clamp01((rNorm - innerRatio) / (1f - innerRatio));

                return new Color(0f, 0f, 0f, Mathf.SmoothStep(1f, 0f, u));
            };
        }


        private static System.Func<float, float, Color> MakeDiskColorFn(float innerRatio)
        {
            return (rNorm, angleDeg) =>
            {
                float u = Mathf.Clamp01((rNorm - innerRatio) / (1f - innerRatio));
                var c = DiskRamp(u);


                float approach = 0.5f + 0.5f * Mathf.Cos(angleDeg * Mathf.Deg2Rad);
                float d = Mathf.Clamp01(Config.DopplerStrength);
                float beam = Mathf.Lerp(1f, Mathf.Lerp(0.22f, 1f, Mathf.Pow(approach, 0.7f)), d);

                c.a = Mathf.Clamp01(c.a * beam);
                c.r = Mathf.Clamp01(c.r + (1f - approach) * 0.06f * d);
                c.b = Mathf.Clamp01(c.b + approach * 0.14f * d);
                return c;
            };
        }


        private static System.Func<float, float, Color> MakeSwirlColorFn(float innerRatio)
        {
            return (rNorm, angleDeg) =>
            {
                float u = Mathf.Clamp01((rNorm - innerRatio) / (1f - innerRatio));
                float a = angleDeg * Mathf.Deg2Rad;

                float arms = Mathf.Sin(a * 2f - u * 9f);
                float fine = Mathf.Sin(a * 9f - u * 26f + 2.3f);
                float m = Mathf.Clamp01(0.5f + 0.35f * arms + 0.22f * fine);

                var c = DiskRamp(u * 0.85f);
                c.a = Mathf.Clamp01(c.a * m);
                return c;
            };
        }

        private struct Mote
        {
            public float R;      // current orbital radius
            public float Theta;  // azimuth, radians
            public float Y0;     // height at the spawn radius; scales down as R shrinks
            public float Spin;   // per-mote angular rate jitter
            public float Size;   // per-mote size jitter
            public bool Alive;   // false once it has arrived and respawning has stopped
        }

        private static readonly System.Random _rng = new System.Random(0x5E1F);

        private static float Rand(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        private static void RespawnMote(ref Mote m, float outerR, bool initial)
        {
            m.R = initial ? Rand(outerR * 0.15f, outerR) : Rand(outerR * 0.96f, outerR);
            m.Theta = Rand(0f, Mathf.PI * 2f);
            m.Y0 = Rand(-1f, 1f) * outerR * 0.35f;
            m.Spin = Rand(0.75f, 1.3f);
            m.Size = Rand(0.65f, 1.4f);
            m.Alive = true;
        }

        private static Color MoteColor(float p, bool rotating)
        {
            if (!rotating)
            {
                var cold = Color.Lerp(new Color(0.55f, 0.70f, 1.00f),
                                      new Color(0.80f, 0.86f, 1.00f), p);
                float fIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p / 0.08f));
                float fOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - p) / 0.12f));
                cold.a = fIn * fOut * Mathf.Lerp(0.30f, 0.70f, p);
                return cold;
            }

            Color c;
            if (p < 0.55f)
                c = Color.Lerp(new Color(0.55f, 0.70f, 1.00f), new Color(1.00f, 0.72f, 0.35f), p / 0.55f);
            else
                c = Color.Lerp(new Color(1.00f, 0.72f, 0.35f), new Color(1.00f, 0.95f, 0.85f), (p - 0.55f) / 0.45f);

            float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p / 0.08f));
            float fadeOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - p) / 0.12f));
            c.a = fadeIn * fadeOut * Mathf.Lerp(0.35f, 1f, p);
            return c;
        }

        private static void EnsureMoteBuffers(SingularityInfo info, int count)
        {
            if (info.Motes != null && info.Motes.Length == count) return;

            info.Motes = new Mote[count];
            for (int i = 0; i < count; i++)
                RespawnMote(ref info.Motes[i], Config.PullRadius, true);

            info.MoteVerts = new Vector3[count * 4];
            info.MoteCols = new Color[count * 4];
            info.MoteUvs = new Vector2[count * 4];

            var tris = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                int v = i * 4, t = i * 6;
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;

                info.MoteUvs[v] = new Vector2(0f, 0f);
                info.MoteUvs[v + 1] = new Vector2(1f, 0f);
                info.MoteUvs[v + 2] = new Vector2(0f, 1f);
                info.MoteUvs[v + 3] = new Vector2(1f, 1f);
            }

            info.MoteMesh.Clear();
            info.MoteMesh.SetVertices(info.MoteVerts);
            info.MoteMesh.SetUVs(0, info.MoteUvs);
            info.MoteMesh.SetColors(info.MoteCols);
            info.MoteMesh.SetIndices(tris, MeshTopology.Triangles, 0);
        }

        private static float MoteFallTime(bool rotating)
        {
            float baseTime = rotating ? 2.5f : 5f;
            return baseTime / Mathf.Max(0.05f, Config.MoteSpeed);
        }

        private static void UpdateMotes(SingularityInfo info, Vector3 toCam, bool haveCam,
            float dt, float remainingLife, HoleLook look)
        {
            bool rotating = look.Rotating;
            int count = Mathf.Clamp(Config.MoteCount, 0, 2000);
            if (count == 0 || !haveCam)
            {
                info.MoteRenderer.enabled = false;
                return;
            }
            info.MoteRenderer.enabled = true;

            EnsureMoteBuffers(info, count);

            float outerR = Mathf.Max(0.5f, Config.PullRadius);
            float innerR = Mathf.Max(0.05f, info.EventHorizonRadius * DiskInnerEdge);
            float span = Mathf.Max(0.01f, outerR - innerR);

            float fallTime = MoteFallTime(rotating);
            float c = 0.6667f * Mathf.Pow(outerR, 1.5f) / fallTime;
            bool spawning = remainingLife > fallTime;

            float spinK = rotating ? 0.85f * outerR : 0f;
            float yPow = rotating ? 1.2f : 1.0f;

            float sizeBase = Mathf.Max(0.001f, Config.MoteSize);
            float streak = Mathf.Max(0f, Config.MoteStreak);
            float bright = Mathf.Max(0f, look.MoteBrightness);

            for (int i = 0; i < count; i++)
            {
                ref var m = ref info.Motes[i];

                int vd = i * 4;
                if (!m.Alive)
                {
                    info.MoteVerts[vd] = Vector3.zero;
                    info.MoteVerts[vd + 1] = Vector3.zero;
                    info.MoteVerts[vd + 2] = Vector3.zero;
                    info.MoteVerts[vd + 3] = Vector3.zero;
                    continue;
                }

                float rSafe = Mathf.Max(m.R, innerR * 0.5f);
                float fallSpeed = c / Mathf.Sqrt(rSafe);
                float omega = spinK <= 0f ? 0f : Mathf.Min(10f, spinK * m.Spin / rSafe);

                m.R -= fallSpeed * dt;
                m.Theta += omega * dt;

                if (m.R <= innerR)
                {
                    if (spawning)
                    {
                        RespawnMote(ref m, outerR, false);
                    }
                    else
                    {
                        m.Alive = false;
                        info.MoteVerts[vd] = Vector3.zero;
                        info.MoteVerts[vd + 1] = Vector3.zero;
                        info.MoteVerts[vd + 2] = Vector3.zero;
                        info.MoteVerts[vd + 3] = Vector3.zero;
                        continue;
                    }
                }

                float p = Mathf.Clamp01(1f - (m.R - innerR) / span);
                float cosT = Mathf.Cos(m.Theta);
                float sinT = Mathf.Sin(m.Theta);

                float y = m.Y0 * Mathf.Pow(Mathf.Clamp01(m.R / outerR), yPow);
                var pos = new Vector3(cosT * m.R, y, sinT * m.R);

                var vel = new Vector3(-sinT, 0f, cosT) * (omega * m.R)
                        - new Vector3(cosT, 0f, sinT) * fallSpeed;

                Vector3 dir = vel - toCam * Vector3.Dot(vel, toCam);
                if (dir.sqrMagnitude < 1e-8f)
                    dir = Vector3.Cross(toCam, Vector3.up);
                if (dir.sqrMagnitude < 1e-8f)
                    dir = Vector3.Cross(toCam, Vector3.right);
                dir.Normalize();
                Vector3 perp = Vector3.Cross(toCam, dir);

                float half = sizeBase * m.Size * 0.5f;
                float len = half * (1f + streak * vel.magnitude * 0.09f);

                Vector3 dl = dir * len;
                Vector3 dw = perp * half;

                int v = vd;
                info.MoteVerts[v] = pos - dl - dw;
                info.MoteVerts[v + 1] = pos + dl - dw;
                info.MoteVerts[v + 2] = pos - dl + dw;
                info.MoteVerts[v + 3] = pos + dl + dw;

                var col = MoteColor(p, rotating);
                col.a *= bright;
                var tail = col;
                tail.a *= 0.25f;
                info.MoteCols[v] = tail;
                info.MoteCols[v + 1] = col;
                info.MoteCols[v + 2] = tail;
                info.MoteCols[v + 3] = col;
            }

            info.MoteMesh.SetVertices(info.MoteVerts);
            info.MoteMesh.SetColors(info.MoteCols);
            info.MoteMesh.bounds = new Bounds(Vector3.zero, Vector3.one * outerR * 2.2f);
        }

        // ── Orientation helpers ─────────────────────────────────────────────────

        public static Quaternion RandomDiskTilt()
        {
            float maxTilt = Mathf.Abs(Config.DiskInclination);
            float incl = Rand(-maxTilt, maxTilt);
            float azimuth = Rand(0f, 360f);
            return Quaternion.Euler(0f, azimuth, 0f) * Quaternion.Euler(incl, 0f, 0f);
        }

        private static bool BuildBillboard(Vector3 toCam, Camera cam, out Quaternion rot)
        {
            Vector3 up = Vector3.ProjectOnPlane(cam.transform.up, toCam);
            if (up.sqrMagnitude < 1e-6f)
                up = Vector3.ProjectOnPlane(Vector3.up, toCam);
            if (up.sqrMagnitude < 1e-6f) { rot = Quaternion.identity; return false; }

            rot = Quaternion.LookRotation(up.normalized, toCam);
            return true;
        }
        private static bool BuildArcBillboard(Vector3 toCam, Vector3 diskNormal, out Quaternion rot)
        {
            Vector3 bulge = Vector3.ProjectOnPlane(diskNormal, toCam);
            if (bulge.sqrMagnitude < 1e-6f) { rot = Quaternion.identity; return false; }
            rot = Quaternion.LookRotation(bulge.normalized, toCam);
            return true;
        }

        private static bool BuildDiskRotation(Vector3 toCam, Quaternion tilt, out Quaternion rot, out float edgeOn)
        {
            Vector3 n = tilt * Vector3.up;
            edgeOn = 1f - Mathf.Abs(Vector3.Dot(toCam, n));

            Vector3 inPlane = Vector3.ProjectOnPlane(toCam, n);
            if (inPlane.sqrMagnitude < 1e-5f) { rot = tilt; return false; }

            rot = Quaternion.FromToRotation(tilt * Vector3.forward, inPlane.normalized) * tilt;
            return true;
        }

        private static Material MakeGlowMaterial(int queue)
        {
            var m = new Material(UnlitShader);
            m.color = Color.white;
            m.renderQueue = queue;
            return m;
        }

        private static Renderer AddMeshLayer(Transform parent, string name, Mesh mesh, float scale, int queue)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * scale;

            var mf = go.AddComponent(Il2CppType.Of<MeshFilter>()).TryCast<MeshFilter>();
            mf.mesh = mesh;

            var mr = go.AddComponent(Il2CppType.Of<MeshRenderer>()).TryCast<MeshRenderer>();
            mr.material = MakeGlowMaterial(queue);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Create / Destroy
        // ═══════════════════════════════════════════════════════════════════════

        public static GameObject Create(GravityWell well)
        {
            var root = new GameObject($"Singularity_{well.Position:F1}");

            // Event horizon radius: the visually opaque "shadow" of the black hole.
            float eh = ShadowRadius;

            float diskOuter = Mathf.Max(DiskInnerEdge + 0.5f, Config.DiskOuterScale);
            float diskInnerRatio = DiskInnerEdge / diskOuter;

            // ── 1. SHADOW — the opaque black sphere ──────────────────────────
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shadow.name = "Shadow";
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localScale = Vector3.one * eh * 2f;
            var shRend = shadow.GetComponent<Renderer>();
            shRend.material = new Material(OpaqueShader);
            shRend.material.color = new Color(0f, 0f, 0f, 1f);
            shRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shRend.receiveShadows = false;
            var shCol = shadow.GetComponent<Collider>();
            if (shCol != null) shCol.enabled = false;

            // ── 2. SKY DARKENING — contrast against a bright sky ─────────────
            var look = HoleLook.For(well.Rotating);
            float darkenOuter = Mathf.Max(DarkenOuterMin, look.SkyDarkenRadius);
            float darkenInnerRatio = 1f / darkenOuter;
            var darkenMesh = GetRingMesh($"SkyDarken|{darkenInnerRatio:F3}", darkenInnerRatio, 20, 64, 1.5f,
                MakeDarkenColorFn(darkenInnerRatio));
            var darkenRend = AddMeshLayer(root.transform, "SkyDarken", darkenMesh, eh * darkenOuter, QDarken);
            darkenRend.material.color = new Color(1f, 1f, 1f, Mathf.Clamp01(look.SkyDarken));

            // ── 3. GLOW — wide faint warm halo ───────────────────────────────
            var glowMesh = GetRingMesh("Glow", GlowInner, 18, 72, 1.4f, GlowColor);
            var glowRend = AddMeshLayer(root.transform, "Glow", glowMesh, eh * GlowOuter, QGlow);

            // ── 4. ACCRETION DISK — real equatorial plane, Doppler-lit ───────
            Renderer diskRend = null;
            Renderer swirlRend = null;
            if (well.Rotating)
            {
                var diskMesh = GetRingMesh($"Disk|{diskInnerRatio:F3}", diskInnerRatio, 24, 96, 1.5f,
                    MakeDiskColorFn(diskInnerRatio), bulgeAmplitude: 0.02f);
                diskRend = AddMeshLayer(root.transform, "AccretionDisk", diskMesh, eh * diskOuter, QDisk);

                float swirlInner = Mathf.Min(0.98f, diskInnerRatio + 0.02f);
                var swirlMesh = GetRingMesh($"DiskSwirl|{swirlInner:F3}", swirlInner, 20, 96, 1.4f,
                    MakeSwirlColorFn(swirlInner), bulgeAmplitude: 0.025f);
                swirlRend = AddMeshLayer(root.transform, "DiskSwirl", swirlMesh, eh * diskOuter, QSwirl);
            }

            // ── 5. LENSED ARCS — the vertical wrap over and under the shadow ─
            var arcMesh = GetRingMesh("LensedArcs", ArcInner, 26, 96, 1.3f, ArcColor);
            var arcRend = AddMeshLayer(root.transform, "LensedArcs", arcMesh, eh * ArcOuter, QArcs);

            // ── 6. PHOTON RING — razor sliver on the shadow rim ──────────────
            var photonMesh = GetRingMesh("PhotonRing", PhotonInner, 40, 96, 1.8f, PhotonRingColor);
            var photonRend = AddMeshLayer(root.transform, "PhotonRing", photonMesh, eh * PhotonOuter, QPhoton);

            // ── 7. MOTES — matter spiralling in from the pull radius ─────────
            var moteMesh = new Mesh();
            moteMesh.name = "Motes";
            moteMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            moteMesh.MarkDynamic();
            var moteRend = AddMeshLayer(root.transform, "Motes", moteMesh, 1f, QMotes);

            // ── Debug sphere ───────────────────────────────────────────────
            var debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugSphere.name = "DebugRadius";
            debugSphere.transform.SetParent(root.transform, false);
            debugSphere.transform.localScale = Vector3.one * Config.PullRadius * 2f;
            var debugMR = debugSphere.GetComponent<Renderer>();
            debugMR.material = MakeGlowMaterial(QGlow - 10);
            debugMR.material.color = new Color(1f, 1f, 1f, 0.06f);
            debugMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var debugCol = debugSphere.GetComponent<Collider>();
            if (debugCol != null) debugCol.enabled = false;
            debugMR.enabled = Config.DebugDrawRadius;

            // Store references
            var info = new SingularityInfo
            {
                Shadow = shadow,
                ShadowRenderer = shRend,
                SkyDarken = darkenRend,
                Glow = glowRend,
                AccretionDisk = diskRend,
                DiskSwirl = swirlRend,
                LensedArcs = arcRend,
                PhotonRing = photonRend,
                MoteRenderer = moteRend,
                MoteMesh = moteMesh,
                DebugSphere = debugMR,
                EventHorizonRadius = eh,
                DarkenRadius = darkenOuter,
                DiskScale = eh * diskOuter,
                Tilt = well.DiskTilt,
            };
            _vfxInfos[root] = info;

            root.transform.position = well.Position;

            if (Config.Dbg1)
                MelonLogger.Msg($"[Singularity][VFX] Root placed at {root.transform.position:F1}");

            return root;
        }

        public static void Destroy(GameObject root)
        {
            if (root == null) return;
            if (_vfxInfos.TryGetValue(root, out var info))
                TeardownInfo(info);
            _vfxInfos.Remove(root);
            Object.Destroy(root);
        }

        public static void Forget(GravityWell well)
        {
            if (ReferenceEquals(well.Visuals, null)) return;
            if (_vfxInfos.TryGetValue(well.Visuals, out var info))
            {
                TeardownInfo(info);
                _vfxInfos.Remove(well.Visuals);
            }
            well.Visuals = null;
        }

        public static void ResetForNewScene()
        {
            foreach (var kv in _vfxInfos)
                TeardownInfo(kv.Value);
            _vfxInfos.Clear();

            _cachedCam = null;
            _camPollCountdown = 0;
        }

        private static void TeardownInfo(SingularityInfo info)
        {
            if (info == null || info.MoteMesh == null) return;
            Object.Destroy(info.MoteMesh);
            info.MoteMesh = null;
        }

        public static void Update(GravityWell well)
        {
            if (well.Visuals == null) return;
            if (!_vfxInfos.TryGetValue(well.Visuals, out var info)) return;
            if (info.Collapsing) return;

            float age = 1f - (well.Lifetime / well.MaxLifetime);
            float t = Time.time;
            float eh = info.EventHorizonRadius;

            if (well.Visuals.transform.position != well.Position)
                well.Visuals.transform.position = well.Position;

            float massPulse = 1f + 0.03f * Mathf.Sin(t * Config.PulseSpeed * 0.5f);
            float ageGrow = 1f + age * 0.15f;
            float shadowScale = massPulse * ageGrow;

            info.Shadow.transform.localScale = Vector3.one * eh * 2f * shadowScale;
            info.SkyDarken.transform.localScale = Vector3.one * eh * info.DarkenRadius * shadowScale;
            info.Glow.transform.localScale = Vector3.one * eh * GlowOuter * shadowScale;
            info.LensedArcs.transform.localScale = Vector3.one * eh * ArcOuter * shadowScale;
            info.PhotonRing.transform.localScale = Vector3.one * eh * PhotonOuter * shadowScale;

            var look = HoleLook.For(well.Rotating);
            var tint = look.Tint;
            float fade = Mathf.Lerp(1f, 0.55f, age);

            float boost = Mathf.Max(0f, look.EmissionBoost);
            float hotR = tint.r * boost, hotG = tint.g * boost, hotB = tint.b * boost;

            // ── Billboarded layers ─────────────────────────────────────────
            var cam = GetCamera();
            Vector3 toCam = Vector3.zero;
            bool haveCam = false;
            if (cam != null)
            {
                toCam = cam.transform.position - well.Position;
                if (toCam.sqrMagnitude > 1e-4f) { toCam.Normalize(); haveCam = true; }
            }

            float edgeOn = 1f;
            if (haveCam)
            {
                if (BuildBillboard(toCam, cam, out var billboard))
                {
                    info.PhotonRing.transform.rotation = billboard;
                    info.Glow.transform.rotation = billboard;
                    info.SkyDarken.transform.rotation = billboard;
                    info.LensedArcs.transform.rotation = billboard;
                }

                if (BuildArcBillboard(toCam, well.SpinAxis, out var arcRot))
                    info.LensedArcs.transform.rotation = arcRot;

                if (info.AccretionDisk != null &&
                    BuildDiskRotation(toCam, info.Tilt, out var diskRot, out edgeOn))
                {
                    info.AccretionDisk.transform.rotation = diskRot;
                }
            }

            // ── Sky darkening ──────────────────────────────────────────────
            info.SkyDarken.material.color =
                new Color(1f, 1f, 1f, Mathf.Clamp01(look.SkyDarken * fade));

            // ── Photon ring ────────────────────────────────────────────────
            float ringBoost = look.PhotonRingBrightness;
            info.PhotonRing.material.color =
                new Color(hotR * ringBoost, hotG * ringBoost, hotB * ringBoost, fade);

            // ── Lensed arcs — the wrap only exists when you're off the disk plane ──
            float arcAlpha = well.Rotating
                ? Config.LensedArcStrength * Mathf.SmoothStep(0f, 1f, edgeOn * 1.6f) * fade
                : 0f;
            float arcPulse = 0.88f + 0.12f * Mathf.Sin(t * Config.PulseSpeed * 0.6f + 1.3f);
            info.LensedArcs.material.color =
                new Color(hotR, hotG, hotB, Mathf.Clamp01(arcAlpha * arcPulse));

            // ── Glow ───────────────────────────────────────────────────────
            float glowPulse = 0.85f + 0.15f * Mathf.Sin(t * Config.PulseSpeed * 0.7f);
            Color glowRgb = look.Rotating
                ? new Color(1.00f, 0.72f, 0.38f)
                : new Color(0.55f, 0.70f, 1.00f);
            info.Glow.material.color = new Color(
                tint.r * glowRgb.r, tint.g * glowRgb.g, tint.b * glowRgb.b,
                Mathf.Clamp01(look.GlowStrength * glowPulse * fade));

            // ── Disk + swirl ───────────────────────────────────────────────
            if (info.AccretionDisk != null)
            {
                info.AccretionDisk.transform.localScale = Vector3.one * info.DiskScale * ageGrow;
                info.AccretionDisk.material.color =
                    new Color(hotR, hotG, hotB, Mathf.Clamp01(Config.DiskBrightness * fade));

                if (info.DiskSwirl != null)
                {
                    info.SwirlAngle += Config.SwirlSpeed * Config.RingRotationSpeed * Time.deltaTime;
                    if (info.SwirlAngle > 360f) info.SwirlAngle -= 360f;

                    info.DiskSwirl.transform.localRotation =
                        info.Tilt * Quaternion.AngleAxis(info.SwirlAngle, Vector3.up);
                    info.DiskSwirl.transform.localScale = Vector3.one * info.DiskScale * ageGrow;
                    info.DiskSwirl.material.color = new Color(hotR, hotG, hotB,
                        Mathf.Clamp01(Config.DiskBrightness * Config.SwirlStrength * fade));
                }
            }

            // ── Motes — matter spiralling in, and the only cue for the pull radius ──
            UpdateMotes(info, toCam, haveCam, Time.deltaTime, well.Lifetime, look);

            info.DebugSphere.enabled = Config.DebugDrawRadius;
        }

        public static void Collapse(GravityWell well)
        {
            if (well.Visuals == null) return;
            if (!_vfxInfos.TryGetValue(well.Visuals, out var info)) return;
            if (info.Collapsing) return;
            info.Collapsing = true;

            if (Config.Dbg1)
                MelonLogger.Msg($"[Singularity] Starting collapse at {well.Position:F1}");

            MelonCoroutines.Start(CollapseCoroutine(well, info));
        }

        private static System.Collections.IEnumerator CollapseCoroutine(
            GravityWell well, SingularityInfo info)
        {
            float collapseDuration = 1f;
            float elapsed = 0f;
            var look = HoleLook.For(well.Rotating);

            while (elapsed < collapseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / collapseDuration;

                float scale;
                Color coreColor;
                Color hotColor;
                Color glowColor;

                if (t < 0.6f)
                {
                    float pt = t / 0.6f;

                    scale = 1f - pt * 0.9f;

                    float brightness = Mathf.Lerp(1f, 3f, pt);
                    coreColor = Color.black;
                    hotColor = new Color(brightness, brightness * 0.95f, brightness * 0.9f, 1f);
                    glowColor = new Color(brightness, brightness * 0.85f, brightness * 0.7f,
                        Mathf.Lerp(0.10f, 0.30f, pt));

                    if (info.DiskSwirl != null)
                        info.DiskSwirl.transform.Rotate(0f, (1f + pt * 25f) * 1.5f, 0f, Space.Self);
                }
                else
                {
                    float pt = (t - 0.6f) / 0.4f;

                    scale = 0.1f - pt * 0.1f;
                    coreColor = new Color(pt, pt, pt, 1f);
                    hotColor = new Color(1f, 1f, 1f, 1f - pt);
                    glowColor = new Color(1f, 1f, 1f, (1f - pt) * 0.5f);
                }

                if (well.Visuals == null) yield break;
                well.Visuals.transform.localScale = Vector3.one * Mathf.Max(0.001f, scale);

                info.ShadowRenderer.material.color = coreColor;
                info.PhotonRing.material.color = hotColor;
                info.LensedArcs.material.color = hotColor;
                info.Glow.material.color = glowColor;
                info.SkyDarken.material.color = new Color(1f, 1f, 1f,
                    Mathf.Clamp01(look.SkyDarken) * Mathf.Max(0f, 1f - t * 1.4f));
                info.MoteRenderer.material.color =
                    new Color(1f, 1f, 1f, Mathf.Max(0f, 1f - t * 2.2f));

                float diskFade = Mathf.Max(0f, 1f - t * 2f);
                if (info.AccretionDisk != null)
                    info.AccretionDisk.material.color = new Color(1f, 1f, 1f, diskFade);
                if (info.DiskSwirl != null)
                    info.DiskSwirl.material.color = new Color(1f, 1f, 1f, diskFade);

                yield return null;
            }

            TeardownInfo(info);
            if (well.Visuals != null)
            {
                _vfxInfos.Remove(well.Visuals);
                Object.Destroy(well.Visuals);
                well.Visuals = null;
            }
        }

        private class SingularityInfo
        {
            public GameObject Shadow;
            public Renderer ShadowRenderer;
            public Renderer SkyDarken;
            public Renderer Glow;
            public Renderer AccretionDisk;
            public Renderer DiskSwirl;
            public Renderer LensedArcs;
            public Renderer PhotonRing;
            public Renderer MoteRenderer;
            public Mesh MoteMesh;
            public Mote[] Motes;
            public Vector3[] MoteVerts;
            public Color[] MoteCols;
            public Vector2[] MoteUvs;
            public Renderer DebugSphere;
            public float EventHorizonRadius;
            public float DarkenRadius;
            public float DiskScale;
            public Quaternion Tilt;
            public float SwirlAngle;
            public bool Collapsing;
        }
    }
}
