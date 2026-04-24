using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Buttons.Impostor;

namespace TouMegaChujoweExtension.Events.Impostor;

public static class KamikazeEvents
{
    // RoundStartEvent handles both GameStart and MeetingEnded in MiraAPI.
    // However, we moved the Usable logic to a dynamic property in KamikazeSuicideButton.cs
    // to avoid state management issues and compilation errors with missing event types.
}