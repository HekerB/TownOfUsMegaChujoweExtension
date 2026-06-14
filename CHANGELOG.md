# Changelog

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
