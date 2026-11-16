using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
  
namespace Content.Shared.CMU14.Xenomorphs.Pathogen.AssassinMark;
  
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AssassinMarkSystem))]
public sealed partial class AssassinMarkedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan WearOffAt;
  
    [DataField, AutoNetworkedField]  
    public FixedPoint2 DamageMultiplier = 1.30f;
}
