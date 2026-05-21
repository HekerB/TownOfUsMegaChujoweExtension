using System.Collections;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Classic.Impostor;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;
using System.Linq;

namespace TouMegaChujoweExtension.Buttons.Classic.Impostor;

public sealed class BootButton : TownOfUsRoleButton<BootRole>
{
    public static BootButton? Instance { get; private set; }
    private bool _isProcessingClick;

    public BootButton()
    {
        Instance = this;
    }

    public override string Name => TouLocale.Get("ExtensionRoleBootAction", "Boot");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction; // Typically F
    public override float Cooldown => OptionGroupSingleton<BootOptions>.Instance.BootCooldown;
    public override LoadableAsset<Sprite> Sprite => TouExtensionIcons.BootRoleIcon;
    public override int MaxUses => 0;

    public DeadBody? ClosestBody { get; set; }

    public override bool CanUse()
    {
        if (MeetingHud.Instance || ExileController.Instance) return false;
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data.IsDead) return false;
        
        ClosestBody = UnityEngine.Object.FindObjectsOfType<DeadBody>().FirstOrDefault(b => Vector2.Distance(player.GetTruePosition(), b.transform.position) <= player.MaxReportDistance);
        
        return Timer <= 0f && ClosestBody != null && !player.inVent;
    }

    public override bool CanClick()
    {
        return CanUse();
    }

    public override void ClickHandler()
    {
        if (_isProcessingClick) return;
        _isProcessingClick = true;

        try
        {
            if (!CanUse()) return;
            OnClick();
        }
        finally
        {
            Reactor.Utilities.Coroutines.Start(ResetProcessingFlag());
        }
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isProcessingClick = false;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || ClosestBody == null) return;

        var options = OptionGroupSingleton<BootOptions>.Instance;
        Vector2 randomPos;

        var vents = UnityEngine.Object.FindObjectsOfType<Vent>();
        if (vents != null && vents.Length > 0)
        {
            var randomVent = vents[UnityEngine.Random.Range(0, vents.Length)];
            randomPos = (Vector2)randomVent.transform.position;
        }
        else
        {
            randomPos = player.GetTruePosition();
        }

        // Send RPC
        BootRole.RpcTeleportBody(player, ClosestBody.ParentId, randomPos);

        Timer = Cooldown;

        if (options.SyncCooldowns)
        {
            PlayerControl.LocalPlayer.SetKillTimer(options.KillCooldown);
        }
    }
}
