using System;
using Reactor.Utilities.Attributes;
using UnityEngine;

namespace TouMegaChujoweExtension.Modules;

[RegisterInIl2Cpp]
public sealed class JokerCloneControlComponent(IntPtr ptr) : MonoBehaviour(ptr)
{
    public byte OwnerId;
    public byte AppearanceId;
}
