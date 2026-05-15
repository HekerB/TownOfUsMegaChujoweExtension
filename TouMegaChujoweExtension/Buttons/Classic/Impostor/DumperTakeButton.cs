using System.Linq;
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

public sealed class DumperTakeButton : TownOfUsRoleButton<DumperRole>
{
    public static DumperTakeButton? Instance { get; private set; }

    public DumperTakeButton()
    {
        Instance = this;
    }

    public override string Name => TouLocale.Get("ExtensionRoleDumperTake", "Take");
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Dumper;
    public override float Cooldown => OptionGroupSingleton<DumperOptions>.Instance.TakeCooldown;
    public override LoadableAsset<Sprite> Sprite => TouImpAssets.DragSprite;

    public DeadBody? TargetBody { get; private set; }

    protected override void FixedUpdate(PlayerControl player)
    {
        base.FixedUpdate(player);
        if (player == null) return;

        if (!DumperSystem.IsDragging(player.PlayerId))
        {
            TargetBody = GetClosestBody();
        }
        else
        {
            TargetBody = null;
        }
    }

    private DeadBody? GetClosestBody()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return null;

        var bodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
        DeadBody? closest = null;
        float minDist = GameManager.Instance.LogicOptions.GetKillDistance();

        foreach (var body in bodies)
        {
            float dist = Vector2.Distance(player.transform.position, body.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = body;
            }
        }

        return closest;
    }

    public override bool CanUse()
    {
        if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data.IsDead) return false;
        if (DumperSystem.IsDragging(PlayerControl.LocalPlayer.PlayerId)) return false;
        
        // Don't allow taking bodies from your own kills if restricted (MyKills logic is currently simple in DumperSystem)
        // But user said "nie świeci się przycisk od wzięcia ciała gdy jestem w pobliżu ciała"
        // Let's make sure TargetBody check is correct.
        
        return TargetBody != null && Timer <= 0f;
    }

    protected override void OnClick()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null) return;

        if (TargetBody != null)
        {
            DumperSystem.RpcPickupBody(player, TargetBody.ParentId);
            Timer = 0f;
        }
    }
}
