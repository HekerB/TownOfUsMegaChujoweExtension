using System;

namespace TouMegaChujoweExtension.Modifiers.Impostor;

public interface IInjectedModifier
{
    Guid InjectionId { get; set; }
    string GetEffectDescription();
}












