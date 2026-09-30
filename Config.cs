using FruitLib;
using UnityEngine;

namespace Singularity
{
    /// <summary>
    /// Every value the mod reads, and how the mod menu should draw it.
    ///
    /// Ranges are not decoration. Without one FruitLib guesses from the default, and the
    /// guess is wrong in the direction that matters here: most of these end up inside a
    /// <c>Clamp01</c> in <see cref="HoleVFX"/>, so a derived 0..4 slider would spend three
    /// quarters of its travel doing nothing. Where a value is clamped downstream, the
    /// range below matches the clamp.
    /// </summary>
    internal static class Config
    {
        // ── Physics ───────────────────────────────────────────────────────────────

        [MenuCategory("Physics"), MenuLabel("Pull radius (m)"), MenuRange(1, 600)]
        public static float PullRadius = 15f;

        [MenuCategory("Physics"), MenuLabel("Pull strength"), MenuRange(0, 5000)]
        public static float PullForce = 8f;

        [MenuCategory("Physics"), MenuLabel("Falloff exponent"), MenuRange(0, 5)]
        public static float PullFalloff = 2f;

        [MenuCategory("Physics"), MenuLabel("Upward bias"), MenuRange(0, 2)]
        public static float PullUpward = 0.5f;

        [MenuCategory("Physics"), MenuLabel("Frame dragging (Kerr)"), MenuRange(0, 20)]
        public static float SpinForce = 2f;

        [MenuCategory("Physics"), MenuLabel("Accretion distance"), MenuRange(0, 10)]
        public static float AccretionThreshold = 1f;

        [MenuCategory("Physics"), MenuLabel("Accretion violence"), MenuRange(0, 100)]
        public static float AccretionForce = 20f;

        // ── Behaviour ─────────────────────────────────────────────────────────────

        [MenuCategory("Behaviour"), MenuLabel("Lifetime (s)"), MenuRange(1, 300)]
        public static float Lifetime = 12f;

        [MenuCategory("Behaviour"), MenuLabel("Deploy range (m)"), MenuRange(5, 200)]
        public static float SpawnRange = 40f;

        [MenuCategory("Behaviour"), MenuLabel("Heaviest body pulled (kg)"), MenuRange(0, 2000)]
        public static float MassLimit = 200f;

        // ── Visuals (shared by both hole types) ───────────────────────────────────

        [MenuCategory("Visuals"), MenuLabel("Core size"), MenuRange(0.1f, 3)]
        public static float CoreScale = 0.5f;

        [MenuCategory("Visuals"), MenuLabel("Mote count"), MenuRange(0, 200)]
        public static int MoteCount = 40;

        [MenuCategory("Visuals"), MenuLabel("Mote size"), MenuRange(0.01f, 1)]
        public static float MoteSize = 0.12f;

        [MenuCategory("Visuals"), MenuLabel("Mote speed"), MenuRange(0.05f, 5)]
        public static float MoteSpeed = 1f;

        [MenuLabel("Mote streaking"), MenuRange(0, 5)]
        public static float MoteStreak = 1f;

        [MenuLabel("Pulse speed"), MenuRange(0, 10)]
        public static float PulseSpeed = 2f;

        [MenuLabel("Ring rotation speed"), MenuRange(0, 20)]
        public static float RingRotationSpeed = 3f;

        // ── Disk (Kerr only) ──────────────────────────────────────────────────────

        [MenuCategory("Disk"), MenuLabel("Inclination (deg)"), MenuRange(0, 90)]
        public static float DiskInclination = 12f;

        [MenuLabel("Inner edge"), MenuRange(1.02f, 3.5f)]
        public static float DiskInnerScale = 1.15f;

        [MenuCategory("Disk"), MenuLabel("Outer edge"), MenuRange(1.5f, 10)]
        public static float DiskOuterScale = 4.5f;

        [MenuCategory("Disk"), MenuLabel("Brightness"), MenuRange(0, 1)]
        public static float DiskBrightness = 1f;

        [MenuLabel("Doppler beaming"), MenuRange(0, 1)]
        public static float DopplerStrength = 1f;

        [MenuLabel("Swirl strength"), MenuRange(0, 1)]
        public static float SwirlStrength = 0.5f;

        [MenuLabel("Swirl speed"), MenuRange(0, 30)]
        public static float SwirlSpeed = 6f;

        [MenuCategory("Disk"), MenuLabel("Lensed arcs"), MenuRange(0, 2)]
        public static float LensedArcStrength = 1f;

        // ── Kerr (rotating) ───────────────────────────────────────────────────────

        [MenuLabel("Photon ring"), MenuRange(0, 2)]
        public static float KerrPhotonRingBrightness = 0.5f;

        [MenuCategory("Kerr"), MenuLabel("Glow"), MenuRange(0, 1)]
        public static float KerrGlowStrength = 0.2f;

        [MenuLabel("Sky darkening"), MenuRange(0, 1)]
        public static float KerrSkyDarken = 0.65f;

        [MenuLabel("Darkening radius"), MenuRange(0, 30)]
        public static float KerrSkyDarkenRadius = 6f;

        [MenuLabel("Mote brightness"), MenuRange(0, 3)]
        public static float KerrMoteBrightness = 1f;

        [MenuLabel("Emission boost"), MenuRange(0, 5)]
        public static float KerrEmissionBoost = 1.6f;

        [MenuCategory("Kerr"), MenuLabel("Tint, red"), MenuRange(0, 1)]
        public static float KerrTintR = 1f;

        [MenuCategory("Kerr"), MenuLabel("Tint, green"), MenuRange(0, 1)]
        public static float KerrTintG = 1f;

        [MenuCategory("Kerr"), MenuLabel("Tint, blue"), MenuRange(0, 1)]
        public static float KerrTintB = 1f;

        // ── Schwarzschild (stationary) ────────────────────────────────────────────

        [MenuLabel("Photon ring"), MenuRange(0, 2)]
        public static float SchwPhotonRingBrightness = 0.5f;

        [MenuCategory("Schwarzschild"), MenuLabel("Glow"), MenuRange(0, 1)]
        public static float SchwGlowStrength = 0.14f;

        [MenuLabel("Sky darkening"), MenuRange(0, 1)]
        public static float SchwSkyDarken = 0.65f;

        [MenuLabel("Darkening radius"), MenuRange(0, 30)]
        public static float SchwSkyDarkenRadius = 6f;

        [MenuLabel("Mote brightness"), MenuRange(0, 3)]
        public static float SchwMoteBrightness = 1f;

        [MenuLabel("Emission boost"), MenuRange(0, 5)]
        public static float SchwEmissionBoost = 1.6f;

        [MenuCategory("Schwarzschild"), MenuLabel("Tint, red"), MenuRange(0, 1)]
        public static float SchwTintR = 1f;

        [MenuCategory("Schwarzschild"), MenuLabel("Tint, green"), MenuRange(0, 1)]
        public static float SchwTintG = 1f;

        [MenuCategory("Schwarzschild"), MenuLabel("Tint, blue"), MenuRange(0, 1)]
        public static float SchwTintB = 1f;

        // ── Debug ─────────────────────────────────────────────────────────────────

        [MenuCategory("Debug"), MenuLabel("Logging detail"), MenuRange(0, 2)]
        public static int DebugLevel = 0;

        [MenuCategory("Debug"), MenuLabel("Draw pull radius")]
        public static bool DebugDrawRadius = false;

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
