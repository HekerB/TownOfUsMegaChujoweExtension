using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Keybinds;
using MiraAPI.Modifiers;
using MiraAPI.Utilities.Assets;
using System.Collections;
using System.Linq;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using TownOfUs.Assets;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class PopeCanonizeButton : TownOfUsRoleButton<PopeRole, PlayerControl>
{
    public override string Name => TouLocale.GetParsed("ExtensionRolePopeCanonize", "Give Cream Cake");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Pope;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<PopeOptions>.Instance.CanonizeCooldown + MapCooldown, 5f, 120f);
    public override int MaxUses => (int)OptionGroupSingleton<PopeOptions>.Instance.MaxCanonizations;
    public override LoadableAsset<Sprite> Sprite => TouNeutAssets.ChefServeCakeSprite;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && !PopeRole.EveryoneCanonized();
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.AllPlayerControls.ToArray()
            .Where(x => !x.Data.IsDead
                      && x.PlayerId != PlayerControl.LocalPlayer.PlayerId
                      && !x.HasModifier<PopeCanonizedModifier>()
                      && !PelicanSystem.IsSwallowed(x.PlayerId)
                      && Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), x.GetTruePosition()) <= Distance)
            .OrderBy(x => Vector2.Distance(PlayerControl.LocalPlayer.GetTruePosition(), x.GetTruePosition()))
            .FirstOrDefault();
    }

    protected override void OnClick()
    {
        if (Target == null) return;

        Target.RpcAddModifier<PopeCanonizedModifier>(PlayerControl.LocalPlayer, true);
    }
}

public sealed class PopeJudgementButton : TownOfUsRoleButton<PopeRole>
{
    private static bool _triggered;

    public override string Name => TouLocale.GetParsed("ExtensionRolePopeJudgement", "Sanctify");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Pope;
    public override float Cooldown => 0.001f;
    public override float InitialCooldown => 0.001f;
    public override LoadableAsset<Sprite> Sprite => TouExtensionNeuAssets.PopeJudgementButtonSprite;

    public override bool Enabled(RoleBehaviour? role)
    {
        return base.Enabled(role) && PopeRole.EveryoneCanonized() && !_triggered;
    }

    protected override void OnClick()
    {
        if (_triggered) return;

        if (!ShipStatus.Instance) return;

        var sabId = (SystemTypes)PopeJudgementSystem.SabotageId;
        if (!ShipStatus.Instance.Systems.ContainsKey(sabId)) return;

        foreach (var sys in ShipStatus.Instance.Systems.Values)
        {
            var sabo = sys.TryCast<ICriticalSabotage>();
            sabo?.ClearSabotage();
        }

        PopeRole.RpcTriggerJudgement(PlayerControl.LocalPlayer);

        _triggered = true;
    }

    public static void Reset()
    {
        _triggered = false;
    }
}




















