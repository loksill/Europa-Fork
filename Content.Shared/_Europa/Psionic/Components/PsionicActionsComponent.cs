using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Europa.Psionic.Components;

[RegisterComponent]
public sealed partial class PsionicActionsComponent : Component
{
    /// <summary>
    /// Доступные способности
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> GrantedActions = new();

    /// <summary>
    /// Уже выданные способности (чтобы не выдавать повторно)
    /// </summary>
    [ViewVariables]
    public HashSet<string> GrantedActionIds = new();
}
