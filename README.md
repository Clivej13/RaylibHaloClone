# Raylib Halo Clone Prototype

A Halo-inspired first-person shooter prototype built with C# and [Raylib-cs](https://github.com/ChrisDill/Raylib-cs). The project is a compact arena combat sandbox focused on responsive movement, readable HUD feedback, basic enemy encounters, objectives, switches, doors, and a generic interactable pickup/object foundation.

## Requirements

- .NET 8 SDK or newer
- A desktop environment capable of opening a Raylib window
- NuGet access for restoring `Raylib-cs` version `8.0.0`

## Build

```bash
dotnet restore
dotnet build -c Debug
```

## Run

```bash
dotnet run -c Debug
```

## Controls

- **Mouse**: Look around / aim crosshair
- **W/A/S/D**: Move
- **Left Shift**: Sprint
- **Space**: Jump
- **Left Mouse Button**: Fire the equipped weapon when armed
- **R**: Reload the equipped weapon when armed
- **1 / 2 / 3**: Switch equipped slot (Primary / Secondary / Sidearm)
- **E**: Interact with the object under the crosshair when close enough
- **G**: Drop the current weapon into the world
- **Enter**: Restart after victory or defeat
- **Esc / window close**: Quit

## Current Features

- **FPS movement**: first-person WASD movement, mouse look, sprinting, and jumping.
- **Collision and platforming**: player collision against arena bounds, walls, doors, cover, and a raised platforming route.
- **Combat**: hitscan rifle firing with enemy hit detection, tracer feedback, muzzle flash, and recoil/kick.
- **Weapon ammo and reload**: magazine ammo, reserve ammo, reload timing, safe unarmed input handling, and HUD reload status.
- **Player health and shield**: health damage, rechargeable shield, shield break feedback, and damage overlays.
- **Enemy AI and combat**: enemies detect the player, move, strafe, check line of sight, aim, attack, take damage, and flash when hit.
- **HUD**: crosshair, FPS/debug info, position/speed, objective text, enemy count, shield/health bars, weapon/ammo panel, reload status, hit marker, damage overlays, interaction prompts, and victory/defeat messaging.
- **Objectives**: eliminate all targets, activate the extraction switch, then move into the extraction zone.
- **Switches, doors, and lights**: light switch, security door switch, powered door switch, extraction switch, doors, lights, and light fixtures are all represented in the level.
- **Extraction, victory, defeat, and reset**: reaching the active extraction zone after completing objectives triggers victory; death triggers defeat; Enter resets the level.
- **Interactable pickup system**: switches, pickups, and dropped objects use a shared interactable model with position, bounds, display text, active/consumed state, rendering, and interaction behavior.
- **Crosshair interactions**: interactions are selected by camera position and look direction, require the object to be under the crosshair, and use a short activation distance.
- **Health packs**: health packs heal player health without affecting shields and are not consumed when health is already full.
- **Weapon pickups and dropping**: world weapon pickups can be collected with E, and the current weapon can be dropped with G as a new world object. Dropping removes the weapon from its equipment slot instead of regenerating a replacement rifle.
- **Object overlap prevention**: placed and dropped objects try deterministic nearby offsets when the preferred spawn position overlaps another pickup, interactable, or collision object.

## Equipment System

Phase 1 adds a minimal equipment foundation without a full inventory UI or Resident Evil-style inventory management. The player has **Primary**, **Secondary**, and **Sidearm** slots, and the current weapon is derived from the selected equipped slot. Empty slots are allowed, so selecting or dropping into an empty slot leaves the player **Unarmed**; firing and reloading safely do nothing while unarmed.

The default mission loadout is an **MA5B Rifle** in the Primary slot only. Secondary starts empty, and Sidearm is currently reserved for future sidearm categories. Weapon pickups fill Primary first, then Secondary; if both weapon slots are full, picking up another weapon swaps it with the currently equipped slot and drops the previous weapon into the world. Restarting after victory or defeat restores the default mission loadout, so weapons dropped during a run stay removed only for that run.

## Gameplay Loop

1. **Eliminate targets**: Use movement, cover, shield recharge windows, and the rifle to defeat all enemies in the arena.
2. **Use switches**: Aim at switches with the crosshair and press E when close enough to toggle lights or open doors.
3. **Activate extraction**: Once all targets are down, find and activate the extraction switch.
4. **Reach extraction zone**: Move fully into the active extraction zone to complete the match and trigger victory.

## Next Steps

- Multiple weapon types with meaningful stat differences and switching rules
- Ammo pickups and richer resource placement
- Additional enemy varieties and encounter roles
- Proper data-driven level loading
- Audio for weapons, shields, UI, doors, switches, and ambience
- UI polish for prompts, objective tracking, and menus
- Real models/assets for weapons, enemies, pickups, and level geometry
- Save/checkpoint system for longer levels
