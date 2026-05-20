using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Modifiers;
using MiraAPI.Patches.Stubs;
using MiraAPI.Roles;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Neutral;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TownOfUs.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Interfaces;
using TownOfUs.Modifiers;
using TownOfUs.Modifiers.Game;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modifiers.Neutral;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Roles.Neutral;
using TownOfUs.Utilities;
using UnityEngine;

namespace TouMegaChujoweExtension.Roles.Classic.Neutral;

public sealed class ArcanistRole(IntPtr cppPtr) : NeutralRole(cppPtr), ITownOfUsRole, IWikiDiscoverable, IDoomable
{
    public DoomableType DoomHintType => DoomableType.Insight;
    public string LocaleKey => "Arcanist";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Arcanist");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");
    public Color RoleColor => TouExtensionColors.Arcanist;
    public ModdedRoleTeams Team => ModdedRoleTeams.Custom;
    public RoleAlignment RoleAlignment => RoleAlignment.NeutralOutlier;

    public string GetAdvancedDescription()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription"));
        sb.AppendLine(" ");
        sb.Append(MiscUtils.AppendOptionsText(GetType()));
        return sb.ToString();
    }

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.Get("ExtensionRoleArcanistDraw", "Draw Card"),
            TouLocale.GetParsed("ExtensionRoleArcanistDrawWikiDescription"),
            TouExtensionIcons.ArcanistButtonIcon)
    ];

    public int CardsLeft { get; set; } = 10;
    public HashSet<TarotCard> DisabledCards { get; } = [];

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        CardsLeft = (int)OptionGroupSingleton<ArcanistOptions>.Instance.DeckSize;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        DisabledCards.Clear();
    }

    [MethodRpc((uint)ExtensionRpc.ArcanistDrawCard)]
    public static void RpcDrawCard(PlayerControl player, TarotCard card, ushort subRole = 0, byte targetId = 255)
    {
        var role = player.GetRole<ArcanistRole>();
        if (role == null) return;

        role.CardsLeft--;
        ProcessCardEffect(player, card, subRole, targetId);
    }

    public CustomRoleConfiguration Configuration => new(this)
    {
        UseVanillaKillButton = false,
        Icon = TouExtensionIcons.ArcanistRoleIconTraitor,
        IntroSound = TouAudio.ScientistIntroSound,
    };

    [HideFromIl2Cpp]
    public float CooldownMultiplier { get; set; } = 1f;

    public static bool DevilActivated { get; set; }

    public static TarotCard GetRandomCard()
    {
        var opts = OptionGroupSingleton<ArcanistOptions>.Instance;
        var role = PlayerControl.LocalPlayer.GetRole<ArcanistRole>();

        bool jesterAlive = IsRoleAlive(RoleId.Get<JesterRole>());
        bool lawyerAlive = IsRoleAlive(RoleId.Get<LawyerRole>());
        bool popeAlive = IsRoleAlive(RoleId.Get<PopeRole>());
        bool bountyAlive = IsRoleAlive(RoleId.Get<BountyHunterRole>());
        bool doomAlive = IsRoleAlive(RoleId.Get<DoomsayerRole>());
        bool loverAlive = IsModifierAlive<LoverModifier>();
        bool exeAlive = IsRoleAlive(RoleId.Get<ExecutionerRole>());

        float wFool = (role != null && role.DisabledCards.Contains(TarotCard.TheFool)) || jesterAlive ? 0f : opts.WeightFool;
        float wMagician = role != null && role.DisabledCards.Contains(TarotCard.TheMagician) ? 0f : opts.WeightMagician;
        float wHighPriestess = (role != null && role.DisabledCards.Contains(TarotCard.TheHighPriestess)) || doomAlive ? 0f : opts.WeightHighPriestess;
        float wEmpress = role != null && role.DisabledCards.Contains(TarotCard.TheEmpress) ? 0f : opts.WeightEmpress;
        float wEmperor = role != null && role.DisabledCards.Contains(TarotCard.TheEmperor) ? 0f : opts.WeightEmperor;
        float wHierophant = (role != null && role.DisabledCards.Contains(TarotCard.TheHierophant)) || exeAlive ? 0f : opts.WeightHierophant;
        float wLovers = 0f;
        float wChariot = role != null && role.DisabledCards.Contains(TarotCard.TheChariot) ? 0f : opts.WeightChariot;
        float wStrength = role != null && role.DisabledCards.Contains(TarotCard.Strength) ? 0f : opts.WeightStrength;
        float wHermit = role != null && role.DisabledCards.Contains(TarotCard.TheHermit) ? 0f : opts.WeightHermit;
        float wWheel = role != null && role.DisabledCards.Contains(TarotCard.WheelOfFortune) ? 0f : opts.WeightWheel;
        float wJustice = role != null && role.DisabledCards.Contains(TarotCard.Justice) ? 0f : opts.WeightJustice;
        float wHangedMan = (role != null && role.DisabledCards.Contains(TarotCard.TheHangedMan)) || bountyAlive ? 0f : opts.WeightHangedMan;
        float wDeath = role != null && role.DisabledCards.Contains(TarotCard.Death) ? 0f : opts.WeightDeath;
        float wTemperance = role != null && role.DisabledCards.Contains(TarotCard.Temperance) ? 0f : opts.WeightTemperance;
        float wDevil = 0f;
        float wTower = role != null && role.DisabledCards.Contains(TarotCard.TheTower) ? 0f : opts.WeightTower;
        float wStar = role != null && role.DisabledCards.Contains(TarotCard.TheStar) ? 0f : opts.WeightStar;
        float wMoon = role != null && role.DisabledCards.Contains(TarotCard.TheMoon) ? 0f : opts.WeightMoon;
        float wSun = role != null && role.DisabledCards.Contains(TarotCard.TheSun) ? 0f : opts.WeightSun;
        float wJudgement = (role != null && role.DisabledCards.Contains(TarotCard.Judgement)) || popeAlive ? 0f : opts.WeightJudgement;
        float wWorld = role != null && role.DisabledCards.Contains(TarotCard.TheWorld) ? 0f : opts.WeightWorld;

        float totalWeight = wFool + wMagician + wHighPriestess + wEmpress + wEmperor + wHierophant + wLovers + wChariot +
            wStrength + wHermit + wWheel + wJustice + wHangedMan + wDeath + wTemperance + wDevil + wTower + wStar +
            wMoon + wSun + wJudgement + wWorld;

        if (totalWeight <= 0f) return TarotCard.TheFool;

        float rand = UnityEngine.Random.Range(0f, totalWeight);

        if ((rand -= wFool) < 0) return TarotCard.TheFool;
        if ((rand -= wMagician) < 0) return TarotCard.TheMagician;
        if ((rand -= wHighPriestess) < 0) return TarotCard.TheHighPriestess;
        if ((rand -= wEmpress) < 0) return TarotCard.TheEmpress;
        if ((rand -= wEmperor) < 0) return TarotCard.TheEmperor;
        if ((rand -= wHierophant) < 0) return TarotCard.TheHierophant;
        if ((rand -= wLovers) < 0) return TarotCard.TheLovers;
        if ((rand -= wChariot) < 0) return TarotCard.TheChariot;
        if ((rand -= wStrength) < 0) return TarotCard.Strength;
        if ((rand -= wHermit) < 0) return TarotCard.TheHermit;
        if ((rand -= wWheel) < 0) return TarotCard.WheelOfFortune;
        if ((rand -= wJustice) < 0) return TarotCard.Justice;
        if ((rand -= wHangedMan) < 0) return TarotCard.TheHangedMan;
        if ((rand -= wDeath) < 0) return TarotCard.Death;
        if ((rand -= wTemperance) < 0) return TarotCard.Temperance;
        if ((rand -= wDevil) < 0) return TarotCard.TheDevil;
        if ((rand -= wTower) < 0) return TarotCard.TheTower;
        if ((rand -= wStar) < 0) return TarotCard.TheStar;
        if ((rand -= wMoon) < 0) return TarotCard.TheMoon;
        if ((rand -= wSun) < 0) return TarotCard.TheSun;
        if (rand < wJudgement) return TarotCard.Judgement;
        return TarotCard.TheWorld;
    }

    private static void ProcessCardEffect(PlayerControl player, TarotCard card, ushort subRole, byte targetId)
    {
        var cardName = TouLocale.Get($"TarotCard{card}", card.ToString());
        var msg = TouLocale.GetParsed("ExtensionArcanistCardDrawn", $"You drew {cardName}!").Replace("{0}", cardName);

        if (card == TarotCard.Death && subRole == 1)
        {
            msg = TouLocale.Get("ExtensionArcanistDeathLuckyDay", "It's your lucky day!.");
        }

        if (player.AmOwner)
        {
            Helpers.CreateAndShowNotification(
                $"<b>{TouExtensionColors.Arcanist.ToTextColor()}{msg}</color></b>",
                Color.white,
                new Vector3(0f, 1f, -20f),
                spr: TouExtensionIcons.ArcanistRoleIconTraitor.LoadAsset())
            .AdjustNotification();
        }

        switch (card)
        {
            case TarotCard.Justice:
                var revealMsg = TouLocale.GetParsed("ExtensionArcanistJusticeReveal", "{0} is the Arcanist!").Replace("{0}", player.Data.PlayerName);
                MiscUtils.AddFakeChat(player.Data, "<color=#FF0000>System</color>", revealMsg, false, true);
                break;
            case TarotCard.TheLovers:
                var evil = MiscUtils.PlayerById(targetId);
                if (evil != null)
                {
                    player.AddModifier<LoverModifier>(evil.PlayerId);
                    evil.AddModifier<LoverModifier>(player.PlayerId);
                }
                break;
            case TarotCard.TheHermit:
                player.AddModifier<InjectedSpeedBoostModifier>(999999f, InjectorEffectDurationType.AllGame, false);
                break;
            case TarotCard.TheTower:
                player.AddModifier<InjectedSlownessModifier>(999999f, InjectorEffectDurationType.AllGame, false);
                break;
            case TarotCard.TheStar:
                player.AddModifier<InjectedVisionBoostModifier>(999999f, InjectorEffectDurationType.AllGame, false);
                break;
        }

        if (player.AmOwner)
        {
            switch (card)
            {
                case TarotCard.TheFool:
                    if (!OptionGroupSingleton<ArcanistOptions>.Instance.AllowDuplicateRoles && IsRoleAlive(RoleId.Get<JesterRole>()))
                    {
                        HandleDuplicateRole(player, card);
                        break;
                    }
                    player.ChangeRole(RoleId.Get<JesterRole>());
                    break;
                case TarotCard.TheMagician:
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc != null && !pc.Data.IsDead && !pc.IsImpostorAligned() && !pc.Is(RoleAlignment.NeutralKilling))
                        {
                            pc.RpcAddModifier<TouMegaChujoweExtension.Modifiers.Impostor.InjectedVeryLowVisionModifier>(10f, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime, false);
                        }
                    }
                    break;
                case TarotCard.TheHighPriestess:
                    if (!OptionGroupSingleton<ArcanistOptions>.Instance.AllowDuplicateRoles && IsRoleAlive(RoleId.Get<DoomsayerRole>()))
                    {
                        HandleDuplicateRole(player, card);
                        break;
                    }
                    player.ChangeRole(RoleId.Get<DoomsayerRole>());
                    break;
                case TarotCard.TheEmpress:
                    player.RpcAddModifier<KnightedModifier>();
                    break;
                case TarotCard.TheEmperor:
                    player.RpcAddModifier<TouMegaChujoweExtension.Modifiers.Game.DrunkModifier>();
                    break;
                case TarotCard.TheHierophant:
                    if (subRole != 0)
                    {
                        if (!OptionGroupSingleton<ArcanistOptions>.Instance.AllowDuplicateRoles && IsRoleAlive(subRole))
                        {
                            HandleDuplicateRole(player, card);
                            break;
                        }
                        player.ChangeRole(subRole);
                    }
                    break;
                case TarotCard.TheLovers:
                    break;
                case TarotCard.TheChariot:
                    player.RpcAddModifier<TouMegaChujoweExtension.Modifiers.Impostor.InjectedSpeedBoostModifier>(30f, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime, false);
                    break;
                case TarotCard.Strength:
                    var role2 = player.GetRole<ArcanistRole>();
                    if (role2 != null) role2.CooldownMultiplier *= 0.8f;
                    break;
                case TarotCard.TheHermit:
                    break;
                case TarotCard.WheelOfFortune:
                    if (subRole != 0)
                    {
                        if (!OptionGroupSingleton<ArcanistOptions>.Instance.AllowDuplicateRoles && IsRoleAlive(subRole))
                        {
                            HandleDuplicateRole(player, card);
                            break;
                        }
                        player.ChangeRole(subRole);
                    }
                    break;
                case TarotCard.TheHangedMan:
                    if (!OptionGroupSingleton<ArcanistOptions>.Instance.AllowDuplicateRoles && IsRoleAlive(RoleId.Get<BountyHunterRole>()))
                    {
                        HandleDuplicateRole(player, card);
                        break;
                    }
                    player.ChangeRole(RoleId.Get<BountyHunterRole>());
                    break;
                case TarotCard.Death:
                    if (subRole == 0)
                    {
                        player.RpcSpecialMurder(player, causeOfDeath: "DeathReasonPlayingWithMagic");
                    }
                    break;
                case TarotCard.Temperance:
                    var role3 = player.GetRole<ArcanistRole>();
                    if (role3 != null) role3.CooldownMultiplier *= 1.2f;
                    break;
                case TarotCard.TheDevil:
                    if (!OptionGroupSingleton<ArcanistOptions>.Instance.AllowDuplicateRoles && IsRoleAlive(RoleId.Get<LawyerRole>()))
                    {
                        HandleDuplicateRole(player, card);
                        break;
                    }
                    player.ChangeRole(RoleId.Get<LawyerRole>());
                    break;
                case TarotCard.TheTower:
                    break;
                case TarotCard.TheStar:
                    break;
                case TarotCard.TheMoon:
                    player.RpcAddModifier<TouMegaChujoweExtension.Modifiers.Impostor.InjectedVeryLowVisionModifier>(30f, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime, false);
                    break;
                case TarotCard.TheSun:
                    player.RpcAddModifier<TouMegaChujoweExtension.Modifiers.Impostor.InjectedVisionBoostModifier>(30f, TouMegaChujoweExtension.Options.Roles.Impostor.InjectorEffectDurationType.SetTime, false);
                    break;
                case TarotCard.Judgement:
                    if (!OptionGroupSingleton<ArcanistOptions>.Instance.AllowDuplicateRoles && IsRoleAlive(RoleId.Get<PopeRole>()))
                    {
                        HandleDuplicateRole(player, card);
                        break;
                    }
                    player.ChangeRole(RoleId.Get<PopeRole>());
                    break;
                case TarotCard.TheWorld:
                    player.RpcAddModifier<TouMegaChujoweExtension.Modifiers.Neutral.DeathNoteModifier>();
                    break;
            }
        }

        if (card == TarotCard.TheDevil)
        {
            DevilActivated = true;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public static class MeetingHudStartPatch
    {
        public static void Postfix()
        {
            if (DevilActivated)
            {
                DevilActivated = false;
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.Data != null)
                {
                    MiscUtils.AddFakeChat(PlayerControl.LocalPlayer.Data, "<color=#FF0000>System</color>", TouLocale.Get("ExtensionArcanistDevilMeeting", "Someone has made a deal with the Devil and become a Lawyer!"), false, true);
                }
            }
        }
    }
    private static bool IsRoleAlive(uint roleId)
    {
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null || p.Data.IsDead || p.Data.Role == null) continue;

            if ((uint)p.Data.Role.Role == roleId) return true;

            if (p.Data.Role is ITownOfUsRole touRole && RoleId.Get(touRole.GetType()) == roleId) return true;
        }
        return false;
    }

    private static bool IsModifierAlive<T>() where T : BaseModifier
    {
        foreach (var p in PlayerControl.AllPlayerControls)
        {
            if (p == null || p.Data == null || p.Data.IsDead) continue;
            if (p.HasModifier<T>()) return true;
        }
        return false;
    }
    private static void HandleDuplicateRole(PlayerControl player, TarotCard card)
    {
        if (!player.AmOwner) return;

        var role = player.GetRole<ArcanistRole>();
        role?.DisabledCards.Add(card);

        var button = MiraAPI.Hud.CustomButtonSingleton<TouMegaChujoweExtension.Buttons.Classic.Neutral.ArcanistDrawButton>.Instance;
        if (button != null) button.Timer = 5f;

        var cardName = TouLocale.Get($"TarotCard{card}", card.ToString());
        var msg = TouLocale.GetParsed("ExtensionArcanistDuplicateRole", $"{cardName}: Role already exists! Card removed from deck.");
        Helpers.CreateAndShowNotification(
            $"<b><color=#FF0000>{msg}</color></b>",
            Color.white,
            new Vector3(0f, 1f, -20f),
            spr: TouExtensionIcons.ArcanistRoleIconTraitor.LoadAsset())
        .AdjustNotification();
    }
}

public enum TarotCard
{
    TheFool,
    TheMagician,
    TheHighPriestess,
    TheEmpress,
    TheEmperor,
    TheHierophant,
    TheLovers,
    TheChariot,
    Strength,
    TheHermit,
    WheelOfFortune,
    Justice,
    TheHangedMan,
    Death,
    Temperance,
    TheDevil,
    TheTower,
    TheStar,
    TheMoon,
    TheSun,
    Judgement,
    TheWorld
}
