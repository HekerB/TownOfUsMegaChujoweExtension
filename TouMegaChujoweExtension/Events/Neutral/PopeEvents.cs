using MiraAPI.Events.Mira;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Events.Vanilla.Meeting;
using MiraAPI.Events;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using TownOfUs.Buttons;
using UnityEngine;

namespace TouMegaChujoweExtension.Events.Neutral;

public static class PopeEvents
{
    public static void CheckCanonized(PlayerControl source, PlayerControl target)
    {
        if (!OptionGroupSingleton<PopeOptions>.Instance.CanonizeInteractions) return;

        source.TryGetModifier<PopeCanonizedModifier>(out var sourceCanMod);
        target.TryGetModifier<PopeCanonizedModifier>(out var targetCanMod);
        var sourcePope = source.Data.Role is PopeRole;
        var targetPope = target.Data.Role is PopeRole;

        if (sourceCanMod == null && !sourcePope && targetCanMod == null && !targetPope) return;

        var pope = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x.Data.Role is PopeRole);
        if (pope == null) return;

        if ((sourcePope || (sourceCanMod != null && sourceCanMod.CanSpread && !sourceCanMod.HasSpread)) && targetCanMod == null && !targetPope)
        {
            target.RpcAddModifier<PopeCanonizedModifier>(pope, sourcePope);
            if (sourceCanMod != null) sourceCanMod.HasSpread = true;
        }
        else if ((targetPope || (targetCanMod != null && targetCanMod.CanSpread && !targetCanMod.HasSpread)) && sourceCanMod == null && !sourcePope)
        {
            source.RpcAddModifier<PopeCanonizedModifier>(pope, targetPope);
            if (targetCanMod != null) targetCanMod.HasSpread = true;
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

















