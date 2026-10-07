# Windows combat demo

Windows x64 Development build, Unity 6000.3.24f1. The demo starts directly in Squad with the existing scenario selector, combat controls and character presentation. This milestone adds a saved entry scene and repeatable packaging/validation; it does not retune combat.

## Play

Extract the entire ZIP and run `Play Demo.cmd`. It opens a 1440 x 960 window. Keep all supplied runtime files together. Unity Editor is not required. Alt+F4 closes the game; Esc retains its existing pause behavior. See `WINDOWS_DEMO_README.txt` for controls and Company testing steps.

## Rebuild

Save and close the project in Unity, then run from the repository root:

```powershell
powershell -NoProfile -File .\Tools\Build-WindowsDemo.ps1
powershell -NoProfile -File .\Tools\Test-WindowsDemo.ps1 -BuildDirectory "<generated build folder>"
```

The build defaults to a fresh timestamped folder under `Builds/WindowsCombatDemo`. `-OutputDirectory` chooses a new destination. Existing directories are rejected to prevent stale outputs. Both scripts reject failed results; the smoke runner also has a 120-second timeout and checks all eleven scenario markers and the captured image.

Editor alternative: **Reclamation > Build > Windows Combat Demo**, which builds to `Builds/WindowsCombatDemo/ReclamationCombatDemo.exe` (the executable can be launched directly). Use the PowerShell wrapper for timestamped outputs, launcher and README. The builder creates `Assets/Scenes/CombatDemo.unity` only if it is missing and preserves subsequent scene edits. Its explicit scene list leaves the global Build Profiles list unchanged. Ordinary File > Build still uses the existing global scene configuration.

## Implementation

- `Assets/Reclamation/Editor/CombatDemoBuild.cs` and `.meta`: Windows builder and first-time scene creation.
- `Assets/Reclamation/Runtime/Outbreak/CombatDemoSmoke.cs` and `.meta`: explicit command-line smoke mode and serialized shader references so runtime Shader.Find assets are included. Normal play exits this component's coroutine immediately without touching gameplay.
- `Assets/Scenes/CombatDemo.unity` and `.meta`: independent Squad entry scene with the existing BlightCombatLab.
- `Tools/Build-WindowsDemo.ps1`, `Tools/Test-WindowsDemo.ps1`: repeatable build and packaged-player validation.
- Agent/project/test documentation and player README updated. The preceding Company repair report now records Tom's successful expanded-region follow-up.

## Verified

The final executable built successfully with Unity exit 0. Its packaged-player smoke passed all 11 scenarios, player/camera creation, keyboard/mouse device availability, material/shader support, scenario resets, pause, expanded Company plan and recall. It exited 0 with COMBAT_DEMO_SMOKE_PASSED. The game-camera image was inspected separately for missing or pink materials. The smoke uses an offscreen camera render because screen capture from the hidden automated window failed; it does not verify IMGUI, physical input or subjective combat feel.

The existing 116 EditMode and 205 PlayMode passes are the preceding Company-repair baseline, not a rerun in this build milestone. The player build and packaged smoke are the validation specific to this change. Manual playtesting of the standalone window remains the next user check.

Build warnings remain, primarily package shader/compiler warnings (including Sentis); no package upgrades or vendor edits were made. This is a local Development demo, not an optimized release or installer. The existing resources bring dependencies into the package that may be trimmed in a separate measured packaging task.

Evidence is in this Codex task's `outputs/windows-demo`: final `build-04/build.log`, `BUILD-RESULT.txt`, `smoke-03/player.log`, `summary.json`, `squad.png`, and `preservation-audit.json`. Earlier failed build/capture attempts are retained separately; only build-04 is packaged.

Unity player builds can serialize render-pipeline defaults and stage Sidekick runtime database assets. This run restored the three changed URP assets and ProjectSettings byte-for-byte, and backed up/removed only newly generated Sidekick database files. Future builders should inspect their working tree after building.

## Sidekick player follow-up

The current scene now uses the approved PF_SampleFace player variant and the packaged smoke also verifies bridge installation across compatible scenarios and its LimbDamage fallback. The build history above describes the earlier prototype package; current integration results and the updated package are recorded in `Docs/SIDEKICK_PLAYER_INTEGRATION.md`.
