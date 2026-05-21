using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using MiraAPI.GameOptions;
using MiraAPI.Roles;
using MiraAPI.Patches.Stubs;
using TouMegaChujoweExtension.Options.Roles.Crewmate;
using TownOfUs.Assets;
using TownOfUs.Extensions;
using TownOfUs.Modules.Localization;
using TownOfUs.Modules.Wiki;
using TownOfUs.Roles;
using TownOfUs.Utilities;
using UnityEngine;
using TouMegaChujoweExtension.Assets;
using TouMegaChujoweExtension.Buttons.Classic.Crewmate;
using Reactor.Utilities;

namespace TouMegaChujoweExtension.Roles.Classic.Crewmate;

public enum BuilderStructureType
{
    Crate = 0,
    Wall = 1,
    Pillar = 2,
    Barrier = 3
}

public sealed class BuilderRole(IntPtr cppPtr) : CrewmateRole(cppPtr), ITownOfUsRole, IWikiDiscoverable
{
    public string LocaleKey => "Builder";
    public string RoleName => TouLocale.Get($"ExtensionRole{LocaleKey}", "Builder");
    public string RoleDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}IntroBlurb");
    public string RoleLongDescription => TouLocale.GetParsed($"ExtensionRole{LocaleKey}TabDescription");

    public string GetAdvancedDescription()
    {
        return TouLocale.GetParsed($"ExtensionRole{LocaleKey}WikiDescription") + MiscUtils.AppendOptionsText(GetType());
    }

    public Color RoleColor => TouExtensionColors.Builder;
    public ModdedRoleTeams Team => ModdedRoleTeams.Crewmate;
    public RoleAlignment RoleAlignment => RoleAlignment.CrewmateSupport;

    public CustomRoleConfiguration Configuration => new(this)
    {
        Icon = TouExtensionIcons.BuilderRoleIcon,
        IntroSound = TouAudio.ViperIntroSound,
        OptionsScreenshot = TouBanners.CrewmateRoleBanner
    };

    [HideFromIl2Cpp]
    public List<Type> RoleButtons => new List<Type> { typeof(BuilderPlaceButton), typeof(BuilderCycleButton) };

    [HideFromIl2Cpp]
    public List<CustomButtonWikiDescription> Abilities =>
    [
        new(
            TouLocale.GetParsed("ExtensionRoleBuilderPlace", "Place"),
            TouLocale.GetParsed("ExtensionRoleBuilderPlaceWikiDescription"),
            TouExtensionIcons.BuilderRoleIcon),
        new(
            TouLocale.GetParsed("ExtensionRoleBuilderCycle", "Cycle"),
            TouLocale.GetParsed("ExtensionRoleBuilderCycleWikiDescription"),
            TouExtensionIcons.BuilderRoleIcon)
    ];

    public BuilderStructureType CurrentStructureType { get; set; } = BuilderStructureType.Crate;
    private int _nextStructureIndex = 0;

    public static Dictionary<int, GameObject> PlacedStructures { get; } = new();

    public override void Initialize(PlayerControl player)
    {
        RoleBehaviourStubs.Initialize(this, player);
        CurrentStructureType = BuilderStructureType.Crate;
        _nextStructureIndex = 0;
    }

    public override void Deinitialize(PlayerControl targetPlayer)
    {
        RoleBehaviourStubs.Deinitialize(this, targetPlayer);
        // Clean up structures on deinitialization
        foreach (var structure in PlacedStructures.Values)
        {
            if (structure != null) UnityEngine.Object.Destroy(structure);
        }
        PlacedStructures.Clear();
    }

    [HideFromIl2Cpp]
    public void PlaceStructureLocal()
    {
        if (Player == null) return;

        float offsetDistance = 1.5f;
        switch (CurrentStructureType)
        {
            case BuilderStructureType.Crate:
                offsetDistance = 1.5f;
                break;
            case BuilderStructureType.Wall:
                offsetDistance = 2.5f;
                break;
            case BuilderStructureType.Pillar:
                offsetDistance = 1.2f;
                break;
            case BuilderStructureType.Barrier:
                offsetDistance = 3.0f;
                break;
        }
        float facingOffset = Player.cosmetics.FlipX ? -offsetDistance : offsetDistance;
        Vector2 spawnPos = Player.GetTruePosition() + new Vector2(facingOffset, 0f);
        
        int structureId = 50000 + Player.PlayerId * 1000 + _nextStructureIndex++;
        RpcBuildStructure(Player, spawnPos, (int)CurrentStructureType, structureId);
    }

    [Reactor.Networking.Attributes.MethodRpc((uint)Networking.ExtensionRpc.BuilderBuild)]
    public static void RpcBuildStructure(PlayerControl builder, Vector2 position, int type, int structureId)
    {
        if (builder == null) return;

        // Spawn on all clients
        var structureGo = new GameObject("BuilderStructure_" + structureId);
        structureGo.transform.position = new Vector3(position.x, position.y, -0.5f);
        structureGo.layer = LayerMask.NameToLayer("Ship"); // Interacts with physical movement colliders

        var sr = structureGo.AddComponent<SpriteRenderer>();
        var col = structureGo.AddComponent<BoxCollider2D>();

        if (type == (int)BuilderStructureType.Crate)
        {
            sr.sprite = CreateStructureSprite(type, 32, 32, 2.0f);
            col.size = new Vector2(2.0f, 2.0f);
        }
        else if (type == (int)BuilderStructureType.Wall)
        {
            sr.sprite = CreateStructureSprite(type, 64, 16, 4.0f);
            col.size = new Vector2(4.0f, 1.0f);
        }
        else if (type == (int)BuilderStructureType.Pillar)
        {
            sr.sprite = CreateStructureSprite(type, 16, 48, 1.2f);
            col.size = new Vector2(1.2f, 3.5f);
        }
        else if (type == (int)BuilderStructureType.Barrier)
        {
            sr.sprite = CreateStructureSprite(type, 80, 24, 5.0f);
            col.size = new Vector2(5.0f, 1.5f);
        }

        // Store structure
        PlacedStructures[structureId] = structureGo;

        // Play structural build sound effect
        PlayBuildSound(position);

        // Schedule destruction after configured duration
        float duration = OptionGroupSingleton<BuilderOptions>.Instance.BuildDuration;
        Coroutines.Start(CoDestroyStructureAfterTime(structureId, duration));
    }

    private static System.Collections.IEnumerator CoDestroyStructureAfterTime(int structureId, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PlacedStructures.TryGetValue(structureId, out var go))
        {
            if (go != null) UnityEngine.Object.Destroy(go);
            PlacedStructures.Remove(structureId);
        }
    }

    [HideFromIl2Cpp]
    private static Sprite CreateStructureSprite(int type, int width, int height, float physicalSize)
    {
        var texture = new Texture2D(width, height);
        
        if (type == (int)BuilderStructureType.Crate)
        {
            // Wooden Crate style (Brown with diagonal crosses and dark borders)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        texture.SetPixel(x, y, new Color(0.25f, 0.12f, 0.04f, 1.0f)); // dark brown frame
                    }
                    else if (x == 1 || x == width - 2 || y == 1 || y == height - 2 ||
                             x == 2 || x == width - 3 || y == 2 || y == height - 3)
                    {
                        texture.SetPixel(x, y, new Color(0.35f, 0.18f, 0.06f, 1.0f)); // lighter brown inner frame
                    }
                    // Diagonal cross pattern inside the crate
                    else if (Math.Abs(x - y) <= 1 || Math.Abs(x - (height - 1 - y)) <= 1)
                    {
                        texture.SetPixel(x, y, new Color(0.3f, 0.15f, 0.05f, 1.0f)); // dark cross beams
                    }
                    else
                    {
                        texture.SetPixel(x, y, new Color(0.5f, 0.28f, 0.12f, 1.0f)); // wood panels fill
                    }
                }
            }
        }
        else if (type == (int)BuilderStructureType.Wall)
        {
            // Metal/Steel wall style (Metallic gray panels with dark lines and rivets)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        texture.SetPixel(x, y, new Color(0.15f, 0.15f, 0.15f, 1.0f)); // dark metal border
                    }
                    else if (x % 16 == 0 || y == height / 2) // vertical panel lines
                    {
                        texture.SetPixel(x, y, new Color(0.25f, 0.25f, 0.25f, 1.0f)); // dark panel lines
                    }
                    else
                    {
                        // Iron gray color
                        texture.SetPixel(x, y, new Color(0.45f, 0.45f, 0.47f, 1.0f));
                    }
                }
            }
        }
        else if (type == (int)BuilderStructureType.Pillar)
        {
            // Concrete / Stone pillar (Light gray concrete with texture)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        texture.SetPixel(x, y, new Color(0.25f, 0.25f, 0.25f, 1.0f)); // dark concrete border
                    }
                    else
                    {
                        // concrete texture noise
                        bool noise = (x * 7 + y * 13) % 5 == 0;
                        texture.SetPixel(x, y, noise ? new Color(0.55f, 0.55f, 0.55f, 1.0f) : new Color(0.65f, 0.65f, 0.65f, 1.0f));
                    }
                }
            }
        }
        else if (type == (int)BuilderStructureType.Barrier)
        {
            // Yellow and Black construction barrier
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        texture.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        // diagonal caution stripes
                        bool yellow = ((x + y) / 4) % 2 == 0;
                        texture.SetPixel(x, y, yellow ? new Color(0.9f, 0.72f, 0.08f, 1.0f) : new Color(0.12f, 0.12f, 0.12f, 1.0f));
                    }
                }
            }
        }
        
        texture.Apply();
        float pixelsPerUnit = width / physicalSize;
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private static void PlayBuildSound(Vector2 pos)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;

        var listenerPos = (Vector2)(Camera.main?.transform.position ?? Vector3.zero);
        var dist = Vector2.Distance(pos, listenerPos);

        const float maxDist = 12f;
        if (dist <= maxDist)
        {
            var clip = TouExtensionAudio.DecoyPlaceSound.LoadAsset(); // Beautiful high-tech confirmation sound
            if (clip == null) return;

            var volume = Mathf.Clamp01(1f - (dist / maxDist)) * 0.8f;
            SoundManager.Instance.PlaySound(clip, false, volume);
        }
    }
}
