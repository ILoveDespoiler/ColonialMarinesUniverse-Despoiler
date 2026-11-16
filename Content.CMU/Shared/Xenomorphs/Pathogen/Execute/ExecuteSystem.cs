using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
  
namespace Content.Shared.CMU14.Xenomorphs.Pathogen.Execute;
  
public sealed class CMUPathogenExecuteSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
  
    public override void Initialize()
    {
        SubscribeLocalEvent<CMUPathogenExecuteComponent, CMUPathogenExecuteActionEvent>(OnExecute);
    }
  
    private void OnExecute(Entity<CMUPathogenExecuteComponent> ent, ref CMUPathogenExecuteActionEvent args)
    {
        if (args.Handled)
            return;
  
        var target = args.Entity;
        if (target == null ||
            TerminatingOrDeleted(target.Value) ||
            !_xeno.CanAbilityAttackTarget(ent, target.Value))
        {
            return;
        }
  
        if (!_rmcActions.TryUseAction(ent, args.Action, ent))
            return;
  
        args.Handled = true;
  
        var damage = new DamageSpecifier(ent.Comp.BaseDamage);
   
        if (_mobThreshold.TryGetIncapThreshold(target.Value, out var incapThreshold) &&
            _damageable.GetTotalDamage(target.Value) >= incapThreshold.Value * ent.Comp.BonusThresholdRatio)
        {
            damage += ent.Comp.BonusDamage;
        }
  
        if (_net.IsServer)
        {
            _damageable.TryChangeDamage(
                target.Value,
                damage,
                armorPiercing: ent.Comp.ArmorPiercing,
                origin: ent,
                tool: ent);
        }
  
        if (ent.Comp.Sound != null)
            _audio.PlayPvs(ent.Comp.Sound, target.Value);
    }
}
