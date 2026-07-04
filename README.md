# WFollowBot

A **GameHelper** plugin that controls up to 2 follower characters in **Path of Exile 2** by emulating an Xbox 360 controller.

## How it works

Reads game memory via GameHelper, pathfinds (A\*) using terrain data, and controls characters through a virtual gamepad.

## Manual gamepad control

Keyboard input can be forwarded to the virtual controller, letting you **manually control the follower** as if using a real Xbox 360 gamepad. 
To enable it, open the plugin's ImGui overlay and check **Enable Keyboard** under the **Keyboard** tree node per follower.

Default bindings: `WASD` — left stick, `Arrow keys` — D-pad, `J`/`K`/`H`/`U` — A/B/X/Y, `Q`/`E` — LB/RB, `Z`/`C` — LT/RT, `Tab`/`Enter` — Back/Start, `Space` — R3, `LShift` — L3, `F` — Guide.

Bindings are defined in `Settings/KeyboardLayout.cs` and are not editable via the UI.

## Adding a new action

1. Add a new value to the `ActionState` enum in `Managers/StateManager.cs`
2. Add a transition in `StateManager.Update()` that calls `SetState(YourNewState)` when the condition is met
3. Create a class implementing `IAction` in `Action/STD/` (always active) or `Action/Simple/` (per skill set). Set `State` and `SkillSetName`, implement `Execute(PlayerContext)`
4. Done — auto-discovered via reflection, no manual registration needed

## Dependencies

- **GameHelper** — PoE memory reading framework (not included in this repo)
- **Nefarius.ViGEm.Client** — Xbox 360 controller emulation library
- **ViGEmBus** — kernel driver, required for operation

## Known issues

- **Followers run in opposite directions** — try swapping Player 1 and Player 2
- **Followers don't always click portals/transitions** — no joystick targeting data available
- **Followers lose the path when you're near the edge of the terrain**
- **Joystick direction display is inverted** (up/down swapped in the overlay)

And plenty more — but the project is abandoned.

## Status

Project abandoned due to the end of the Path of Exile 2 league.

For the plugin to work properly, proper gamepad targeting info (how the game selects targets with the right stick / shoulder buttons) needs to be read from memory. Implementing that requires reversing those memory structures — which I simply couldn't be bothered to do.

```
    █████╗ ███╗   ███╗███╗   ██╗██╗   ██╗ █████╗ ███╗   ███╗
   ██╔══██╗████╗ ████║████╗  ██║╚██╗ ██╔╝██╔══██╗████╗ ████║
   ███████║██╔████╔██║██╔██╗ ██║ ╚████╔╝ ███████║██╔████╔██║
   ██╔══██║██║╚██╔╝██║██║╚██╗██║  ╚██╔╝  ██╔══██║██║╚██╔╝██║
   ██║  ██║██║ ╚═╝ ██║██║ ╚████║   ██║   ██║  ██║██║ ╚═╝ ██║
   ╚═╝  ╚═╝╚═╝     ╚═╝╚═╝  ╚═══╝   ╚═╝   ╚═╝  ╚═╝╚═╝     ╚═╝
```
