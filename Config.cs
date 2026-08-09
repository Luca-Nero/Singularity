using UnityEngine;

namespace Singularity
{
    internal static class Config
    {
        // ── Controls ──────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Controls")] public static KeyCode DeployKey = KeyCode.R;
        [FruitLib.MenuCategory("Controls")] public static KeyCode HoleTypeKey = KeyCode.F5;

        // ── Physics ───────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Physics")] public static float PullRadius = 15f;
        [FruitLib.MenuCategory("Physics")] public static float PullForce = 8f;
        [FruitLib.MenuCategory("Physics")] public static float PullFalloff = 2f;
        [FruitLib.MenuCategory("Physics")] public static float PullUpward = 0.5f;
        [FruitLib.MenuCategory("Physics")] public static float SpinForce = 2f;
        // Fraction of the VISIBLE disk radius, not of PullRadius. 1 = shred anything
        // inside the glowing disk.
        [FruitLib.MenuCategory("Physics")] public static float AccretionThreshold = 1f;
        [FruitLib.MenuCategory("Physics")] public static float AccretionForce = 20f;

        // ── Behaviour ─────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Behaviour")] public static float Lifetime = 12f;
        [FruitLib.MenuCategory("Behaviour")] public static float SpawnRange = 40f;
        [FruitLib.MenuCategory("Behaviour")] public static float MassLimit = 200f;
        [FruitLib.MenuCategory("Behaviour")] public static bool SpawnRotating = true;

        // ── Visuals (shared by both hole types) ───────────────────────────────────
        [FruitLib.MenuCategory("Visuals")] public static float CoreScale = 0.5f;
        [FruitLib.MenuCategory("Visuals")] public static int MoteCount = 40;
        [FruitLib.MenuCategory("Visuals")] public static float MoteSize = 0.12f;
        [FruitLib.MenuCategory("Visuals")] public static float MoteSpeed = 1f;
        [FruitLib.MenuCategory("Visuals")] public static float MoteStreak = 1f;
        [FruitLib.MenuCategory("Visuals")] public static float PulseSpeed = 2f;
        [FruitLib.MenuCategory("Visuals")] public static float RingRotationSpeed = 3f;

        // ── Disk (Kerr only) ──────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Disk")] public static float DiskInclination = 12f;
        [FruitLib.MenuCategory("Disk")] public static float DiskInnerScale = 1.15f;
        [FruitLib.MenuCategory("Disk")] public static float DiskOuterScale = 4.5f;
        [FruitLib.MenuCategory("Disk")] public static float DiskBrightness = 1f;
        [FruitLib.MenuCategory("Disk")] public static float DopplerStrength = 1f;
        [FruitLib.MenuCategory("Disk")] public static float SwirlStrength = 0.5f;
        [FruitLib.MenuCategory("Disk")] public static float SwirlSpeed = 6f;
        [FruitLib.MenuCategory("Disk")] public static float LensedArcStrength = 1f;

        // ── Kerr (rotating) ───────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Kerr")] public static float KerrPhotonRingBrightness = 0.5f;
        [FruitLib.MenuCategory("Kerr")] public static float KerrGlowStrength = 0.2f;
        [FruitLib.MenuCategory("Kerr")] public static float KerrSkyDarken = 0.65f;
        [FruitLib.MenuCategory("Kerr")] public static float KerrSkyDarkenRadius = 6f;
        [FruitLib.MenuCategory("Kerr")] public static float KerrMoteBrightness = 1f;
        [FruitLib.MenuCategory("Kerr")] public static float KerrEmissionBoost = 1.6f;
        [FruitLib.MenuCategory("Kerr")] public static float KerrTintR = 1f;
        [FruitLib.MenuCategory("Kerr")] public static float KerrTintG = 1f;
        [FruitLib.MenuCategory("Kerr")] public static float KerrTintB = 1f;

        // ── Schwarzschild (stationary) ────────────────────────────────────────────
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwPhotonRingBrightness = 0.5f;
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwGlowStrength = 0.14f;
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwSkyDarken = 0.65f;
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwSkyDarkenRadius = 6f;
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwMoteBrightness = 1f;
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwEmissionBoost = 1.6f;
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwTintR = 1f;
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwTintG = 1f;
        [FruitLib.MenuCategory("Schwarzschild")] public static float SchwTintB = 1f;

        // ── Debug ─────────────────────────────────────────────────────────────────
        [FruitLib.MenuCategory("Debug")] public static int DebugLevel = 0;
        [FruitLib.MenuCategory("Debug")] public static bool DebugDrawRadius = false;
        [FruitLib.MenuCategory("Debug")] public static float DebugRayDuration = 1f;
        [FruitLib.MenuCategory("Debug")] public static KeyCode ShaderDumpKey = KeyCode.F9;
        [FruitLib.MenuCategory("Debug")] public static bool ExperimentalDistortion = false;

        // ── Helpers ───────────────────────────────────────────────────────────────
        public static bool Dbg1 => DebugLevel >= 1;
        public static bool Dbg2 => DebugLevel >= 2;
    }
    internal struct HoleLook
    {
        public bool Rotating;
        public float PhotonRingBrightness;
        public float GlowStrength;
        public float SkyDarken;
        public float SkyDarkenRadius;
        public float MoteBrightness;
        public float EmissionBoost;
        public Color Tint;

        public static HoleLook For(bool rotating) => rotating
            ? new HoleLook
            {
                Rotating = true,
                PhotonRingBrightness = Config.KerrPhotonRingBrightness,
                GlowStrength = Config.KerrGlowStrength,
                SkyDarken = Config.KerrSkyDarken,
                SkyDarkenRadius = Config.KerrSkyDarkenRadius,
                MoteBrightness = Config.KerrMoteBrightness,
                EmissionBoost = Config.KerrEmissionBoost,
                Tint = new Color(Config.KerrTintR, Config.KerrTintG, Config.KerrTintB, 1f),
            }
            : new HoleLook
            {
                Rotating = false,
                PhotonRingBrightness = Config.SchwPhotonRingBrightness,
                GlowStrength = Config.SchwGlowStrength,
                SkyDarken = Config.SchwSkyDarken,
                SkyDarkenRadius = Config.SchwSkyDarkenRadius,
                MoteBrightness = Config.SchwMoteBrightness,
                EmissionBoost = Config.SchwEmissionBoost,
                Tint = new Color(Config.SchwTintR, Config.SchwTintG, Config.SchwTintB, 1f),
            };
    }
}
