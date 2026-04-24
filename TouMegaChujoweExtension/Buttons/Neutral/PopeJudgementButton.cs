using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Roles.Neutral;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class PopeJudgementButton : TownOfUsRoleButton<PopeRole>
{
    private static bool _triggered;

    public override string Name => TouLocale.GetParsed("ExtensionRolePopeJudgement", "Judgement");
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
