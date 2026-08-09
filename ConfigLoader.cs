using MelonLoader;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Singularity
{
    internal static class ConfigLoader
    {
        public static string IniPath => Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "SingularityConfig.ini");

        public static void Load()
        {
            try
            {
                if (!File.Exists(IniPath)) { Write(); MelonLogger.Msg("Wrote default SingularityConfig.ini"); return; }
                foreach (var line in File.ReadAllLines(IniPath))
                {
                    string t = line.Trim();
                    if (string.IsNullOrEmpty(t) || t.StartsWith("#")) continue;
                    int eq = t.IndexOf('=');
                    if (eq < 0) continue;
                    SetField(t.Substring(0, eq).Trim(), t.Substring(eq + 1).Trim());
                }
                Write();
                MelonLogger.Msg("SingularityConfig.ini loaded.");
            }
            catch (Exception e) { MelonLogger.Warning($"Config load failed: {e.Message}"); }
        }

        private static void SetField(string key, string value)
        {
            var f = typeof(Config).GetField(key, BindingFlags.Public | BindingFlags.Static);
            if (f == null) return;
            try
            {
                if (f.FieldType == typeof(float)) f.SetValue(null, float.Parse(value, CultureInfo.InvariantCulture));
                else if (f.FieldType == typeof(int)) f.SetValue(null, int.Parse(value));
                else if (f.FieldType == typeof(bool)) f.SetValue(null, value.ToLower() == "true");
                else if (f.FieldType == typeof(KeyCode)) f.SetValue(null, (KeyCode)Enum.Parse(typeof(KeyCode), value, true));
            }
            catch { }
        }

        private static readonly Dictionary<string, string> FieldHelp = new Dictionary<string, string>
        {
            ["DeployKey"] = "key to deploy a singularity at your look target",

            ["PullRadius"] = "radius in metres within which rigidbodies are affected",
            ["PullForce"] = "inward pull strength applied to affected rigidbodies",
            ["PullFalloff"] = "falloff exponent on (1 - dist/radius): 1 = linear, 2 = quadratic (default), higher = pull concentrated near the centre",
            ["PullUpward"] = "vertical bias factor to keep the hole above the ground",
            ["AccretionThreshold"] = "crushing radius as a fraction of the VISIBLE disk radius (1 = anything inside the glowing disk). Scales with CoreScale and DiskOuterScale, so the shredding stays where the picture says it is",
            ["SpinForce"] = "tangential force, applied about the hole's own disk normal. Rotating (Kerr) holes only -- a Schwarzschild hole imparts none",
            ["AccretionForce"] = "force applied when objects are crushed into the singularity",

            ["Lifetime"] = "how many seconds a singularity lasts before collapsing",
            ["SpawnRange"] = "maximum distance (m) you can deploy a singularity",
            ["MassLimit"] = "rigidbodies heavier than this are ignored (safety against immovable objects)",

            ["CoreScale"] = "size multiplier for the shadow (event horizon) sphere",
            ["HoleTypeKey"] = "toggles the type of the NEXT singularity between Kerr and Schwarzschild",
            ["SpawnRotating"] = "type for the next deploy: true = rotating (Kerr, accretion disk + lensed arcs + spiralling motes), false = stationary (Schwarzschild, bare shadow, motes fall straight in cold). Each hole keeps whatever it was deployed as, so the two can coexist",
            ["DiskInclination"] = "maximum disk tilt in degrees. Each hole rolls its own tilt in [-this, +this] plus a random azimuth, so no two look alike. 0 = every disk perfectly edge-on",
            ["DiskInnerScale"] = "disk inner edge (the ISCO) in units of the shadow radius. 1.15 is physical -- the innermost stable orbit sits just outside the shadow, so hot disk material runs right up to the rim. Raise it to open a dark gap between the rim and the disk",
            ["DiskOuterScale"] = "disk outer radius in units of the shadow radius",
            ["DiskBrightness"] = "overall disk brightness multiplier",
            ["DopplerStrength"] = "0 = evenly lit disk, 1 = full relativistic beaming (one side much brighter)",
            ["SwirlStrength"] = "opacity of the spinning streak overlay on the disk",
            ["SwirlSpeed"] = "how fast the streak overlay orbits",
            ["LensedArcStrength"] = "brightness of the arcs lensed over and under the shadow; 0 disables them",
            ["MoteCount"] = "how many infalling motes orbit in from the pull radius (0 disables them)",
            ["MoteSize"] = "mote size in metres before streaking",
            ["MoteSpeed"] = "how fast motes fall in; 1 = about 2.5s from the pull radius to the disk (5s for a Schwarzschild hole)",
            ["MoteStreak"] = "how far motes elongate along their motion; 0 = round dots",
            ["PulseSpeed"] = "speed of the mass pulse animation",
            ["RingRotationSpeed"] = "global multiplier on rotation speeds",

            ["KerrPhotonRingBrightness"] = "Kerr: brightness of the thin bright ring on the shadow rim",
            ["KerrGlowStrength"] = "Kerr: wide warm halo strength; keep low, a broad wash washes the hole out",
            ["KerrSkyDarken"] = "Kerr: peak dimming of the world behind the hole (0 = off, 1 = black). Above 1 does nothing -- widen the radius instead",
            ["KerrSkyDarkenRadius"] = "Kerr: how far the dimming reaches, in units of the shadow radius (min 1.5)",
            ["KerrMoteBrightness"] = "Kerr: mote brightness multiplier",
            ["KerrEmissionBoost"] = "Kerr: over-1 colour push on disk, arcs and photon ring; only visible with HDR + bloom, otherwise it clamps",
            ["KerrTintR"] = "Kerr: red multiplier on every emissive layer (1 = physical)",
            ["KerrTintG"] = "Kerr: green multiplier on every emissive layer (1 = physical)",
            ["KerrTintB"] = "Kerr: blue multiplier on every emissive layer (1 = physical)",

            ["SchwPhotonRingBrightness"] = "Schwarzschild: brightness of the thin bright ring on the shadow rim",
            ["SchwGlowStrength"] = "Schwarzschild: halo strength. Tinted cold rather than warm -- with no disk or arcs there is no warm source for it to be bloom from",
            ["SchwSkyDarken"] = "Schwarzschild: peak dimming of the world behind the hole (0 = off, 1 = black)",
            ["SchwSkyDarkenRadius"] = "Schwarzschild: how far the dimming reaches, in units of the shadow radius (min 1.5)",
            ["SchwMoteBrightness"] = "Schwarzschild: mote brightness multiplier",
            ["SchwEmissionBoost"] = "Schwarzschild: over-1 colour push on the photon ring; only visible with HDR + bloom",
            ["SchwTintR"] = "Schwarzschild: red multiplier on every emissive layer (1 = physical)",
            ["SchwTintG"] = "Schwarzschild: green multiplier on every emissive layer (1 = physical)",
            ["SchwTintB"] = "Schwarzschild: blue multiplier on every emissive layer (1 = physical)",

            ["DebugLevel"] = "0 = silent, 1 = events, 2 = verbose per-frame",
            ["DebugDrawRadius"] = "draw a wireframe sphere showing pull radius",
            ["ShaderDumpKey"] = "dumps every loaded shader name to SingularityShaderDump.txt",
            ["ExperimentalDistortion"] = "test sphere using the game's 'Unify' blur shader -- it is a UI backdrop shader, so on a world-space mesh it renders as a pale sphere that flattens all contrast. Off by default",
        };

        private static bool IsRenderable(Type t) =>
            t == typeof(bool) || t == typeof(float) || t == typeof(int) ||
            t == typeof(string) || t == typeof(KeyCode);

        public static void Write()
        {
            var sb = new StringBuilder();

            sb.AppendLine("# ╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("# ║        Singularity — Portable Gravity Well Mod               ║");
            sb.AppendLine("# ╚══════════════════════════════════════════════════════════════╝");
            sb.AppendLine("# Reload requires game restart. All floats use . as decimal separator.");
            sb.AppendLine();

            var categories = new List<string>();
            var byCategory = new Dictionary<string, List<FieldInfo>>();

            foreach (var f in typeof(Config).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.IsSpecialName || !IsRenderable(f.FieldType)) continue;
                var attr = (FruitLib.MenuCategoryAttribute)Attribute.GetCustomAttribute(
                    f, typeof(FruitLib.MenuCategoryAttribute));
                if (attr == null) continue;

                if (!byCategory.TryGetValue(attr.Name, out var list))
                {
                    list = new List<FieldInfo>();
                    byCategory[attr.Name] = list;
                    categories.Add(attr.Name);
                }
                list.Add(f);
            }

            foreach (var cat in categories)
            {
                sb.AppendLine($"# ── {cat} ──");
                foreach (var f in byCategory[cat])
                {
                    if (FieldHelp.TryGetValue(f.Name, out var help))
                        sb.AppendLine($"# {f.Name} : {help}");
                    sb.AppendLine($"{f.Name} = {FormatValue(f)}");
                }
                sb.AppendLine();
            }

            File.WriteAllText(IniPath, sb.ToString());
        }

        private static string FormatValue(FieldInfo f)
        {
            object val = f.GetValue(null);
            if (f.FieldType == typeof(float))
                return ((float)val).ToString("0.##############", CultureInfo.InvariantCulture);
            return val?.ToString() ?? "";
        }
    }
}
