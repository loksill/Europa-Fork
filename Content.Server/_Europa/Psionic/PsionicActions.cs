using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Content.Shared.Actions;
using Content.Server._Europa.Psionic.Components;
using Content.Server._Europa.Psionic;
using Content.Server.Mind;
using Content.Shared.Mind;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Server._Europa.Psionic.Actions;

public sealed class PsionicActions : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PsionicComponent, ComponentStartup>(OnPsionicStartup);
        SubscribeLocalEvent<PsionicComponent, ComponentShutdown>(OnPsionicShutdown);
        SubscribeLocalEvent<PsionicActionsComponent, ComponentShutdown>(OnActionsShutdown);

        SubscribeLocalEvent<MindSwapActionComponent, MindSwapEvent>(OnMindSwap);
        SubscribeLocalEvent<FlammableComponent, PsionicInflammationEvent>(OnPsionicInflammation);
    }

    private void OnPsionicStartup(EntityUid uid, PsionicComponent component, ComponentStartup args)
    {
        GrantPsionicActions(uid, component);
    }

    private void OnPsionicShutdown(EntityUid uid, PsionicComponent component, ComponentShutdown args)
    {
        RemovePsionicActions(uid);
    }

    private void OnActionsShutdown(EntityUid uid, PsionicActionsComponent component, ComponentShutdown args)
    {
        foreach (var action in component.GrantedActions)
        {
            _actions.RemoveAction(uid, action);
        }
    }

    /// <summary>
    /// Выдём Actions в зависимости от значения PowerLevel
    /// </summary>
    private void GrantPsionicActions(EntityUid uid, PsionicComponent psionic)
    {
        var actions = EnsureComp<PsionicActionsComponent>(uid);

        // PsionicInflammation - уровень 1.0
        if (psionic.PowerLevel == 1.0f && !actions.GrantedActionIds.Contains("ActionPsionicInflammation"))
        {
            var inflammationAction = Spawn("ActionPsionicInflammation");
            _actions.AddAction(uid, inflammationAction, inflammationAction);
            actions.GrantedActions.Add(inflammationAction);
            actions.GrantedActionIds.Add("ActionPsionicInflammation");
        }

        // MindSwap - уровень 2.0
        if (psionic.PowerLevel == 2.0f && !actions.GrantedActionIds.Contains("ActionMindSwap"))
        {
            var mindSwapAction = Spawn("ActionMindSwap");
            _actions.AddAction(uid, mindSwapAction, mindSwapAction);
            actions.GrantedActions.Add(mindSwapAction);
            actions.GrantedActionIds.Add("ActionMindSwap");
        }
    }

    /// <summary>
    /// Удаляем все Actions
    /// </summary>
    private void RemovePsionicActions(EntityUid uid)
    {
        if (!TryComp<PsionicActionsComponent>(uid, out var actions))
            return;

        foreach (var action in actions.GrantedActions)
        {
            _actions.RemoveAction(uid, action);
        }

        RemComp<PsionicActionsComponent>(uid);
    }


    /// <summary>
    /// Обновляем действия при изменении уровня силы
    /// </summary>
    public void UpdatePsionicActions(EntityUid uid, PsionicComponent psionic)
    {
        RemovePsionicActions(uid);
        GrantPsionicActions(uid, psionic);
    }

    /// <summary>
    /// Обработка Psionic Inflammation
    /// </summary>
    private void OnPsionicInflammation(PsionicInflammationEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;

        // Исспользуем существующую систему FlammableComponent
        if (TryComp<FlammableComponent>(target, out var flammable))
        {
            // Поджигаем цель
            _flammable.AdjustFireStacks(target, 3f, flammable); // Добавляем 3 стака огня
            _flammable.Ignite(target, flammable);

            // Создаём визуальный эффект
            if (_prototype.HasIndex<EntityPrototype>("EffectSpark"))
            {
                var effect = Spawn("EffectSpark", Transform(target).Coordinates);
                Timer.Spawn(2000, () => Del(effect));
            }
        }

        args.Handled = true;
    }

    private void OnMindSwap(EntityUid uid, MindSwapActionComponent component, MindSwapEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        var user = uid;
        var target = args.Target.Value;

        // Проверяем, что у обеих сущностей есть MindComponent
        if (!TryComp<MindComponent>(user, out var userMind) ||
            !TryComp<MindComponent>(target, out var targetMind))
            return;

        // Сохраняем текущие Mind'ы
        var userMindId = userMind.Mind;
        var targetMindId = targetMind.Mind;

        // Меняем Mind'ы местами
        if (userMindId != null && targetMindId != null)
        {
            _mind.TransferTo(userMindId, target, ghostOnWindow: false);
            _mind.TransferTo(targetMindId, user, ghostOnWindow: false);
        }

        if (_prototype.HasIndex<EntityPrototype>(component.SwapEffect))
        {
            var effectUser = Spawn(component.SwapEffect, Transform(user).Coordinates);
            var effectTarget = Spawn(component.SwapEffect, Transform(target).Coordinates);

            Timer.Spawn(2000, () =>
            {
                Del(effectUser);
                Del(effectTarget);
            });
        }

        // Автоматическое возвращение
        Timer.Spawn(TimeSpan.FromSeconds(component.SwapDuration), () =>
        {
            if (Deleted(user) || Deleted(target))
                return;

            // Возвращаем Mind'ы обратно
            if (TryComp<MindComponent>(user, out var currentUserMind) &&
                TryComp<MindComponent>(target, out var currentTargetMind))
            {
                var currentUserMindId = currentUserMind.Mind;
                var currentTargetMindId = currentTargetMind.Mind;

                if (currentUserMindId != null && currentTargetMindId != null)
                {
                    _mind.TransferTo(currentUserMindId, user, ghostOnWindow: false);
                    _mind.TransferTo(currentTargetMindId, target, ghostOnWindow: false);
                }
            }
        });

        args.Handled = true;
    }

    /// <summary>
    /// Событие возгарания (PsionicInflammation)
    /// </summary>
    public sealed partial class PsionicInflammationEvent : EntityTargetActionEvent {}

    /// <summary>
    /// Событие обмена разумом (MindSwap)
    /// </summary>
    public sealed partial class MindSwapEvent : EntityTargetActionEvent {}
}


