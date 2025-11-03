using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Europa.Psionic.Components;

[RegisterComponent]
public sealed partial class MindSwapActionComponent : Component
{
    /// <summary>
    /// Длительность действия эффекта в секундах
    /// </summary>
    [DataField("swapDuration")]
    public float SwapDuration = 30f;

    /// <summary>
    /// Эффект при успешном обмене
    /// </summary>
    [DataField("swapEffect", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SwapEffect = "EffectPsionicSwap";
}

