using Robust.Shared.GameStates;
using Content.Shared.CMU14.Xenomorphs.Pathogen.AssassinMark; // CMU14

namespace Content.Shared._RMC14.Xenonids.Finesse;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoFinesseSystem), typeof(AssassinMarkSystem))] // CMU14
public sealed partial class XenoMarkedComponent : Component
{
    [DataField]
    public TimeSpan TimeAdded;

    [DataField]
    public TimeSpan WearOffAt;
}
