using FruitLib;
using HarmonyLib;
using MelonLoader;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: MelonInfo(typeof(Singularity.Core), "Singularity", Singularity.Core.Version, "Luca_Nero")]
[assembly: MelonGame()]
[assembly: MelonOptionalDependencies("FruitLib")]
[assembly: HarmonyDontPatchAll]

namespace Singularity
{

    public class Core : MelonMod
    {
        public const string Version = "1.0.5";

        // ── Input ───────────────────────────────────────────────────────────────
        private static float _deployCooldown;

        // ── FruitLib dependency ──────────────────────────────────────────────
        private const int LibMajor = 2, LibMinor = 0, LibPatch = 0;
        private bool _active;

        public override void OnInitializeMelon()
        {
            _active = FruitGate.Check("Singularity", LibMajor, LibMinor, LibPatch);
            if (!_active) return;

            Init();
        }

        public override void OnLateInitializeMelon()
        {
            if (_active) return;
            try { Unregister(FruitGate.FailureReason, silent: true); } catch { }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Init()
        {
            ConfigLoader.Load();
            FruitMenu.Register("Singularity", ConfigLoader.IniPath, typeof(Config));
            FruitHud.Register("Singularity", BuildHud, order: 20);

            FruitPerfMon.RegisterCounter("Singularities", () => HoleManager.ActiveCount);
            FruitPerfMon.RegisterCounter("Affected RBs", () => HoleManager.AffectedRbs());

            FruitUpdateCheck.Register("Singularity", Version, "Luca-Nero", "Singularity");

            LoggerInstance.Msg($"Singularity v{Version} loaded.");
        }

        public override void OnUpdate()
        {
            if (!_active) return;
            UpdateBody();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdateBody()
        {
            float dt = Time.deltaTime;

            if (!FruitMenu.IsInputSuppressed)
            {
                if (Input.GetKeyDown(Config.DeployKey))
                {
                    if (_deployCooldown <= 0f)
                    {
                        HoleManager.TryDeploy();
                        _deployCooldown = 0.5f; // half-second cooldown between deployments
                    }
                }

                if (Input.GetKeyDown(Config.HoleTypeKey))
                {
                    Config.SpawnRotating = !Config.SpawnRotating;
                    LoggerInstance.Msg($"Next singularity: {(Config.SpawnRotating ? "Kerr (rotating)" : "Schwarzschild (stationary)")}");
                }

                _deployCooldown = Mathf.Max(0f, _deployCooldown - dt);
            }

            HoleManager.Update(dt);
        }

        public override void OnFixedUpdate()
        {
            if (!_active) return;
            FixedUpdateBody();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void FixedUpdateBody()
        {
            HoleManager.FixedUpdate(Time.fixedDeltaTime);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (!_active) return;
            SceneLoadedBody(sceneName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SceneLoadedBody(string sceneName)
        {
            HoleManager.ClearAll();
            if (Config.Dbg1)
                LoggerInstance.Msg($"[Singularity] Scene '{sceneName}' loaded — cleared active singularities.");
        }

        private static void BuildHud(HudPanel p)
        {
            string type = Config.SpawnRotating ? "KERR" : "SCHWARZSCHILD";
            p.Line($"[ {Config.DeployKey} ] Deploy Singularity");
            p.Line($"[ {Config.HoleTypeKey} ] Type: {type}");

            int active = HoleManager.ActiveCount;
            if (active > 0)
            {
                p.Line($"⬤ Active: {active}");
                p.Line($"  Affected: {HoleManager.AffectedRbs()} bodies");
                p.Line($"  Pull: {Config.PullRadius}m @ {Config.PullForce}N");
            }

            if (Config.Dbg1)
            {
                p.Separator();
                p.Line($"Debug | PullForce={Config.PullForce} Falloff={Config.PullFalloff}", HudPanel.Dim);
                p.Line($"       Spin={Config.SpinForce} Acc={Config.AccretionThreshold}", HudPanel.Dim);
            }
        }
    }
}
