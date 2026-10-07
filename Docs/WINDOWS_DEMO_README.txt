RECLAMATION - WINDOWS COMBAT DEMO

Extract the entire ZIP to a folder, then double-click Play Demo.cmd.
Keep the executable, ReclamationCombatDemo_Data, UnityPlayer.dll,
MonoBleedingEdge and the other supplied files together.
Unity Editor is not required to play.

The demo opens in Squad. Choose scenario (top-right) switches between
the existing combat labs, including Duel, Hulk, Weapons and Company.
The player uses the PF_SampleFace Sidekick character. Companions keep their
existing visuals. LimbDamage keeps its specialized articulated player rig.

CONTROLS
WASD: move                    Left mouse: light attack
E: heavy attack               Space: directional dodge
Ctrl: block                   Shift: sprint
Right mouse: orbit camera     Tab: target lock
Q: change target              X: change weapon
B: equipment                  H: show/hide help
Esc: pause/resume             R: reset scenario
Alt+F4: close the demo

Click the game window, then press Esc if switching windows paused it.
Squad: 1-4 issue squad orders; read the on-screen labels.
Company: 1-3 select platoons; 4 recalls all; G opens the tactical map.
For the expanded Company test, select Company, close the scenario menu,
click Region: compact (reset to expanded), then Plan assault.

This is a Development build for local playtesting, not a release build.
No persistence or save-game workflow has been added.
Report the scenario, action, observed behavior, and preferably a short video.

REBUILD FROM THE PROJECT
Save and close Unity, then run from the Reclamation2 repository:
powershell -NoProfile -File .\Tools\Build-WindowsDemo.ps1
Output is a new timestamped folder under Builds\WindowsCombatDemo.
Alternatively use Reclamation > Build > Windows Combat Demo in the editor.
The build tool explicitly includes Assets/Scenes/CombatDemo.unity;
the existing global Build Profiles scene list is not changed.

AUTOMATED PACKAGED-PLAYER CHECK
ReclamationCombatDemo.exe -screen-fullscreen 0 -screen-width 1440 -screen-height 960 --combat-demo-smoke -logFile smoke.log
The explicit smoke flag cycles all 11 scenarios, checks initialization,
materials, input-device availability, reset, pause and Company plan/recall,
then exits. Success requires exit 0 and COMBAT_DEMO_SMOKE_PASSED in the log.
This does not replace hands-on keyboard/mouse and combat-feel testing.

Smoke screenshots render the game camera offscreen; they do not validate the HUD.
