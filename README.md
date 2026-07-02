# Raylib Halo Clone Prototype

A Halo-inspired first-person shooter prototype built with C# and [Raylib-cs](https://github.com/ChrisDill/Raylib-cs). The project is a compact sci-fi ship interior combat sandbox focused on responsive movement, readable HUD feedback, basic enemy encounters, objectives, switches, doors, and a generic interactable pickup/object foundation.

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
- **H**: Use a medkit to heal health when damaged
- **Q**: Use lethal equipment placeholder
- **C**: Use special equipment placeholder
- **1**: Equip Primary slot
- **2**: Equip Secondary slot
- **3**: Equip Sidearm slot
- **E**: Pickup or interact with the object under the crosshair when close enough
- **G**: Drop the equipped weapon into the world
- **Enter**: Restart after victory or defeat
- **Esc / window close**: Quit

## Current Features

- **FPS movement**: first-person WASD movement, mouse look, sprinting, and jumping.
- **Collision and ship traversal**: player collision against arena bounds, ship walls, doors, cover, crates, bulkheads, and raised floor plates.
- **Combat**: hitscan rifle, shotgun, and pistol firing with enemy hit detection, tracer feedback, muzzle flash, and recoil/kick.
- **Weapon ammo and reload**: magazine ammo, reserve ammo, reload timing, safe unarmed input handling, and HUD reload status.
- **Equipment resources**: stackable medkits, lethal equipment storage, and special equipment storage with HUD counts and placeholder use feedback.
- **Player health and shield**: health damage, rechargeable shield, shield break feedback, and damage overlays.
- **Enemy AI and combat**: enemies detect the player, move, strafe, check line of sight, aim, attack, take damage, and flash when hit.
- **HUD**: crosshair, FPS/debug info, position/speed, objective text, enemy count, shield/health bars, weapon/ammo panel, reload status, hit marker, damage overlays, interaction prompts, and victory/defeat messaging.
- **Objectives**: breach into the derelict ship, restore lights, open security doors, clear the cargo room, activate the engineering extraction console, then move into the extraction zone.
- **Switches, doors, and lights**: light switch, security door switch, powered door switch, extraction switch, doors, lights, and light fixtures are all represented in the level.
- **Extraction, victory, defeat, and reset**: reaching the active extraction zone after completing objectives triggers victory; death triggers defeat; Enter resets the level.
- **Interactable pickup system**: switches, pickups, and dropped objects use a shared interactable model with position, bounds, display text, active/consumed state, rendering, and interaction behavior.
- **Crosshair interactions**: interactions are selected by camera position and look direction, require the object to be under the crosshair, and use a short activation distance.
- **Medkits**: medkit pickups add to a stack instead of healing instantly; medkits heal health only when used and never affect shields.
- **Weapon pickups and dropping**: world weapon pickups can be collected with E, and the current weapon can be dropped with G as a new world object. Dropping removes the weapon from its equipment slot instead of regenerating a replacement rifle.
- **Object overlap prevention**: placed and dropped objects try deterministic nearby offsets when the preferred spawn position overlaps another pickup, interactable, or collision object.

## Equipment System

The equipment foundation remains intentionally lightweight without a full inventory UI or Resident Evil-style inventory management. The player has **Primary**, **Secondary**, and **Sidearm** weapon slots, stackable **Medkits**, a **Lethal** equipment slot, and a **Special** equipment slot. The current weapon is derived from the selected equipped weapon slot. Empty weapon, lethal, and special slots are allowed, so selecting or using empty slots safely does nothing and shows HUD feedback where appropriate.

Current weapon content:

- **Primary slot**: **MA5B Rifle**. Automatic, balanced damage, range, magazine size, and reserve ammo.
- **Secondary slot**: **M90 Shotgun**. Semi-auto, higher damage, shorter range, slower fire rate, and smaller magazine. The test level includes a shotgun pickup.
- **Sidearm slot**: **M6D Pistol**. Semi-auto, lower damage than the rifle, moderate range, slower fire cadence, and sidearm ammo.

Additional equipment:

- **Medkit stack**: starts at **1/3**. Medkit pickups can be collected at full health while the stack is not full. Press **H** to heal health only; using a medkit at full health does not consume it.
- **Lethal equipment slot**: placeholder foundation with **0/2** by default and no grenade physics yet. Press **Q** to attempt use; empty lethal storage shows `NO LETHAL AVAILABLE`.
- **Special equipment slot**: placeholder foundation with **0/1** by default and no special behaviour yet. Press **C** to attempt use; empty special storage shows `NO SPECIAL AVAILABLE`.

The default mission loadout is an **MA5B Rifle** in Primary, an empty Secondary slot, an **M6D Pistol** in Sidearm, **1/3 Medkits**, **0/2 Lethals**, and **0/1 Special**. Weapon pickups go into their category-matched slot. If that slot is empty, the pickup is stored and equipped there; if it is full, the pickup replaces that slot and drops the old weapon nearby using the existing no-overlap placement rules. Restarting after victory or defeat restores the default mission loadout, so weapons and equipment used or dropped during a run reset only when the level restarts.

## Level: Derelict Space Hulk

The current playable level is a compact UNSC-style ship interior built entirely from Raylib box geometry. The player starts in a small breached entry bay, pushes through a narrow cover-lined corridor, restores lighting at a wall switch, opens a security bulkhead, fights through a cargo/storage room full of crate cover, collects shotgun and medkit pickups, reaches the engineering/control room, activates extraction, then returns to or proceeds into the marked extraction zone.

## Gameplay Loop

1. **Breach and power up**: Start in the entry bay, move down the corridor, and activate the light switch.
2. **Open the ship**: Use switches with E to open the security and engineering doors while keeping the existing objective/door systems intact.
3. **Clear cargo storage**: Use cover, shield recharge windows, and weapon pickups to defeat enemies in the storage room.
4. **Activate extraction**: Once all targets are down, use the engineering/control room extraction console.
5. **Reach extraction zone**: Move fully into the active extraction zone to complete the match and trigger victory.

## Next Steps

- Multiple weapon types with meaningful stat differences and switching rules
- Ammo pickups and richer resource placement
- Additional enemy varieties and encounter roles
- Proper data-driven level loading
- Audio for weapons, shields, UI, doors, switches, and ambience
- UI polish for prompts, objective tracking, and menus
- Real models/assets for weapons, enemies, pickups, and level geometry
- Save/checkpoint system for longer levels
