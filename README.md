# Singularity v1.0

Deploy portable gravity wells that pull, spin, and shred anything nearby, with
a fully procedural black hole rendered in Unity meshes — no video, no
pre-baked sprite. Runs on [FruitLib](../5_FruitLib).

---

## Features

### Deploy
Press **R** to look somewhere and drop a singularity at your aim point (a
raycast finds the surface; if nothing's hit, it places at half `SpawnRange`
along your view). Half-second cooldown between deploys. Each one lives for
`Lifetime` seconds, then collapses and despawns.

### Two hole types
Press **F5** to pick what the *next* deploy will be — existing holes keep
whatever type they were spawned as, so both can be out at once:
- **Kerr (rotating)** — accretion disk, spiralling motes, lensed arcs, tangential
  spin force on anything caught in the pull.
- **Schwarzschild (stationary)** — bare shadow and photon ring only, motes fall
  straight in cold, no spin force.

### Physics
Rigidbodies inside `PullRadius` get pulled toward the well, force scaled by
`PullForce` and an exponential falloff (`PullFalloff`) that concentrates pull
near the center. A slight `PullUpward` bias keeps the hole from just yanking
everything into the floor. Rotating holes add a tangential `SpinForce` around
the disk normal. Anything that crosses into the visible disk (a fraction of
the rendered disk radius, set by `AccretionThreshold`) gets crushed and torn
with extra impulse and torque (`AccretionForce`). The player's own ragdoll is
excluded. Objects heavier than `MassLimit` are ignored so you can't try to
suck in something that should never move.

### Visuals
Everything is built at runtime from procedural ring meshes — no textures.
Shadow sphere size (`CoreScale`), disk inner/outer radius relative to the
shadow (`DiskInnerScale` / `DiskOuterScale`), disk tilt randomized per hole
within `DiskInclination`, Doppler beaming (`DopplerStrength`), a spinning
swirl overlay, lensed arcs above and below the shadow, and infalling motes
(`MoteCount`, streaked along their velocity by `MoteStreak`) that spiral in
over a few seconds. Kerr and Schwarzschild have independent brightness, glow,
sky-darkening, and RGB tint knobs, since a stationary hole with no disk needs
a different visual balance than a spinning one. Most emissive layers push
brightness over 1 (`*EmissionBoost`) and rely on HDR + bloom to read — see
[FruitLib's VFX notes](../5_FruitLib) if bloom isn't visibly kicking in.

### Debug
`DebugLevel` 1 logs deploy/collapse events, 2 adds verbose per-frame physics
logs. `DebugDrawRadius` draws a wireframe sphere at the pull radius. **F9**
dumps every currently loaded shader name to `SingularityShaderDump.txt` next
to the DLL — useful when hunting for a shader by name.

### HUD & Perf
A HUD panel shows the deploy key, current next-spawn type, and while any hole
is active: count, affected body count, and pull radius/force. FruitLib's perf
monitor (**F11**) gets two extra counters: active singularities and total
affected rigidbodies across all of them.

---

## How to Install
1. Install [FruitLib](../5_FruitLib) first — Singularity won't start without it.
2. Drag **Singularity.dll** into your `Mods/` folder.
3. Run the game — `SingularityConfig.ini` appears next to the DLL on first launch.

## How to Update
1. Drop in the new DLL — the config is rewritten on load with any new fields
   added and stale ones removed.

---

## Controls (Defaults)

| Key | Action |
|-----|--------|
| R | Deploy singularity at aim point |
| F5 | Toggle next-deploy type (Kerr / Schwarzschild) |
| F9 | Dump loaded shader names to `SingularityShaderDump.txt` |

Remap in `SingularityConfig.ini`, or live in FruitLib's menu.

---

## Config

Every parameter is in a sectioned, documented `.ini` file — Controls, Physics,
Behaviour, Visuals, Disk (Kerr only), Kerr, Schwarzschild, and Debug. Each
field's `.ini` comment explains what it does; the in-game FruitLib menu
mirrors the same categories.
