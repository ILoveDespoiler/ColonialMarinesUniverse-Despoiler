using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
  
namespace Content.Shared.CMU14.Xenomorphs.Pathogen.Execute;
  
public sealed class CMUPathogenExecuteSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _flash = default!;
    [Dependency] private readonly INetManager _net = default!;
  
    private EntityQuery<RMCCooldownOnMissComponent> _missCooldownQuery;
  
    public override void Initialize()
    {
        _missCooldownQuery = GetEntityQuery<RMCCooldownOnMissComponent>();
  
        SubscribeLocalEvent<CMUPathogenExecuteComponent, CMUPathogenExecuteActionEvent>(OnExecuteAction);
    }
  
    private void OnExecuteAction(Entity<CMUPathogenExecuteComponent> ent, ref CMUPathogenExecuteActionEvent args)
    {
        if (args.Handled)
            return;
  
        var target = args.Entity;
        if (target == null ||
            TerminatingOrDeleted(target.Value) ||
            !_xeno.CanAbilityAttackTarget(ent, target.Value, true))
        {
            ExecuteMiss(ent, args.Action);
            return;
        }
  
        if (!_rmcActions.TryUseAction(args))
            return;
  
        args.Handled = true;
        var targetId = target.Value;
  
        var damage = new DamageSpecifier(ent.Comp.BaseDamage);
  
        // Bonus damage once the target has taken at least BonusThresholdRatio  
        if (_mobThreshold.TryGetIncapThreshold(targetId, out var incapThreshold) &&  
            _damageable.GetTotalDamage(targetId) >= incapThreshold.Value * ent.Comp.BonusThresholdRatio)
        {
            damage += ent.Comp.BonusDamage;
        }
  
        var finalDamage = _xeno.TryApplyXenoSlashDamageMultiplier(targetId, damage);
        var damageTaken = _damageable.TryChangeDamage(
            targetId,
            finalDamage,
            armorPiercing: ent.Comp.ArmorPiercing,
            origin: ent,
            tool: ent,
            impact: DamageImpact.XenoRendingSlash(3) with
            {
                Contact = DamageImpactContact.Slash,
                Penetration = DamageImpactPenetration.Medium,
            });
  
        if (damageTaken?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(targetId, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == ent.Owner);
            _flash.RaiseEffect(Color.Red, new List<EntityUid> { targetId }, filter);
        }
  
        _rmcMelee.DoLunge(ent, targetId);
  
        if (_net.IsClient)
            return;
  
        if (ent.Comp.Sound != null)
            _audio.PlayPvs(ent.Comp.Sound, ent);
    }
  
    private void ExecuteMiss(Entity<CMUPathogenExecuteComponent> ent, EntityUid action)
    {
        if (_net.IsClient)
            return;
  
        if (_missCooldownQuery.TryComp(action, out var cooldown))
            _actions.SetIfBiggerCooldown(action, cooldown.MissCooldown);
    }
}
