# Kotama Academy Citadel — Numpad Bind Patch (IL2CPP)

This repo contains a BepInEx IL2CPP plugin that removes the game's “numpad not bindable” restriction and makes numpad bindings actually work in-game, while keeping the keybind UI usable.

## Features

- Allows binding numpad keys like `Numpad 0–9`, `Numpad * / + -`, etc.
- Allows binding mouse side buttons (typically `MB4` / `MB5`).
- Preserves the game's conflict/swap behavior (the mod keeps `PressTxt` as a logical key path like `<Keyboard>/numpad4`).
- Avoids the missing-sprite crash/stuck UI state when the game tries to render numpad icons.
- Shows a text fallback for numpad binds (e.g. `Numpad 4`) while keeping native keys in icon mode.

## Requirements

- Kotama Academy Citadel (Unity 2022.3 IL2CPP)
- BepInEx 6 (IL2CPP build). Tested with `BepInEx 6.0.0-be.752`.

## Install

1. Build the plugin DLL (see below), or use your locally built `Kotama.NumpadRebind.dll`.
2. Copy it to: `KotamaAcademyCitadel/BepInEx/plugins/Kotama.NumpadRebind.dll`
3. Launch the game.

## Build (recommended workflow)

This project references the game's `BepInEx/core` and `BepInEx/interop` assemblies via relative paths.

Simplest setup:

1. Clone this repo into your game folder so the hint paths resolve:
   - `...\KotamaAcademyCitadel\Modding\NumpadProbe\`
2. Build:
   - `dotnet build .\NumpadProbe\NumpadProbe.csproj -c Release`
3. The output DLL is:
   - `.\NumpadProbe\bin\Release\net6.0\NumpadProbe.dll`

In my local setup, the built DLL is deployed as:
- `KotamaAcademyCitadel/BepInEx/plugins/Kotama.NumpadRebind.dll`

## Notes

- Implementation notes and the “why” behind the patches live in `IMPLEMENTATION_MEMO.md`.
- This repo intentionally does not include any game files, BepInEx binaries, or build outputs.
