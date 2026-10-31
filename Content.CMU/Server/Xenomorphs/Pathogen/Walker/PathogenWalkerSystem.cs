using Content.Server.NPC.Systems;
using Content.Server.Humanoid;
using Content.Shared.CMU14.Xenomorphs.Pathogen.MycotoxinInject;
using Content.Shared.CMU14.Xenomorphs.Pathogen.Walker;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Nutrition.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared._RMC14.Armor;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Jittering;
using Content.Shared.CMU14.Medical.Injuries.Wounds;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.StatusEffectNew;
using Content.Server._RMC14.Language.Systems;
using Content.Shared._RMC14.Language.Components;
using Content.Shared.Radio.Components;
using Content.Server.Ghost.Roles.Components;
using Robust.Shared.Player;
using Content.Server.Mind;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared._RMC14.Synth;
using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Content.Shared._RMC14.Pulling;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameObjects;
using Content.Shared.CMU14.Medical.Anatomy.BodyParts.Events;
using Content.Shared.Body.Part;
using Content.Shared.CMU14.Medical.Anatomy.Organs.Eyes;

namespace Content.Server.CMU14.Xenomorphs.Pathogen.Walker;

public sealed partial class CMUPathogenWalkerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedXenoHiveSystem _hive = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IPrototypeManager _protoMgr = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private HumanoidOrganAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private SkillsSystem _skills = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    private static readonly ProtoId<NpcFactionPrototype> WalkerFaction = "CMU14PathogenWalker";
    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly string[] PainStatusEffects =
    {
        "StatusEffectCMUPainMild",
        "StatusEffectCMUPainModerate",
        "StatusEffectCMUPainSevere",
        "StatusEffectCMUPainShock",
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<CMUMycotoxinInjectDoReanimateEvent>(OnReanimate);
        SubscribeLocalEvent<CMUPathogenWalkerComponent, MobStateChangedEvent>(OnWalkerDeath);
        SubscribeLocalEvent<CMUPathogenWalkerComponent, ExaminedEvent>(OnWalkerExamined);
        SubscribeNetworkEvent<CMUPathogenWalkerAcceptNetEvent>(OnAcceptNet);
        SubscribeNetworkEvent<CMUPathogenWalkerDeclineNetEvent>(OnDeclineNet);
        SubscribeLocalEvent<BodyPartComponent, BodyPartSeveredEvent>(OnWalkerSevered);
    }

    private void OnReanimate(CMUMycotoxinInjectDoReanimateEvent ev)  
    {  
        var target = ev.Target;  
        var injector = ev.Injector;  
  
        if (HasComp<CMUPathogenWalkerComponent>(target))  
            return;  
  
        if (HasComp<SynthComponent>(target))  
            return;  
  
        var walker = EnsureComp<CMUPathogenWalkerComponent>(target);  
  
        var hives = EntityQueryEnumerator<HiveComponent, MetaDataComponent>();  
        while (hives.MoveNext(out var hiveUid, out _, out var meta))  
        {  
            if (meta.EntityPrototype?.ID != "CMUPathogenHive")  
                continue;  
  
            _hive.SetHive(target, hiveUid);  
            walker.Hive = hiveUid;  
            break;  
        }   
  
        RemComp<CMUOrganBlindnessComponent>(target);  
        RemComp<CMUOrganVisionImpairmentComponent>(target);  
  
        // Walkers don't hunger or thirst  
        RemComp<HungerComponent>(target);  
        RemComp<ThirstComponent>(target);  
  
        // Innate damage resistance 
        var armor = EnsureComp<CMArmorComponent>(target);  
        armor.Melee = 10;  
        armor.Bullet = 10;  
        armor.Bio = 20;           
        armor.ExplosionArmor = 5;  
        Dirty(target, armor);  
  
        // Slight speed boost 
        if (TryComp<MovementSpeedModifierComponent>(target, out var moveSpeed))  
        {  
            moveSpeed.BaseWalkSpeed *= 1.1f;  
            moveSpeed.BaseSprintSpeed *= 1.1f;  
            Dirty(target, moveSpeed);  
        }  
        else  
        {  
            var speed = EnsureComp<MovementSpeedModifierComponent>(target);  
            speed.BaseWalkSpeed = MovementSpeedModifierComponent.DefaultBaseWalkSpeed * 1.1f;  
            speed.BaseSprintSpeed = MovementSpeedModifierComponent.DefaultBaseSprintSpeed * 1.1f;  
            Dirty(target, speed);  
        }  
        _movementSpeedModifier.RefreshMovementSpeedModifiers(target);  
  
        _faction.AddFaction(target, WalkerFaction);  
        // Reanimatable hosts like monkeys can lack the language component entirely.  
        EnsureComp<LanguageComponent>(target);  
        _language.SetExclusiveLanguage(target, "Pathogen");  
  
        EnsureComp<IntrinsicRadioReceiverComponent>(target);  
        var transmitter = EnsureComp<IntrinsicRadioTransmitterComponent>(target);  
        transmitter.Channels = [SharedChatSystem.HivemindChannel];  
        var radio = EnsureComp<ActiveRadioComponent>(target);  
        radio.Channels = [SharedChatSystem.HivemindChannel];  
        var tacIcon = EnsureComp<TacticalMapIconComponent>(target);  
  
        EnsureComp<PullWhitelistComponent>(target);  
        var whitelistEv = new SetPullWhitelistEvent(new EntityWhitelist  
        {  
            Components = new[] { "Xeno", "Infectable", "Synth", "Yautja" }  
        });  
        RaiseLocalEvent(target, ref whitelistEv);  
  
        EquipMarker(target, walker);  
  
        foreach (var (skill, min) in walker.MinimumSkills)  
        {  
            if (_skills.GetSkill(target, skill) < min)  
                _skills.SetSkill(target, skill, min);  
        }  
          
        // If the victim has a connected player, show the offer popup.  
        // Otherwise skip straight to ghost role.  
        if (TryGetWalkerSession(target, out var session))  
        {  
            walker.OfferExpiresAt = _timing.CurTime + walker.OfferTimeout;  
            walker.OfferResolved = false;  
            Dirty(target, walker);  
  
            RaiseNetworkEvent(new CMUPathogenWalkerOfferEvent(  
                GetNetEntity(target),  
                walker.OfferTimeout.TotalSeconds), session);  
        }  
        else  
        {  
            MakeGhostRole(target, walker);  
        }  
  
        Dirty(target, walker);  
    }
