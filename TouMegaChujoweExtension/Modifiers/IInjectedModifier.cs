using System;

namespace TouMegaChujoweExtension.Modifiers;

public interface IInjectedModifier
{
    Guid InjectionId { get; set; }
    string GetEffectDescription();
}