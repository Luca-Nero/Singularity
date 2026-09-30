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
        // 1.2.0 = deploy moved onto a toolbar slot (wheel = type, LMB = deploy, RMB = collapse all);
        //         keyboard binds removed, HUD only shows while the slot is in hand.
        // 2.0.0 = release build: one inventory item per hole type (Kerr / Schwarzschild) instead
        //         of a wheel-cycled toolbar slot; SpawnRotating config removed. Needs FruitLib 4.
        public const string Version = "2.0.2";

        // ── Input ───────────────────────────────────────────────────────────────
        private static float _deployCooldown;

        // ── FruitLib dependency ──────────────────────────────────────────────
        private const int LibMajor = 5, LibMinor = 5, LibPatch = 0;
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
            // Before the ini is read, so Reset to Defaults goes back to the code's values.
            FruitMenu.CaptureDefaults(typeof(Config));
            ConfigLoader.Load();
            FruitMenu.Register("Singularity", ConfigLoader.IniPath, typeof(Config), ConfigLoader.Write);
            FruitHud.Register("Singularity", BuildHud, order: 20);
            RegisterItems();

            HoleManager.PublishField();

            var perf = FruitPerfMon.For("Singularity");
            perf.Counter("Singularities", () => HoleManager.ActiveCount);
            perf.Counter("Affected RBs", () => HoleManager.AffectedRbs());

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

            _deployCooldown = Mathf.Max(0f, _deployCooldown - dt);
            if (_equipped && !FruitMenu.BlocksGameplayInput) TickSlot();

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
            _equipped = false;   // a scene reload drops the held item without firing OnDeselected
            if (Config.Dbg1)
                LoggerInstance.Msg($"[Singularity] Scene '{sceneName}' loaded — cleared active singularities.");
        }

        // ── Inventory items ─────────────────────────────────────────────────────
        // One item per hole type, same pattern as BombsAway and GunsGunsGuns: the item in hand
        // is the type, LMB deploys, RMB collapses all. Holes keep the type they were deployed
        // as, so both can be out at once.

        private static readonly Color KerrColour          = new Color(1.00f, 0.55f, 0.15f);   // disk orange
        private static readonly Color SchwarzschildColour = new Color(0.35f, 0.25f, 0.55f);   // cold violet

        private static bool _equipped;
        private static bool _heldRotating;   // valid while _equipped

        private static void RegisterItems()
        {
            AddHoleItem(rotating: true,  "Kerr",
                "Rotating black hole. Accretion disk, lensed arcs and a spin that drags everything round as it pulls.",
                KerrColour);
            AddHoleItem(rotating: false, "Schwarzschild",
                "Stationary black hole. Bare shadow and photon ring; everything falls straight in.",
                SchwarzschildColour);
        }

        private static void AddHoleItem(bool rotating, string name, string description, Color colour)
        {
            FruitInventory.AddItem(new FruitItem
            {
                Id           = "Singularity:" + name,
                Name         = name,
                Description  = description,
                Category     = nameof(FruitItemCategory.Tool),
                Icon         = FruitIcons.Solid(colour),
                OnSelected   = item => OnHoleSelected(rotating, item.Slot),
                OnDeselected = item => OnHoleDeselected(rotating),
            }
            .AddStat("left click", "deploy")
            .AddStat("right click", "collapse all"));
        }

        private static void OnHoleSelected(bool rotating, int slot)
        {
            // Switching straight between the two can deliver the new select before the old
            // deselect; selecting over the other one is a swap.
            _heldRotating = rotating;
            _equipped     = true;
            if (Config.Dbg1) MelonLogger.Msg($"[Singularity] {HoleTypeName(rotating)} equipped (slot {slot + 1})");
        }

        private static void OnHoleDeselected(bool rotating)
        {
            // The late deselect of the type just swapped out: the other one is already in hand.
            if (!_equipped || _heldRotating != rotating) return;
            _equipped = false;
        }

        private static string HoleTypeName(bool rotating) => rotating ? "Kerr" : "Schwarzschild";

        private static void TickSlot()
        {
            if (Input.GetMouseButtonDown(0) && _deployCooldown <= 0f)
            {
                HoleManager.TryDeploy(_heldRotating);
                _deployCooldown = 0.5f; // half-second cooldown between deployments
            }

            if (Input.GetMouseButtonDown(1)) HoleManager.ClearAll();
        }

        private static void BuildHud(HudPanel p)
        {
            if (!_equipped) return;

            p.Line($"Type: {HoleTypeName(_heldRotating).ToUpperInvariant()}");

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
