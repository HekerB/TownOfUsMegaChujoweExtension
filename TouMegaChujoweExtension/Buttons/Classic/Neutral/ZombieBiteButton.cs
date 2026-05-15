using System;
using System.Linq;
using MiraAPI.GameOptions;
using MiraAPI.Keybinds;
using MiraAPI.Utilities.Assets;
using MiraAPI.Modifiers;
using TouMegaChujoweExtension.Options.Roles.Neutral;
using TouMegaChujoweExtension.Roles.Neutral;
using TouMegaChujoweExtension.Assets;
using TownOfUs.Buttons;
using TownOfUs.Extensions;
using TownOfUs.Utilities;
using TownOfUs.Assets;
using TownOfUs.Modules.Localization;
using TownOfUs.Assets;
using TownOfUs.Roles.Neutral;
using MiraAPI.Roles;
using UnityEngine;
using MiraAPI;
using MiraAPI.Networking;
using MiraAPI.Roles;
using TownOfUs.Interfaces;
using TouMegaChujoweExtension.Modifiers.Neutral;
using TownOfUs.Modifiers.Game.Alliance;
using TownOfUs.Modifiers.Game.Neutral;
using TownOfUs.Modifiers.Game;
using TownOfUs.Networking;

namespace TouMegaChujoweExtension.Buttons.Neutral;

public sealed class ZombieBiteButton : TownOfUsKillRoleButton<ZombieRole, PlayerControl>, IKillButton
{
    public override string Name => "Bite";
    public override BaseKeybind Keybind => Keybinds.PrimaryAction;
    public override Color TextOutlineColor => TouExtensionColors.Zombie;
    public override float Cooldown => 20f; // Default cooldown
    public override LoadableAsset<Sprite> Sprite => TownOfUs.Assets.TouRoleIcons.Vampire;

    public override PlayerControl? GetTarget()
    {
        return PlayerControl.LocalPlayer.GetClosestLivingPlayer(true, GameManager.Instance.LogicOptions.GetKillDistance());
    }

    protected override void OnClick()
    {
        if (Target == null) return;
        
        var player = PlayerControl.LocalPlayer;
        var options = OptionGroupSingleton<ZombieOptions>.Instance;
        
        if (Target.GetModifiers<ZombieModifier>().Any()) return;

        bool isImpostor = Target.IsImpostorAligned();
        bool isNeutral = false;
        if (Target.Data.Role is TownOfUs.Roles.ITownOfUsRole touRole)
        {
            isNeutral = touRole.Team == ModdedRoleTeams.Custom;
        }
        bool isEgotist = Target.GetModifiers<EgotistModifier>().Any();
        bool isCrewpostor = Target.GetModifiers<CrewpostorModifier>().Any();

        if (isEgotist || isCrewpostor)
        {
            CustomTouMurderRpcs.RpcSpecialMurder(player, Target, causeOfDeath: "Eaten");
        }
        else if (isImpostor || isNeutral)
        {
            // Impostors and Neutrals always convert
            ZombieRole.RpcZombieConvert(player, Target);
        }
        else
        {
            // Crewmates have a chance
            var roll = UnityEngine.Random.Range(0, 100);
            if (roll < options.ChanceToConvert)
            {
                ZombieRole.RpcZombieConvert(player, Target);
            }
            else
            {
                CustomTouMurderRpcs.RpcSpecialMurder(player, Target, causeOfDeath: "Eaten");
            }
        }

        Timer = Cooldown;
    }
}
