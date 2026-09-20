# Neighborhood camera update

Apply after the human-character patch. Open PatientZeroLab or LivingNeighborhoodLab,
exit Play Mode, choose **Reclamation > Upgrade Open Scene with Camera Controls**,
and save the scene. The command is repeatable and undoable. Scene regeneration is
unnecessary. Newly generated neighborhood/outbreak labs install controls automatically.

Click the Game view to give it keyboard focus.

| Input | Action |
| --- | --- |
| Scroll wheel | Zoom |
| WASD / arrow keys | Pan; also release follow |
| Middle mouse drag | Pan; also release follow |
| Right mouse drag | Orbit and tilt |
| Q / E | Rotate |
| Tab / Shift+Tab | Follow next / previous active character |
| Escape | Stop following |
| Home / Overview button | Restore original camera position, orientation, and zoom |
| Follow button beside a person | Follow that person |

Camera movement uses real time and remains available while the simulation is paused.
Following ends when the target is deactivated. The camera retains its position.
Zoom and rotation keep following active. The character list is captured at scene startup,
which covers the current labs; later dynamically spawned populations need registration.

Collapse the outbreak or neighborhood panel to retain just its header, clock, and
speed controls. Expand it to inspect civilians and issue commands. Scroll, drag, and
orbit do not affect the camera while the pointer is over these panels or camera controls.
This iteration is intended for the neighborhood and outbreak labs. It does not change
shelter-placement controls or automatically install a camera in the construction lab.

## Verify in Unity

Run 30 EditMode and 18 PlayMode tests. Three camera regression tests cover follow while
paused, deactivated targets, and overview reset. Compilation and runtime tests cannot
be executed in the patch-building environment.

Manually check zoom, pan, rotation, follow, pause, collapse/expand, and neutralizing a
followed person. Scroll the expanded outbreak panel and confirm that the world does
not zoom. Confirm the saved upgrade persists after reopening the scene.
