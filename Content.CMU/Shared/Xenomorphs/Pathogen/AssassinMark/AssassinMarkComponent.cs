using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
  
namespace Content.Shared.CMU14.Xenomorphs.Pathogen.AssassinMark;
  
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMUAssassinMarkSystem))]
public sealed partial class AssassinMarkComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);
  
    [DataField, AutoNetworkedField]
    public FixedPoint2 DamageMultiplier = 1.30f;
}
