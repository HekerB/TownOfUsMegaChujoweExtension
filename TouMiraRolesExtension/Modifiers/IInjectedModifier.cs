using System;

namespace TouMiraRolesExtension.Modifiers;

public interface IInjectedModifier
{
    Guid InjectionId { get; set; }
    string GetEffectDescription();
}