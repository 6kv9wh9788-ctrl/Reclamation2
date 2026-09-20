# Survivor progression and consolidated validation lab

Apply after the brute-tuning checkpoint. This checkpoint adds bounded survivor progression and replaces the four separate combat-lab creation commands with one Systems Validation Lab.

## Create and clean up the lab

Outside Play Mode:

1. Choose **Reclamation > Validation > Create or Replace Systems Validation Lab**.
2. Open/Play `Assets/Scenes/SystemsValidationLab.unity` if it is not already open.
3. Use the upper-right panel to select one of five scenarios: civilian vs ordinary, veteran vs three, bite-rescue squad, three civilians vs brute, or rank comparison.
4. Each scenario is fresh the first time it is selected in a Play session. Stop/restart Play to reset all five.
5. After confirming the consolidated lab works, choose **Reclamation > Validation > Remove Legacy Generated Combat Labs**. The confirmation-scoped cleanup removes only scene/navigation assets whose filenames begin `CivilianVsOne`, `VeteranVsThree`, `ProtectCivilian`, or `ThreeCiviliansVsBrute`. Patient Zero, neighborhood, hauling, and shelter scenes are not touched.

The validation lab uses session-only progression and cannot erase or modify the persistent Patient Zero records. Orange, purple, and blue shirts represent Civilian, Trained, and Veteran starting ranks.

## XP and ranks

| Event | XP |
| --- | ---: |
| Effective damaging strike/shove | 3 |
| Break own grab | 10 |
| Interrupt an active bite | 15 |
| Defeat ordinary threat | 20 |
| Defeat brute | 35 |
| Survive an engagement | 8 |
| First refuge/quarantine arrival | 10 |
| First build of each perimeter section | 8 |
| First repair of each perimeter section | 4 |

Movement, missed attacks, waiting, and repeated completion of the same named construction/rescue objective award no XP. Combat XP remains repeatable because it requires real damage or danger; finite enemy health bounds strike awards.

- Civilian: 0–99 XP
- Trained: 100–299 XP
- Veteran: 300+ XP

Attributes interpolate toward the established veteran preset and remain capped: Strength 12, Dexterity/Agility/Intelligence 9, Speed/Endurance 8. Rank never increases human health above 100. Progression improves damage, windup, dodge cost/reaction, stamina, movement, and threat decisions through the existing attribute system.

## Persistence and reset

Patient Zero survivors are keyed by their display names and saved through Unity PlayerPrefs after each award. The record survives Play restarts. The outbreak panel shows rank, total XP, next threshold, and an attributed session summary.

The bottom of a persistent outbreak panel contains **Reset survivor progression**. It requires a second confirmation click and offers Cancel. This deletes all survivor XP/objective records, then restores the current scene's authored starting XP. The button is hidden in non-persistent validation labs.

Names currently act as identity. Do not give two persistent survivors the same display name; stable generated IDs are future save-system work.

## Verification

Run EditMode and PlayMode suites. New tests cover rank boundaries/caps, technique growth without health inflation, meaningful-versus-failed action XP, attributed rescue/victory summaries, and validation-scenario switching. Existing combat, pacing, awareness, perimeter, and safe-zone tests remain the regression suite.

The patch-authoring environment has no Unity runtime or C# compiler. Static source/metadata checks and clean patch application are verified there; Unity tests, persistence across Play restarts, and UI layout need editor verification.
