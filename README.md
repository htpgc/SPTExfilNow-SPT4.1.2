# SPTExfilNow - SPT 4.1.2 Compatibility Adaptation

A compatibility adaptation of [SPTExfilNow](https://github.com/ragnaroks/SPTExfilNow) by **ragnaroks**, updated for **SPT 4.1.2**.

## Credits

- **Original author:** ragnaroks
- **Original project:** https://github.com/ragnaroks/SPTExfilNow
- **Original mod page:** https://sp-mod.com/mod/2059/sptexfilnow
- **SPT 4.1.2 compatibility adaptation:** HTPGC
- **AI-assisted development/documentation:** ChatGPT
- **License:** GNU Affero General Public License v3.0 (AGPL-3.0)

## Features

Press **Left Ctrl + \\** during a raid to extract immediately from the player's current location.

Tested on **SPT 4.1.2** with:

- PMC extraction
- Scav extraction
- Loot persistence
- Quest item persistence
- Quest progress
- Experience settlement

## Behavioral difference from the original

This compatibility adaptation has one minor behavioral difference from the original SPTExfilNow:

- The original version randomly selects one active extraction point.
- This SPT 4.1.2 adaptation selects the first active extraction point returned by the game.

This only changes which extraction point is used for the settlement name. It does not change the immediate extraction function or the normal raid settlement flow.

## Installation

Download the release archive and merge its `BepInEx` folder into your SPT root folder.

The DLL should end up at:

```text
BepInEx/plugins/SPTExfilNow/SPTExfilNow.dll
```

## Usage

Default shortcut:

```text
Left Ctrl + \
```

Press the shortcut during a raid to trigger extraction through the game's normal raid-ending flow.

## Building from source

The project targets `netstandard2.1`.

Example:

```powershell
dotnet build -c Release -p:SPTPath="D:\Your\SPT"
```

`SPTPath` must point to the SPT root directory containing `EscapeFromTarkov_Data` and `BepInEx`.

## Modification notice

This repository contains a modified version of SPTExfilNow.

- **Original author:** ragnaroks
- **Compatibility adaptation:** HTPGC
- **Target compatibility:** SPT 4.1.2

The original concept and core functionality remain attributable to ragnaroks.

## Permission

Publication permission was requested from the original author through GitHub Issues:

https://github.com/ragnaroks/SPTExfilNow/issues/1

The original author replied that the adaptation may be used and published as long as the current repository license is followed.

## License

The original SPTExfilNow repository is licensed under **GNU Affero General Public License v3.0 (AGPL-3.0)**.

This modified version continues to be distributed under **AGPL-3.0**. See [LICENSE](LICENSE) for the complete license text.
