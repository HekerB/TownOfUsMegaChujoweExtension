using System.Collections;
using System.Reflection;
using MiraAPI.Events;
using MiraAPI.Events.Vanilla.Gameplay;
using MiraAPI.Modifiers;
using Reactor.Utilities;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TouMegaChujoweExtension.Patches.Neutral;
using TownOfUs.Modifiers.Game.Crewmate;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouMegaChujoweExtension.Events.Modifiers;

public static class VenomousEvents
{
    private static MethodInfo? _startRottingMethod;
    private static MethodInfo? _coSetUpRotMethod;

    [RegisterEvent]
    public static void AfterMurderEventHandler(AfterMurderEvent @event)
    {
        if (!@event.Source.HasModifier<VenomousModifier>() || MeetingHud.Instance)
            return;

        _startRottingMethod ??= typeof(RottingModifier).GetMethod(
            "StartRotting",
            BindingFlags.Public | BindingFlags.Static);

        _coSetUpRotMethod ??= typeof(RottingModifier).GetMethod(
            "CoSetUpRot",
            BindingFlags.Public | BindingFlags.Static);

        var body = Object.FindObjectsOfType<DeadBody>()
            .FirstOrDefault(x => x.ParentId == @event.Target.PlayerId);

        if (body == null)
            return;

        if (_coSetUpRotMethod != null)
        {
            VenomousDelayPatch.Active = true;

            var result = _coSetUpRotMethod.Invoke(null, new object[]
            {
                body,
                @event.Target,
                @event.Source
            });

            if (result is IEnumerator coroutine)
                Coroutines.Start(coroutine);

            return;
        }

        if (_startRottingMethod == null)
            return;

        VenomousDelayPatch.Active = true;

        var paramCount = _startRottingMethod.GetParameters().Length;
        var result2 = paramCount >= 2
            ? _startRottingMethod.Invoke(null, new object[] { @event.Target, @event.Source })
            : _startRottingMethod.Invoke(null, new object[] { @event.Target });

        if (result2 is IEnumerator coroutine2)
            Coroutines.Start(coroutine2);
    }
}