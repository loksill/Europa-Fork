using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared._Europa.Psionic.Components;

[RegisterComponent]
public sealed partial class PsionicComponent : Component
{
    [ViewVariables]
    [DataField("powerLevel")]
    public float PowerLevel = 1.0f;
}
