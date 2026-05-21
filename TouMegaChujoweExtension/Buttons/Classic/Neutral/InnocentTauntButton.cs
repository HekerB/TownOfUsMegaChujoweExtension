using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Classic.Neutral;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Classic.Neutral;

public sealed class InnocentTauntButton : TownOfUsRoleButton<InnocentRole>
{
    public override string Name => TouLocale.Get("ExtensionRoleInnocentTaunt", "Taunt");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override float Cooldown => OptionGroupSingleton<InnocentOptions>.Instance.TauntCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionIcons.InnocentRoleIcon;
    public override int MaxUses => 0;

    public PlayerControl? Target { get; set; }

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return false;

        if (!OptionGroupSingleton<InnocentOptions>.Instance.CanTauntFirstRound && GameData.Instance.TotalTasks == 0) 
            return false;

        Target = Helpers.GetClosestPlayers(player, player.MaxReportDistance)
            .FirstOrDefault(p => !p.Data.IsDead && p.PlayerId != player.PlayerId);

        return Timer <= 0f && Target != null && !player.inVent;
    }

    public override bool CanClick()
    {
        return CanUse();
    }

    public override void ClickHandler()
    {
        if (!CanUse()) return;
        OnClick();
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || Target == null) return;

        var role = player.GetRole<InnocentRole>();
        if (role == null) return;

        role.PendingTauntKillerId = Target.PlayerId;

        // Force the target to murder us
        Target.RpcMurderPlayer(player, true);

        Timer = Cooldown;
    }
}
