using System.Text;
using AmongUs.GameOptions;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TownOfUs;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class DeathRole(IntPtr cppPtr)
    : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IUnguessable
{
    public const string DeathReason = "ExtensionDeathClaimed";

    [HideFromIl2Cpp]
    public bool Announced
    {
        get => SoulCollectorRole.DeathAnnounced;
        set => SoulCollectorRole.DeathAnnounced = value;
    }

    public string LocaleKey => "Death";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Death");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string YouAreText => TouLocale.Get("YouAre");
    public string YouWereText => TouLocale.Get("YouWere");
    public bool IsGuessable => false;
    public RoleBehaviour AppearAs => RoleManager.Instance.GetRole((RoleTypes)RoleId.Get<SoulCollectorRole>());

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription");
    }

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(DeathKillButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleSoulCollectorReap", "Reap"),
            TouLocale.Get("ExtensionRoleDeathReapWikiDescription", "Kill a player and leave a blackened unreportable body."),
            TouNeutAssets.ReapSprite)
    ];

    public Color RoleColor => TouExtensionColors.Death;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralKilling;
    public bool HasImpostorVision => true;

    public CustomRoleConfiguration Configuration => new(this)
    {
        CanUseVent = OptionGroupSingleton<SoulCollectorOptions>.Instance?.DeathCanVent ?? false,
        UseVanillaKillButton = false,
        HideSettings = true,
        CanModifyChance = false,
        DefaultChance = 0,
        DefaultRoleCount = 0,
        MaxRoleCount = 0,
        IntroSound = TouAudio.PhantomIntroSound,
        Icon = TouExtensionIcons.SoulCollectorRoleIcon,
        GhostRole = (RoleTypes)RoleId.Get<NeutralGhostRole>()
    };

    [HideFromIl2Cpp]
    public void TriggerDeathAnnouncement()
    {
        var localData = PlayerControl.LocalPlayer?.Data;
        if (localData == null)
        {
            return;
        }

        var msg = TouLocale.GetParsed("ExtensionRoleSoulCollectorDeathAnnouncement", "The final soul has been claimed.\\%nl\\%\\%color=#202020FF\\%Death\\%/color\\%, Horseman of the Apocalypse, has emerged!");
        var title = $"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(TownOfUsColors.SoulCollector)}>{TouLocale.Get("ExtensionRoleSoulCollectorDeathAnnouncementTitle", "Death Warning")}</color>";

        try
        {
            TouMegaChujoweExtension.Modules.RoleAlertUtils.ShowRoleAlert(
                $"<b>{msg.Replace("\n", " ").Replace("\\%nl\\%", " ")}</b>",
                Color.white,
                TouExtensionIcons.SoulCollectorRoleIcon.LoadAsset());
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[TOUMCE] Death role alert failed: {ex}");
        }

        try
        {
            MiscUtils.AddFakeChat(localData, title, msg, false, true);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[TOUMCE] Death fake chat failed: {ex}");
        }
    }

    public override void OnMeetingStart()
    {
        RoleBehaviourStubs.OnMeetingStart(this);

        var options = OptionGroupSingleton<SoulCollectorOptions>.Instance;
        if (Announced ||
            PlayerControl.LocalPlayer?.Data == null ||
            options == null ||
            !options.AnnounceDeath)
        {
            return;
        }
        Announced = true;
        TriggerDeathAnnouncement();
    }

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        EnsureInvulnerability(player);
        EnsureInvisibility(player);

        if (player.AmOwner && HudManager.Instance?.ImpostorVentButton != null)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouNeutAssets.ReaperVentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TouExtensionColors.Death);
        }

        var options = OptionGroupSingleton<SoulCollectorOptions>.Instance;
        if (MeetingHud.Instance != null &&
            !Announced &&
            PlayerControl.LocalPlayer?.Data != null &&
            options != null &&
            options.AnnounceDeath)
        {
            Announced = true;
            TriggerDeathAnnouncement();
        }



    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);

        if (targetPlayer.HasModifier<InvulnerabilityModifier>())
        {
            targetPlayer.RemoveModifier<InvulnerabilityModifier>();
        }

        if (targetPlayer.HasModifier<DeathInvisibleModifier>())
        {
            targetPlayer.RemoveModifier<DeathInvisibleModifier>();
        }

        if (targetPlayer.AmOwner && HudManager.Instance?.ImpostorVentButton != null)
        {
            HudManager.Instance.ImpostorVentButton.graphic.sprite = TouAssets.VentSprite.LoadAsset();
            HudManager.Instance.ImpostorVentButton.buttonLabelText.SetOutlineColor(TownOfUsColors.Impostor);
        }
    }

    public bool WinConditionMet()
    {
        if (Player == null || Player.HasDied())
        {
            return false;
        }

        var deathCount = PlayerControl.AllPlayerControls.ToArray()
            .Count(x => x != null && !x.HasDied() && x.Data?.Role is DeathRole);

        if (MiscUtils.KillersAliveCount > deathCount)
        {
            return false;
        }

        var aliveCount = Helpers.GetAlivePlayers().Count;
        return aliveCount <= 2;
    }

    public override bool DidWin(GameOverReason gameOverReason)
    {
        if (gameOverReason == MiraAPI.GameEnd.CustomGameOver.GameOverReason<GameOver.ExtensionNeutralGameOver>() &&
            TouMegaChujoweExtension.Patches.WinConditions.NeutralExtensionWinCondition.IsApocalypseAllianceWon)
        {
            return true;
        }

        return WinConditionMet();
    }

    public override bool CanUse(IUsable usable)
    {
        if (!GameManager.Instance.LogicUsables.CanUse(usable, Player))
        {
            return false;
        }

        var console = usable.TryCast<Console>();
        return console == null || console.AllowImpostor;
    }

    private static void EnsureInvulnerability(PlayerControl death)
    {
        if (death.HasModifier<InvulnerabilityModifier>())
        {
            death.RemoveModifier<InvulnerabilityModifier>();
        }

        death.AddModifier<InvulnerabilityModifier>(false, false, false);
    }

    private static void EnsureInvisibility(PlayerControl death)
    {
        if (!death.HasModifier<DeathInvisibleModifier>())
        {
            death.AddModifier<DeathInvisibleModifier>();
        }
    }

    [MethodRpc((uint)ExtensionRpc.DeathKill)]
    public static void RpcDeathKill(PlayerControl death, PlayerControl target)
    {
        if (death == null ||
            target == null ||
            target.HasDied() ||
            death.Data?.Role is not DeathRole ||
            ApocalypseUtils.AreAllied(death, target))
        {
            return;
        }

        death.RpcSpecialMurder(
            target,
            ignoreShield: false,
            createDeadBody: true,
            teleportMurderer: false,
            showKillAnim: false,
            playKillSound: true,
            causeOfDeath: DeathReason);
    }

    [MethodRpc((uint)ExtensionRpc.DeathMarkBody, LocalHandling = Reactor.Networking.Rpc.RpcLocalHandling.Before)]
    public static void RpcMarkDeathBody(PlayerControl death, byte targetId)
    {
        var target = MiscUtils.PlayerById(targetId);
        if (death == null || target == null || death.Data?.Role is not DeathRole || ApocalypseUtils.AreAllied(death, target))
        {
            return;
        }

        SoulCollectorSystem.MarkDeathBody(targetId);
    }

    [HideFromIl2Cpp]
    public StringBuilder SetTabText()
    {
        var stringB = new StringBuilder();
        stringB.AppendLine(TownOfUsPlugin.Culture,
            $"{RoleColor.ToTextColor()}{YouAreText}<b> {RoleName},\n<size=80%>{RoleDescription}</size></b></color>");
        stringB.AppendLine(TownOfUsPlugin.Culture,
            $"<size=60%>{TouLocale.Get("Alignment")}: <b>{MiscUtils.GetParsedRoleAlignment(RoleAlignment, true)}</b></size>");
        stringB.Append("<size=70%>");
        stringB.AppendLine(TownOfUsPlugin.Culture, $"{RoleLongDescription}");

        return stringB;
    }
}
