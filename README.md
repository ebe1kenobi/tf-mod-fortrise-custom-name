# CustomName

Gives every player a **name**, drawn above their archer in game and reused by the
other mods (stats, tournament, awards). Names are typed on a virtual keyboard and
kept between sessions.

A mod for **FortRise 5** (>= 5.3.3). The FortRise 4 version (`tf-mod-fortrise-custom-name`) is no longer maintained: fixes and new features only land in this repository.

## Installation

1. Install FortRise 5 and start the game through `FortRise.exe`.
2. Copy `release/customname` (or the shipped folder) into `<TowerFall>/FortRise/Mods/`.

Settings are under **Options > Mods > CustomName**.
Data and log files live in `<TowerFall>/FortRise/Saves/CustomName/` and `<TowerFall>/FortRise/Logs/`.

## Usage

On the archer select screen, **up + the arrows button** opens the virtual keyboard
to enter a new name. The arrows button alone cycles through the names already
stored.

### Virtual keyboard

| Input | Effect |
|-------|--------|
| Physical keyboard | direct typing of letters, digits and `. ' - _ ? ! : ( ) \ /` |
| Backspace | delete one character |
| Enter | confirm |
| Escape | cancel |
| Controller: directions | move around the grid (repeats when held) |
| Controller: A | type the highlighted letter |
| Controller: RB | delete |
| Controller: Start | confirm |
| Controller: B | cancel |

Typing goes through the system text input, so your **keyboard layout is honoured**
(AZERTY included, underscore on the 8 key and all). The game's menu bindings are
disabled while typing, so entering a name can never close the screen by accident.

A confirmed name is written straight to
`<TowerFall>/FortRise/Saves/CustomName/CustomName.playerName.json`.

## API for other mods

The mod publishes an interface retrievable through interop:

```csharp
CustomNameImport.Api = context.Interop.GetApi<ICustomNameModApi>("CustomName");
```

It exposes `GetPlayerName(playerIndex)` and `SetPlayerName(playerIndex, name)`. The
dependency is optional: without this mod, the others fall back to "P1" to "P8".

## Build / deployment

| Script | Purpose |
|--------|---------|
| `script/release.bat` | build, then assemble into `release/` |
| `script/deploy.bat` | copy `release/` into the TowerFall `Mods` folder |
| `script/release_deploy.bat` | both, one after the other |

Paths (game folder, module name) are set in `script/config.bat`.
