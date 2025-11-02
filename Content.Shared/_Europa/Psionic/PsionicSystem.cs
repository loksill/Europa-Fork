using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using Content.Shared._Europa.Psionic.Component;
using Content.Shared._Europa.Psionic.Actions;

namespace Content.Shared._Europa.Psionic;

public sealed class PsionicsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const float DefaultPsionicPercentage = 0.1f; // 10%

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartEvent>(OnRoundStart);
    }

    /// <summary>
    /// Распределяем псиоников
    /// </summary>
    private void OnRoundStart(RoundStartEvent args)
    {
        AssignPsionicRoles();
    }

    public void AssignPsionicRoles()
    {
        // Получаем все сущности, способные стать псиониками (PotentialPsionicComponent)
        var potentialPsionics = EntityManager.EntityQuery<PotentialPsionicComponent>(true);
        var candidateList = new List<EntityUid>(); // Создаём список

        foreach (var (uid, comp) in potentialPsionics) // Вносим в этот список сущности
        {
            candidateList.Add(uid);
        }

        if (candidateList.Count == 0) // Проверяем, есть ли в списке вообще сущности
            return;

        // Получаем процент псиоников из конфигурации или используем значение по умолчанию
        var psionicPercentage = _cfg.GetCVar(CCVars.PsionicPercentage, DefaultPsionicPercentage);
        psionicPercentage = Math.Clamp(psionicPercentage, 0f, 1f);

        // Вычисляем количество псиоников
        var psionicCount = (int) Math.Round(candidateList.Count * psionicPercentage);
        psionicCount = Math.Max(psionicCount, 1); // Минимум 1 псионик

        // Перемешиваем список кандидатов для случайного выбора
        _random.Shuffle(candidateList);

        // Назначаем псиониками выбранных кандидатов
        for (int i = 0; i < psionicCount && i < candidateList.Count; i++)
        {
            var candidate = candidateList[i];
            MakePsionic(candidate);
        }

        Logger.InfoS("psionics", $"Назначено {psionicCount} псиоников из {candidateList.Count} кандидатов"); // Логгируем
    }

    /// <summary>
    /// Превращаем сущность в псионика
    /// </summary>
    public void MakePsionic(EntityUid uid, PsionicComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
        {
            component = EnsureComp<PsionicComponent>(uid);
        }

        // Выбираем способность
        component.PowerLevel = _random.NextFloat(1.0f, 2.0f);

        Logger.InfoS("psionics", $"Сущность {uid} стала псиоником со способностью: {component.PowerLevel}"); // Логиируем

        // Отправляем событие о том, что сущность стала псиоником
        var ev = new PsionicAwakenedEvent(uid, component.PowerLevel);
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Проверяем, является ли сущность псиоником
    /// </summary>
    public bool IsPsionic(EntityUid uid, PsionicComponent? component = null)
    {
        return Resolve(uid, ref component, false);
    }

    /// <summary>
    /// Получаем всех псиоников
    /// </summary>
    public List<EntityUid> GetCurrentPsionics()
    {
        var psionics = EntityManager.EntityQuery<PsionicComponent>(true);
        return psionics.Select(p => p.Owner).ToList();
    }

    /// <summary>
    /// Событие, вызываемое когда сущность становится псиоником
    /// </summary>
    [ByRefEvent]
    public readonly struct PsionicAwakenedEvent
    {
        public readonly EntityUid Entity;
        public readonly float PowerLevel;

        public PsionicAwakenedEvent(EntityUid entity, float powerLevel)
        {
            Entity = entity;
            PowerLevel = powerLevel;
        }
    }

}
