using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TouMegaChujoweExtension.Roles.Crewmate;
using TownOfUs.Buttons;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Buttons.Crewmate;

public sealed class DoctorHealButton : TownOfUsRoleButton<DoctorRole, PlayerControl>
{
    public override string Name => TouLocale.Get("ExtensionRoleDoctorInject", "Inject");
    // public override bool UsesCircleSprite => true; // Need to know the correct property for this
    public override BaseKeybind Keybind => Keybinds.SecondaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Doctor;
    public override float Cooldown => Math.Clamp(OptionGroupSingleton<DoctorOptions>.Instance.HealCooldown + MapCooldown, 5f, 120f);
    public override LoadableAsset<Sprite> Sprite => TouExtensionCrewAssets.DoctorHealButtonSprite;
    public override int MaxUses => (int)OptionGroupSingleton<DoctorOptions>.Instance.InitialUses;

    public override void CreateButton(Transform parent)
    {
        base.CreateButton(parent);
        if (Button != null)
        {
            Button.usesRemainingSprite.sprite = TownOfUs.Assets.TouAssets.AbilityCounterBodySprite.LoadAsset();
        }
    }

    public override bool ZeroIsInfinite { get; set; } = true;

    public override bool IsTargetValid(PlayerControl? target)
    {
        if (!base.IsTargetValid(target) || target == null)
        {
            return false;
        }

        return target != PlayerControl.LocalPlayer; // Cannot heal self by default, or maybe can?
    }

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, Distance);
    }

    protected override void OnClick()
    {
        if (Target == null)
        {
            Error("Doctor Heal: Target is null");
            return;
        }

        var player = PlayerControl.LocalPlayer;
        if (player == null)
        {
            Error("Doctor Heal: LocalPlayer is null");
            return;
        }

        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        DoctorRole.RpcDoctorHeal(player, Target, seed);
    }
}
