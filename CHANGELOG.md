# Changelog

## Unreleased - Town of Us: Mira 1.7.3

### Compatibility
- Built against Town of Us: Mira 1.7.3, MiraAPI 0.5.0 and Among Us 18.0.x (game libraries 2026.8.18).
- Role settings now live under Roles and modifier amount/chance under Modifiers again (MiraAPI 0.5.0 only routes groups by `ParentMenu`; role groups use `AbstractRoleOptionGroup`).
- Extension strings load into MiraAPI's locale tables (TOU 1.7.3 reads all text through them); the old `\%nl\%`/`\%color\%` markup is converted on load, and "Use Polish" with "Translate Role Names" off keeps English role names.
- Extension roles have their own translation ids, so the vanilla role menus, intro and causes of death show their names instead of raw keys.
- Death details use TOU 1.7.3's `GameHistory` (the DeathHandlerModifier is gone); meeting code follows the new `PlayerVoteArea.PlayerId` / `MeetingStates` API.
- Every extension Impostor and Neutral role has a crew counterpart for the Imitator.
- Patches whose targets moved or changed fixed: Time Lord revive filter (wrong list type), Plumber/Burrower vent target (abstract method), Trapper is renamed to Revealer through its 1.7.3 strings.

### Icenberg (new Neutral Killing role)
- Kill, Freeze (any living player cannot move, use abilities or consoles, open the map, vent or report) and Blizzard (everyone else slowed).
- Frozen and blizzard screen overlays; freezing never takes away movement that something else is holding.
- Wins alone like the other neutral killers; full English and Polish text; own cause of death.

### Fixes
- Vision: the shadows were switched off for a living player whenever the camera was a little wider than usual; ghosts got the darkness back after every meeting.
- Drunk: the extension's Drunk is now "Wasted" (PL "Nawalony") with its own icon and options, next to TOU's new Drunk, and never cancels TOU's control inversion out.
- Baker: each Baker counts only its own bread and turns into Famine on its own; a Cleric or Doctor cleansing the last bread holder unlocks Famine at once; Famine only blocks attacks, not every targeted ability; the host unlocks Famine whoever it plays.
- Foreteller hints and the Lawyer's team title read the TOU 1.7.3 strings again.

## 1.4.4 - Draft Visual Refinements & Lobby Fixes

### Draft Mode Visuals
- Restored the shimmering/blinking text effect for the "Draft Mode" title and the "Enabled" / "Disabled" status labels in the lobby.
- Optimized card scaling and vertical spacing for draft setups with many roles (7-10 roles), ensuring the Random button stays higher up and leaves space for the tooltip.
- Moved the draft role tooltip (descriptions) back to the right panel under the roles. The tooltip is now bottom-anchored, allowing descriptions to expand upwards and preventing any text from getting cut off at the bottom of the screen.
- Set a custom pastel-lavender text color for Impostor roles on their card labels, providing high contrast and readability over the dark red backgrounds.

### Lobby Fixes
- Fixed a lobby button duplication bug where the Map and Friends List buttons were being forced visible in the dropship.
- Refactored draft overlay cleanup so the lobby buttons (Settings, Chat, Wiki, PlayerCounter, and top-right elements) are naturally covered by the draft overlay and cleanly restored once the draft finishes or is cancelled.
- Guaranteed the Custom Cancel button remains persistently visible and clickable for the host during the draft.

## 1.4.3 - Perfect Comms API Integration

### Perfect Comms

- Moved the custom voice settings from the old Perfect Comms fork into this extension through the official Perfect Comms v3 API.
- Added a `ToU:Ch**owe` Perfect Comms tab with role-colored options for Impostor, Neutral, and Crewmate voice behavior.
- Added team radios for Pelican belly chat, Recruit/Jackal, Lawyer/Client, Apocalypse, and Spirit Master ghost link.
- Added local listener muffles for Evoker blindness and Injector/Doctor negative effects.
- Added hidden-player muting for invisibility-style roles and modifiers.

### Fixes

- Added Voodoo Master voice options, including mute carry-over for the next round during gameplay.
- Cleared active Doppelganger disguises when meetings start so the wrong head no longer appears to talk.
- Switched the Perfect Comms reference to use installed Perfect Comms instead of the old custom fork.

## 1.4.2 - Draft Balance & Role Polish

### Draft Mode

- Added `Exclude Previous Game Roles`, an optional setting that blocks roles picked in the previous draft from the next draft only.
- Role List now prioritizes roles set to `100%` chance in matching buckets before filling the remaining offers.
- Randomized Role List slot order so the same player/slot is not stuck with the same faction every game.
- Old Draft mode now shuffles the full player order directly instead of front-loading special factions.
- Streak reduction now supports both previous Impostors and previous Neutral Killing players.
- Added Draft local settings for start/end alerts and starting draft music muted.
- Draft lobbies now lock by default while draft is active.

### Voodoo Master

- Voodoo mute now plays a Blackmailer-style meeting intro for the muted player, including self-report timing fixes.
- Only the muted player knows they are muted; other players no longer get a Blackmailer-style overlay.
- Confuse now uses Herbalist-style grey scrambled appearances instead of reversed movement.
- Voodoo symbols are visible only to Impostors and ghosts.
- Cleaned up option names, infinite uses display, role tab text, and wiki ability descriptions.

### Tavern Keeper

- Hangover now blocks killer actions and custom ability buttons more reliably.
- Cleaned up the role tab, highlighted the current drinking target, and switched alerts to yellow role-icon notifications.
- Rewrote Tavern Keeper descriptions and target alerts for clearer protection/ability-block wording.

### Fixes & Polish

- Joker clones now camouflage during Camouflage Comms and restore correctly after comms are fixed.
- Innocent win alerts now appear only when Innocent actually wins and only for Leaves in Victory / Haunt outcomes.
- Inverter Disorient now applies inverted movement only for the disorient duration.
- Fixed the map outline local setting not applying correctly for ghosts.
