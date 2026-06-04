// DeathHandlerInitializationPatch REMOVED (2026-06-04)
// Previously this pre-added DeathHandlerModifier to all players at game start.
// In TOU-Mira 1.6.2-dev, CoWriteDeathHandler does a `yield break` when the
// modifier already exists — without saving the new CauseOfDeath.
// This caused everyone to show "Suicide" (the default CauseOfDeath value).
// TOU-Mira now handles modifier creation via UpdateDeathHandlerImmediate before kill,
// so pre-adding it here is no longer needed and actively harmful.

namespace TouMegaChujoweExtension.Patches.BugFixes;