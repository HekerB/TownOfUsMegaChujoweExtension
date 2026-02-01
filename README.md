# Town of Us: Mira Roles Extension

An extension mod for [Town of Us: Mira](https://github.com/AU-Avengers/TOU-Mira) that adds new roles and modifiers.

## Features

This extension mod:
1. **Renames** the existing Trapper role (ground traps that reveal roles) to **Revealer**
2. **Renames** the existing Scavenger role (impostor killing role with targets) to **Bloodhound**
3. **Adds** a new **Trapper** role (vent traps that immobilize players)
4. **Adds** the **Forestaller** role (crewmate support role that disables sabotages)
5. **Adds** the **Wraith** role (impostor power role with dash and lantern abilities)
6. **Adds** the **Lawyer** role (neutral role that protects a client)
7. **Adds** the **Witch** role (impostor power role that curses players)
8. **Adds** the **Serial Killer** role (neutral killing role)
9. **Adds** the **Mirage** role (crewmate support role that places decoys)
10. **Adds** the **Hacker** role (impostor support role that downloads system information and jams comms)
11. **Adds** the **Injector** role (impostor support role that injects players with random effects)
12. **Adds** the **Charlatan** role (impostor support role that manipulates body reports)
13. **Adds** the **Scavenger** role (neutral evil role that eats dead bodies to win)
14. **Adds** the **Clueless** modifier

### Roles
- **Revealer** (Crewmate, renamed from Trapper): Place traps around the map to reveal roles of players who stay in them long enough.
- **Bloodhound** (Impostor Killing, renamed from Scavenger): Gets new targets after every kill and when the round starts. If they kill their target, they get a reduced kill cooldown, but if they don't, their cooldown is increased significantly.
- **Trapper** (Crewmate, new): Place traps on vents that immobilize players who use them.
- **Forestaller** (Crewmate Support): Complete all tasks to disable sabotages while alive. Revealed in meetings after completing all tasks.
- **Wraith** (Impostor Power): Dash ability increases movement speed by 75% for a short time. Lantern ability lets you place a hidden marker only you can see; reactivate it to teleport back and briefly turn invisible. If the Lantern expires before returning, it breaks and leaves permanent evidence visible to all players.
- **Lawyer** (Neutral Benign): Win by keeping your assigned client from being voted out. If your client gets voted out, you lose. Can object to votes during meetings to make players reconsider their votes.
- **Witch** (Impostor Power): Cast spells on players to curse them. Spellbound players are highlighted in the next meeting and die after a configured amount of meetings. If the Witch dies, gets exiled, or is guessed, all spellbound players survive.
- **Serial Killer** (Neutral Killing): Kill everyone to win alone. Can optionally kill players who are in vents with them, but loses the ability to vent for the rest of the game after doing so.
- **Mirage** (Crewmate Support): Place a decoy with the appearance of a chosen target (yourself or a random player). If any player interacts with the decoy, it disappears instantly and both the Mirage and the toucher receive a notification. Cannot be guessed if the decoy has the appearance of yourself.
- **Hacker** (Impostor Support): Download information from nearby equipment (Admin/Cams/Vitals/Door Log) to charge a portable device. Use the device anywhere to access the downloaded system. Jam disrupts information systems like comms being sabotaged, but emergency meetings can still be called. Gain jam charges from kills.
- **Injector** (Impostor Support): Inject non-impostor players with a syringe that applies a random effect after a delay. Effects can be negative (inverted controls, low vision, slowness, confusion, inability to vent/use/report, nausea, weakness) or positive (speed boost, vision boost, regeneration). Effect duration can be set to a specific time, last the entire round, or persist for the whole game. Starts with a limited number of uses and gains additional uses from kills.
- **Charlatan** (Impostor Support): Manipulate body reports to your advantage. Deceive allows you to report bodies you've killed from any distance for a limited time after killing. Conceal reduces the report range of nearby bodies, but requires you to stay near the body for the duration.
- **Scavenger** (Neutral Evil): Eat dead bodies to win alone. Must eat a configured number of bodies to win. Optionally, can use Scavenge to get arrows pointing to all corpses for a duration. If the win condition becomes impossible, the Scavenger becomes a configured role.

### Modifiers
- **Clueless** (Universal): Removes all task guidance (task list, task arrows/markers, and map task locations). Tasks still function normally and contribute to the task bar.

## Installation

1. Ensure you have [Town of Us: Mira](https://github.com/AU-Avengers/TOU-Mira) installed.
2. Build this project or download a release.
3. Place the compiled DLL in your `BepInEx/plugins/` folder.

## Building

1. Clone this repository.
2. Restore NuGet packages.
3. Build the solution in Visual Studio or using `dotnet build`.

## Requirements

- .NET 6.0
- Town of Us: Mira 1.5.0 or later
- MiraAPI 0.3.6 or later
- Reactor 2.5.0 or later

## License

This software is distributed under the GNU GPLv3 License.

## Copyright

This mod is not affiliated with Among Us or Innersloth LLC, and the content contained therein is not endorsed or otherwise sponsored by Innersloth LLC. Portions of the materials contained herein are property of Innersloth LLC.

© Innersloth LLC.