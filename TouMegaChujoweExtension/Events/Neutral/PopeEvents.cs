using MiraAPI.Events;
using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Buttons.Neutral;
using TouMegaChujoweExtension.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using UnityEngine;

namespace TouMegaChujoweExtension.Events;

public static class PopeEvents
{
    public static void CheckCanonized(PlayerControl source, PlayerControl target)
    {
        if (!OptionGroupSingleton<PopeOptions>.Instance.CanonizeInteractions) return;

        var sourceCan = source.HasModifier<PopeCanonizedModifier>();
        var targetCan = target.HasModifier<PopeCanonizedModifier>();
        var sourcePope = source.Data.Role is PopeRole;
        var targetPope = target.Data.Role is PopeRole;

        if (!sourceCan && !sourcePope && !targetCan && !targetPope) return;

        var pope = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x.Data.Role is PopeRole);
        if (pope == null) return;

        if ((sourcePope || sourceCan) && !targetCan && !targetPope)
        {
            target.RpcAddModifier<PopeCanonizedModifier>(pope);
        }
        else if ((targetPope || targetCan) && !sourceCan && !sourcePope)
        {
            source.RpcAddModifier<PopeCanonizedModifier>(pope);
        }
    }

    [RegisterEvent]
    public static void ReportBodyEventHandler(ReportBodyEvent @event)
    {
        if (@event.Target == null) return;
        CheckCanonized(@event.Target.Object, @event.Reporter);
    }

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (MeetingHud.Instance) return;
        CheckCanonized(@event.Source, @event.Target);
    }

    [RegisterEvent]
    public static void MiraButtonClickEventHandler(MiraButtonClickEvent @event)
    {
        var button = @event.Button as CustomActionButton<PlayerControl>;
        var source = PlayerControl.LocalPlayer;
        var target = button?.Target;

        if (@event.Button is PopeCanonizeButton) return;

        if (target == null || button == null || !button.CanClick()) return;

        CheckCanonized(source, target);
    }
}