# Reclamation prototype setup

## Run the autonomous hauling lab

1. Open the project with Unity `6000.3.24f1`.
2. Wait for script compilation to finish.
3. Choose **Reclamation > Create Autonomous Hauling Lab** from the Unity menu.
4. Open `Assets/Scenes/AutonomousHaulingLab.unity` if Unity does not switch to it automatically.
5. Press **Play**.

Three survivors choose wood piles using policy priority and travel distance. A central reservation registry prevents two survivors from claiming the same pile. The overlay shows each survivor's current state and decision explanation.

The generated scene is a disposable test laboratory. Run the menu command again whenever a clean copy is useful.

## Run tests

1. Choose **Window > General > Test Runner**.
2. Select **EditMode**.
3. Choose **Run All**.

The initial suite covers exclusive reservations, owner-only cancellation, score ordering, and resource underflow protection.

## Current boundary

This milestone proves job selection, reservation, hauling, delivery, and debugging visibility. It does not yet include player controls, needs, construction, saving, combat, or final art.
