using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Impostor;
using TouMegaChujoweExtension.Modifiers.Impostor;
using TouMegaChujoweExtension.Networking;
using TouMegaChujoweExtension.Options.Roles.Impostor;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modifiers.Impostor.Herbalist;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Impostor;

public sealed class InverterRole(IntPtr cppPtr) : ImpostorRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Trickster;
    public string LocaleKey => "Inverter";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Inverter");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription
    {
        get
        {
            var baseDesc = TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
            var victim = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == LastDisorientedPlayerId);
            if (victim != null && victim.TryGetModifier<InverterDisorientedModifier>(out var modifier))
            {
                var name = victim.Data?.PlayerName ?? "Unknown";
                var time = Mathf.CeilToInt(modifier.TimeRemaining);
                var isPolish = TouMegaChujoweExtensionPlugin.Culture.Name.StartsWith("pl", StringComparison.OrdinalIgnoreCase);
                var prefix = isPolish ? "Zdezorientowany: " : "Disoriented: ";
                return $"{baseDesc}\n\n<color=#B088FF>{prefix}{name} ({time}s)</color>";
            }
            return baseDesc;
        }
    }
    public Color RoleColor => Palette.ImpostorRed;
    public ModdedRoleTeams Team => ModdedRoleTeams.Impostor;
    public RoleAlignment RoleAlignment => RoleAlignment.ImpostorSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = true,
        Icon = TouExtensionIcons.InverterRoleIcon,
        IntroSound = TouAudio.ImpostorIntroSound,
        CanUseVent = true,
        OptionsScreenshot = TouBanners.ImpostorRoleBanner,
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => [typeof(InverterDisorientButton)];

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}Disorient", "Disorient"),
            TouLocale.GetParsed($"ExtensionRole{LocaleKey}DisorientWikiDescription"),
            TouExtensionImpAssets.InverterDisorientButtonSprite)
    ];

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription")
            + MiscUtils.AppendOptionsText(GetType());
    }

    public byte LastDisorientedPlayerId { get; set; } = byte.MaxValue;

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        LastDisorientedPlayerId = byte.MaxValue;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
    }

    [MethodRpc((uint)ExtensionRpc.InverterDisorient)]
    public static void RpcDisorient(PlayerControl inverter, PlayerControl victim)
    {
        if (inverter == null || inverter.Data?.Role is not InverterRole inverterRole || victim == null || victim.HasDied())
        {
            return;
        }

        foreach (var existing in victim.GetModifiers<InverterDisorientedModifier>().ToList())
        {
            victim.RemoveModifier(existing);
        }

        var options = OptionGroupSingleton<InverterOptions>.Instance;
        victim.AddModifier<InverterDisorientedModifier>(options.DisorientDuration);

        if (options.ApplyDrunk)
        {
            foreach (var existing in victim.GetModifiers<InjectedInvertedControlsModifier>().ToList())
            {
                victim.RemoveModifier(existing);
            }

            victim.AddModifier<InjectedInvertedControlsModifier>(
                options.DisorientDuration,
                InjectorEffectDurationType.SetTime);
        }

        if (options.ApplyHerbalistConfuse)
        {
            foreach (var existing in victim.GetModifiers<HerbalistConfusedModifier>().ToList())
            {
                victim.RemoveModifier(existing);
            }

            victim.AddModifier<HerbalistConfusedModifier>(inverter);
        }

        inverterRole.LastDisorientedPlayerId = victim.PlayerId;
    }
}
