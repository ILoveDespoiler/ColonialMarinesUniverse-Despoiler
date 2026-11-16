using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.CMU14.Xenomorphs.Pathogen.Execute;
  
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMUPathogenExecuteSystem))]
public sealed partial class CMUPathogenExecuteComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier BaseDamage = new();
  
    [DataField, AutoNetworkedField]
    public DamageSpecifier BonusDamage = new();
  
    [DataField, AutoNetworkedField]
    public float BonusThresholdRatio = 0.5f;
  
    [DataField, AutoNetworkedField]
    public int ArmorPiercing = 10;
  
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;
}
