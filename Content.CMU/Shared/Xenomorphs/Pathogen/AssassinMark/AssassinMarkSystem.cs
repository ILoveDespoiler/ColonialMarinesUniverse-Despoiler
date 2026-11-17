using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Finesse;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
  
namespace Content.Shared.CMU14.Xenomorphs.Pathogen.AssassinMark;
  
public sealed class AssassinMarkSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
  
    public override void Initialize()
    {
        SubscribeLocalEvent<AssassinMarkComponent, AssassinMarkActionEvent>(OnMarkAction);
        SubscribeLocalEvent<AssassinMarkedComponent, DamageModifyEvent>(OnMarkedDamageModify);
    }
    
    private void OnMarkAction(Entity<AssassinMarkComponent> ent, ref AssassinMarkActionEvent args)
    {
        if (args.Handled)
            return;
  
        var target = args.Entity;
        if (target == null || !_xeno.CanAbilityAttackTarget(ent, target.Value))
            return;
  
        // Line of sight check  
        if (!_interaction.InRangeUnobstructed(ent.Owner, target.Value))
            return;
  
        if (!_rmcActions.TryUseAction(args))
            return;
  
        args.Handled = true;
  
        if (_net.IsClient)
            return;
  
        // X icon
        var marked = EnsureComp<XenoMarkedComponent>(target.Value);
        marked.WearOffAt = _timing.CurTime + ent.Comp.Duration;
        marked.TimeAdded = _timing.CurTime;
  
        // The debuff.
        var assassinMark = EnsureComp<AssassinMarkedComponent>(target.Value);
        assassinMark.WearOffAt = _timing.CurTime + ent.Comp.Duration;
        assassinMark.DamageMultiplier = ent.Comp.DamageMultiplier;
        Dirty(target.Value, assassinMark);
  
        // Warning popup
        _popup.PopupEntity(
            Loc.GetString("cmu-assassin-mark-warning"),
            target.Value,
            target.Value,
            PopupType.MediumCaution);
    }
  
    private void OnMarkedDamageModify(Entity<AssassinMarkedComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage *= ent.Comp.DamageMultiplier.Float();
    }
  
    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;
  
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<AssassinMarkedComponent>();
  
        while (query.MoveNext(out var uid, out var mark))
        {
            if (time < mark.WearOffAt)
                continue;
  
            RemCompDeferred<AssassinMarkedComponent>(uid);
            RemCompDeferred<XenoMarkedComponent>(uid);
        }
    }
}
