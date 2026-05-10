using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Hud;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Modules;
using TownOfUs.Roles.Neutral;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using TownOfUs.Buttons;
using TownOfUs;
using MiraAPI.Hud;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class PelicanRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Fearmonger;
    public string LocaleKey => "Pelican";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Pelican");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") +
               MiscUtils.AppendOptionsText(GetType());
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRolePelicanSwallow", "Swallow"),
            TouLocale.GetParsed("ExtensionRolePelicanSwallowWikiDescription",
                "Swallow a nearby player. They are hidden and trapped in your stomach until the next meeting (digested) or until you die (released)."),
            TouExtensionNeuAssets.PelicanSwallowButtonSprite)
    ];

    public Color RoleColor => TouExtensionColors.Pelican;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;

    public bool HasImpostorVision => true;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<PelicanOptions>.Instance.CanVent,
        Icon = TouExtensionIcons.PelicanRoleIcon,
        IntroSound = TouAudio.PhantomIntroSound,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>(),
        OptionsScreenshot = TouBanners.NeutralRoleBanner,
    };

    [HideFromIl2Cpp]
    public bool WinConditionMet()
    {
        if (Player.HasDied()) return false;

        var aliveOthers = 0;
        var threateningOthers = 0;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.HasDied()) continue;
            if (player.PlayerId == Player.PlayerId) continue;
            if (PelicanSystem.IsSwallowed(player.PlayerId)) continue;

            aliveOthers++;
            if (CanPlayerThreatenPelican(player)) threateningOthers++;
        }

        if (aliveOthers == 0) return true;

        if (aliveOthers == 1 && threateningOthers == 0) return true;

        return false;
    }
	
    [HideFromIl2Cpp]
    private static bool CanPlayerThreatenPelican(PlayerControl player)
    {
        var role = player.Data?.Role;
        if (role == null) return false;

        if (role.IsImpostor) return true;

        if (role is ITownOfUsRole touRole)
        {
            return touRole.RoleAlignment switch
            {
                RoleAlignment.CrewmateKilling => true,
                RoleAlignment.CrewmatePower => true,
                RoleAlignment.NeutralKilling => true,

                RoleAlignment.CrewmateProtective => false,
                RoleAlignment.CrewmateInvestigative => false,
                RoleAlignment.CrewmateSupport => false,
                RoleAlignment.NeutralBenign => false,
                RoleAlignment.NeutralEvil => false,
                _ => true
            };
        }

        if (role.CanUseKillButton) return true;

        return false;
    }

    public void OffsetButtons()
    {
        var canVent = OptionGroupSingleton<PelicanOptions>.Instance.CanVent || LocalSettingsTabSingleton<TownOfUsLocalSettings>.Instance.OffsetButtonsToggle.Value;
        var swallow = MiraAPI.Hud.CustomButtonSingleton<PelicanSwallowButton>.Instance;
        Reactor.Utilities.Coroutines.Start(MiscUtils.CoMoveButtonIndex(swallow, !canVent));
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);

        if (player.AmOwner)
        {
            OffsetButtons();
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(RoleColor);
        }
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        PelicanSystem.ReleaseAll(targetPlayer.PlayerId);
        PelicanSystem.ClearForPelican(targetPlayer.PlayerId);

        if (targetPlayer.AmOwner)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TownOfUsColors.Impostor);
        }

        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    public override void OnDeath(DeathReason reason)
    {
        if (Player != null)
        {
            var swallowed = PelicanSystem.GetSwallowedByPelican(Player.PlayerId);
            if (swallowed.Count > 0)
            {
                var releasePosition = Player.GetTruePosition();
                PelicanSystem.ReleaseAllAtPosition(Player.PlayerId, releasePosition);
            }
        }

        RoleBehaviourStubs.OnDeath(this, reason);
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player)) return false;

        var console = usable.TryCast<Console>()!;
        return console == null || console.AllowImpostor;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        return WinConditionMet();
    }

    public static void RpcPelicanSwallow(PlayerControl pelican, byte victimId)
    {
        if (pelican == null) return;

        var victim = MiscUtils.PlayerById(victimId);
        if (victim == null || victim.HasDied()) return;

        if (PelicanSystem.HandleShieldCheck(pelican, victim)) return;

        if (AmongUsClient.Instance.AmClient && pelican.AmOwner)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(pelican.NetId, (byte)ExtensionRpc.PelicanSwallow, Hazel.SendOption.Reliable, -1);
            writer.Write(victimId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        PelicanSystem.SwallowPlayer(pelican.PlayerId, victimId);
        if (victim.AmOwner)
        {
            PelicanSystem.ShowSwallowedNotification();
        }
    }

    public static void RpcPelicanDigest(PlayerControl pelican)
    {
        if (pelican == null) return;

        if (AmongUsClient.Instance.AmClient && pelican.AmOwner)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(pelican.NetId, (byte)ExtensionRpc.PelicanDigest, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        PelicanSystem.DigestAll(pelican.PlayerId);
    }

    public static void RpcPelicanRelease(PlayerControl pelican)
    {
        if (pelican == null) return;

        if (AmongUsClient.Instance.AmClient && pelican.AmOwner)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(pelican.NetId, (byte)ExtensionRpc.PelicanRelease, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        PelicanSystem.ReleaseAll(pelican.PlayerId);
    }
}















