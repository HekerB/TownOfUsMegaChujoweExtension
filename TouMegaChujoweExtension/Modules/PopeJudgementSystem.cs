using Hazel;
using Il2CppInterop.Runtime.Injection;
using MiraAPI.GameEnd;
using MiraAPI.Roles;
using Reactor.Utilities.Attributes;
using TownOfUs.Events;
using TownOfUs.Modifiers;
using TownOfUs.Modules.Localization;
using TownOfUs.Utilities;
using TownOfUs;

namespace TouMegaChujoweExtension.Modules;

[RegisterInIl2Cpp(typeof(ISystemType), typeof(IActivatable))]
public sealed class PopeJudgementSystem(nint cppPtr) : Il2CppSystem.Object(cppPtr)
{
    public const byte SabotageId = 200;

    public static PopeJudgementSystem? Instance { get; private set; }
    public bool IsActive => Stage != PopeJudgementStage.None;
    public static bool InMeeting => MeetingHud.Instance != null || ExileController.Instance != null;
    public bool IsDirty { get; set; }
    public float TimeRemaining { get; set; }
    public PopeJudgementStage Stage { get; set; }
    public bool BombFinished { get; set; }
    public static bool GlobalBombFinished => Instance != null && Instance.BombFinished;
    public static float ConfiguredDuration { get; set; } = 120f;

    private float _dirtyTimer;

    public PopeJudgementSystem(float duration) : this(ClassInjector.DerivedConstructorPointer<PopeJudgementSystem>())
    {
        ClassInjector.DerivedConstructorBody(this);
        Instance = this;
        ConfiguredDuration = duration;
    }

    public void Deteriorate(float deltaTime)
    {
        if (!IsActive) return;
        if (InMeeting) return;

        if (!PlayerTask.PlayerHasTaskOfType<PopeJudgementTask>(PlayerControl.LocalPlayer))
        {
            PlayerControl.LocalPlayer.AddSystemTask((SystemTypes)SabotageId);
        }

        TimeRemaining -= deltaTime;
        _dirtyTimer += deltaTime;
        if (_dirtyTimer > 2f)
        {
            _dirtyTimer = 0f;
            IsDirty = true;
        }

        if (TimeRemaining <= 0)
        {
            switch (Stage)
            {
                case PopeJudgementStage.Initiate:
                    Stage = PopeJudgementStage.Countdown;
                    TimeRemaining = ConfiguredDuration;
                    BombFinished = false;
                    IsDirty = true;
                    break;

                case PopeJudgementStage.Countdown:
                    Stage = PopeJudgementStage.Finished;
                    TimeRemaining = 8f;
                    BombFinished = false;
                    IsDirty = true;

                    if (AmongUsClient.Instance.AmHost)
                    {
                        var pope = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.IsRole<PopeRole>());
                        if (pope != null)
                        {
                            foreach (var player in PlayerControl.AllPlayerControls.ToArray()
                                         .Where(x => !x.HasDied() && x.PlayerId != pope.PlayerId))
                            {
                                player.Die(DeathReason.Kill, false);
                                DeathHandlerModifier.UpdateDeathHandlerImmediate(player,
                                    TouLocale.Get("ExtensionDiedToPopeJudgement", "Divine Judgement"),
                                    DeathEventHandlers.CurrentRound,
                                    DeathHandlerOverride.SetTrue,
                                    TouLocale.GetParsed("ExtensionDiedByStringBasic", "Killed by <player>")
                                        .Replace("<player>", pope.Data.PlayerName),
                                    lockInfo: DeathHandlerOverride.SetTrue);
                            }
                        }
                    }
                    break;

                case PopeJudgementStage.Finished:
                    Stage = PopeJudgementStage.Ending;
                    TimeRemaining = 3f;
                    BombFinished = true;
                    IsDirty = true;

                    if (AmongUsClient.Instance.AmHost)
                    {
                        var winners = PlayerControl.AllPlayerControls.ToArray()
                            .Where(p => p?.Data?.Role is PopeRole && !p.Data.IsDead)
                            .Select(p => p.Data)
                            .ToArray();

                        if (winners.Length > 0)
                        {
                            CustomGameOver.Trigger<ExtensionNeutralGameOver>(winners);
                        }
                    }
                    break;

                case PopeJudgementStage.Ending:
                    Stage = PopeJudgementStage.None;
                    IsDirty = true;
                    break;

                case PopeJudgementStage.PopeDead:
                    Stage = PopeJudgementStage.Ending;
                    TimeRemaining = 3f;
                    BombFinished = false;
                    IsDirty = true;
                    break;
            }
        }
        else if (Stage == PopeJudgementStage.Countdown && IsPopeDead())
        {
            Stage = PopeJudgementStage.PopeDead;
            TimeRemaining = 5f;
            BombFinished = false;
            IsDirty = true;
        }
    }

    private static bool IsPopeDead()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role is PopeRole)
            {
                return player.Data.IsDead;
            }
        }
        return true;
    }

    public void UpdateSystem(PlayerControl player, MessageReader msgReader)
    {
        if (msgReader.ReadByte() != 1) return;
        Stage = PopeJudgementStage.Initiate;
        TimeRemaining = 1.5f;
        IsDirty = true;
    }

    public void Deserialize(MessageReader reader, bool initialState)
    {
        TimeRemaining = reader.ReadSingle();
        Stage = (PopeJudgementStage)reader.ReadByte();
        BombFinished = reader.ReadBoolean();
    }

    public void Serialize(MessageWriter writer, bool initialState)
    {
        writer.Write(TimeRemaining);
        writer.Write((byte)Stage);
        writer.Write(BombFinished);
        IsDirty = initialState;
    }

    public void MarkClean()
    {
        IsDirty = false;
    }
}

public enum PopeJudgementStage
{
    None,
    Initiate,
    Countdown,
    Finished,
    PopeDead,
    Ending,
}














