# Singularity

![Version](https://img.shields.io/github/v/release/Luca-Nero/Singularity?style=flat-square)
![Game Version](https://img.shields.io/badge/Game-v0.1%2B-blue?style=flat-square)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Donate-ff5e5b?style=flat-square&logo=ko-fi&logoColor=white)](https://ko-fi.com/Luca_Nero)

Deploy portable gravity wells that pull, spin, and shred anything nearby. Each one is a fully procedural black hole built from runtime-generated Unity meshes - accretion disk, photon ring, lensed arcs, and infalling motes - with no video and no pre-baked sprite anywhere in the pipeline.

---

## Features

- **Deploy:** Press **R** to drop a singularity at your aim point. A raycast finds the surface; if nothing is hit, it places at half `SpawnRange` along your view. Half-second cooldown between deploys, and each well collapses and despawns after its `Lifetime`.
- **Two Hole Types:** Press **F5** to choose what the *next* deploy will be. Existing holes keep whatever type they spawned as, so both can be out at once.
    - **Kerr (rotating):** Accretion disk, spiralling motes, lensed arcs, and a tangential spin force on anything caught in the pull.
    - **Schwarzschild (stationary):** Bare shadow and photon ring only. Motes fall straight in, cold, with no spin force.
- **Gravitational Pull:** Rigidbodies inside `PullRadius` are drawn toward the well, scaled by `PullForce` and an exponential falloff that concentrates the pull near the centre. A slight upward bias keeps the hole from simply yanking everything into the floor.
    - **Mass Limit:** Objects heavier than `MassLimit` are ignored, and the player's own ragdoll is excluded entirely.
- **Accretion Shredding:** Anything crossing into the visible disk - a fraction of the rendered radius set by `AccretionThreshold` - gets crushed and torn with extra impulse and torque.
- **Procedural Visuals:** Everything is generated at runtime from ring meshes, no textures. Shadow sphere scale, disk inner/outer radius, per-hole randomised disk tilt, Doppler beaming, a spinning swirl overlay, lensed arcs above and below the shadow, and infalling motes streaked along their velocity.
    - **Per-Type Look:** Kerr and Schwarzschild have independent brightness, glow, sky-darkening, and RGB tint knobs, since a stationary hole with no disk needs a different visual balance than a spinning one.
    - **HDR Bloom:** Most emissive layers push brightness above 1 via `*EmissionBoost` and rely on HDR plus bloom to read properly.
- **HUD & Perf:** A HUD panel shows the deploy key and next-spawn type, plus live count, affected body count, and pull radius/force while any hole is active. FruitLib's perf monitor (**F11**) gets two extra counters for active singularities and total affected rigidbodies.
- **QoL Tweaks:** Active singularities are cleared automatically on scene load, `DebugLevel` 1 logs deploy/collapse events (2 adds verbose per-frame physics logs), and `DebugDrawRadius` draws a wireframe sphere at the pull radius.

## Requirements & Compatibility

- **Prerequisites:** MelonLoader 0.7.2+ Installation. [Check out their Tutorial!](https://melonwiki.xyz/#/)
- **Prerequisites:** [FruitLib](https://github.com/Luca-Nero/FruitLib) in your `Mods/` folder - Singularity will not start without it.
- **Compatibility:** No known Incompatabilities.

## Installation

1. Download the latest release from the [Releases page](../../releases/latest).
2. Extract the archive.
3. Drop the contents into your game's `Mods/` directory.

## Controls (Defaults)

| Key | Action |
|-----|--------|
| B | Deploy singularity at aim point |
| F5 | Toggle next-deploy type (Kerr / Schwarzschild) |

## Configuration

`SingularityConfig.ini` is created next to the DLL on first launch. It is sectioned and documented - Controls, Physics, Behaviour, Visuals, Disk (Kerr only), Kerr, Schwarzschild, and Debug - with a comment on each field explaining what it does. The in-game FruitLib menu mirrors the same categories, and the file is rewritten on load so new fields appear on update while stale ones are dropped.

---

## Support & Feedback

Found a bug or have a suggestion? Feel free to open an issue on the [Issues page](../../issues) or catch me on Discord.

If you enjoy my work and want to support future updates, feel free to [buy me a coffee on Ko-fi](https://ko-fi.com/Luca_Nero)!

## License

[MIT](LICENSE) © Luca Nero / Game Community
