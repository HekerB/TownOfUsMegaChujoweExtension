using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TouMegaChujoweExtension.Roles.Impostor;
using TouMegaChujoweExtension.Modules;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Impostor;

public sealed class DumperDumpButton : TownOfUsRoleButton<DumperRole>
{
    public static DumperDumpButton? Instance { get; private set; }

    public DumperDumpButton()
    {
        Instance = this;
    }

    public override string Name => TouLocale.Get("ExtensionRoleDumperDump", "Dump");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Dumper;
    public override float Cooldown => 0f; // Drop has no cooldown or a short one?
    public override LoadableAsset<Sprite> Sprite => TouImpAssets.DropSprite;

    protected override void FixedUpdate(PlayerControl player)
    {
        base.FixedUpdate(player);
        if (player == null) return;

        if (DumperSystem.IsDragging(player.PlayerId))
        {
            var remaining = DumperSystem.GetDragTimer(player.PlayerId);
            if (Button != null)
            {
                Button.SetFillUp(remaining, OptionGroupSingleton<DumperOptions>.Instance.MaxDragDuration);
                Button.cooldownTimerText.text = Mathf.Ceil(remaining).ToString();
                Button.cooldownTimerText.gameObject.SetActive(true);
            }
        }
    }

    public override bool CanUse()
    {
        return PlayerControl.LocalPlayer != null && 
               !PlayerControl.LocalPlayer.Data.IsDead && 
               DumperSystem.IsDragging(PlayerControl.LocalPlayer.PlayerId);
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;
        
        DumperSystem.RpcDropBody(player);
        // Maybe put Take on cooldown after dropping?
        if (DumperTakeButton.Instance != null)
        {
            DumperTakeButton.Instance.Timer = OptionGroupSingleton<DumperOptions>.Instance.TakeCooldown;
        }
    }
}
