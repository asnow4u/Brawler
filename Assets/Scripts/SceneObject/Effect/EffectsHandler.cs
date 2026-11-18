using System;
using UnityEngine;

/// <summary>
/// Builds and runs the scene object's effects from an EffectsDataCollection.
/// Each effect is set up only when its data entry and its source handler exist.
/// Instances live under the "Effects" child of this object, one group per effect.
/// </summary>
public partial class EffectsHandler : MonoBehaviour, IEffects
{
    private const string EffectsRootName = "Effects";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [Flags]
    private enum EffectGroup
    {
        None = 0,
        GuardShield = 1 << 0,
        CounterBlast = 1 << 1,
        WeaponTrail = 1 << 2,
        Hit = 1 << 3,
        Launch = 1 << 4,
        All = GuardShield | CounterBlast | WeaponTrail | Hit | Launch
    }

    [Tooltip("Effect data for this scene object. Entries left empty are not set up.")]
    [SerializeField] private EffectsDataCollection effectsData;

    private ISceneObject sceneObject;
    private IGuard guard;
    private IHurtBoxHandler hurtBoxHandler;
    private ISOHurtBoxHandler soHurtBoxHandler;
    private IAttackHitBoxHandler attackHitBoxHandler;
    private IActionState actionState;
    private new Rigidbody rigidbody;

    private Transform effectsRoot;
    private MaterialPropertyBlock propertyBlock;
    private bool hasStarted;
    private EffectGroup dirtyGroups;


    #region Initialize

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        guard = GetComponent<IGuard>();
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();
        soHurtBoxHandler = hurtBoxHandler as ISOHurtBoxHandler;
        attackHitBoxHandler = GetComponent<IAttackHitBoxHandler>();
        actionState = GetComponent<IActionState>();
        rigidbody = GetComponent<Rigidbody>();
        propertyBlock = new MaterialPropertyBlock();

        SetupEffects(EffectGroup.All);
        SubscribeToDataChanges();
    }

    private void Start()
    {
        hasStarted = true;
        StartWeaponTrails();
    }

    private void OnDisable()
    {
        HideGuardShield();
        StopCounterBlast();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDataChanges();
        TeardownEffects(EffectGroup.All);
    }

    private void SetupEffects(EffectGroup groups)
    {
        if (effectsData == null)
            return;

        if ((groups & EffectGroup.GuardShield) != 0) SetupGuardShield();
        if ((groups & EffectGroup.CounterBlast) != 0) SetupCounterBlast();
        if ((groups & EffectGroup.WeaponTrail) != 0) SetupWeaponTrails();
        if ((groups & EffectGroup.Hit) != 0) SetupHit();
        if ((groups & EffectGroup.Launch) != 0) SetupLaunch();
    }

    private void TeardownEffects(EffectGroup groups)
    {
        if ((groups & EffectGroup.GuardShield) != 0) TeardownGuardShield();
        if ((groups & EffectGroup.CounterBlast) != 0) TeardownCounterBlast();
        if ((groups & EffectGroup.WeaponTrail) != 0) TeardownWeaponTrails();
        if ((groups & EffectGroup.Hit) != 0) TeardownHit();
        if ((groups & EffectGroup.Launch) != 0) TeardownLaunch();
    }

    #endregion


    #region Hierarchy

    /// <summary>Returns the "Effects" child of this object, creating it when missing.</summary>
    private Transform GetEffectsRoot()
    {
        if (effectsRoot != null)
            return effectsRoot;

        effectsRoot = transform.Find(EffectsRootName);
        if (effectsRoot == null)
            effectsRoot = CreateChild(transform, EffectsRootName);

        return effectsRoot;
    }

    /// <summary>Returns the named group under the effects root, creating it when missing.</summary>
    private Transform GetEffectGroup(string groupName)
    {
        Transform root = GetEffectsRoot();
        Transform group = root.Find(groupName);
        return group != null ? group : CreateChild(root, groupName);
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        Transform child = new GameObject(childName).transform;
        child.SetParent(parent, false);
        child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        child.localScale = Vector3.one;
        return child;
    }

    /// <summary>Instantiates the prefab under the group and names the instance after the prefab.</summary>
    private static T InstantiateEffect<T>(T prefab, Transform group) where T : Component
    {
        T instance = Instantiate(prefab, group);
        instance.name = prefab.name;
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        return instance;
    }

    /// <summary>Places the transform at a world position with no rotation and the given world scale.</summary>
    private static void SetWorldPose(Transform target, Vector3 position, Vector3 worldScale)
    {
        target.SetPositionAndRotation(position, Quaternion.identity);

        Vector3 parentScale = target.parent != null ? target.parent.lossyScale : Vector3.one;
        target.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 1e-6f ? value / divisor : value;
    }

    /// <summary>Sets _BaseColor on the renderer through a MaterialPropertyBlock.</summary>
    private void SetRendererBaseColor(Renderer target, Color color)
    {
        target.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, color);
        target.SetPropertyBlock(propertyBlock);
    }

    private static void DestroyInstance(Component instance)
    {
        if (instance != null)
            Destroy(instance.gameObject);
    }

    #endregion


    #region Update

    private void Update()
    {
        if (dirtyGroups != EffectGroup.None)
            RebuildDirtyEffects();

        UpdateLaunch();
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;

        UpdateGuardShield(deltaTime);
        UpdateCounterBlast(deltaTime);
        UpdateWeaponTrails(deltaTime);
    }

    #endregion


    #region Runtime Data Change

    private EffectsDataCollection subscribedCollection;
    private GuardShieldEffectData subscribedGuardShield;
    private CounterBlastEffectData subscribedCounterBlast;
    private WeaponTrailEffectData subscribedWeaponTrail;
    private HitEffectData subscribedHit;
    private LaunchEffectData subscribedLaunch;

    private void SubscribeToDataChanges()
    {
        #if UNITY_EDITOR
        subscribedCollection = effectsData;
        if (subscribedCollection == null) return;

        subscribedCollection.OnChangedEvent += OnCollectionChanged;
        SubscribeToEntries();
        #endif
    }

    private void UnsubscribeFromDataChanges()
    {
        #if UNITY_EDITOR
        if (subscribedCollection == null) return;

        subscribedCollection.OnChangedEvent -= OnCollectionChanged;
        UnsubscribeFromEntries();
        subscribedCollection = null;
        #endif
    }

    private void SubscribeToEntries()
    {
        #if UNITY_EDITOR
        subscribedGuardShield = effectsData.GuardShield;
        subscribedCounterBlast = effectsData.CounterBlast;
        subscribedWeaponTrail = effectsData.WeaponTrail;
        subscribedHit = effectsData.Hit;
        subscribedLaunch = effectsData.Launch;

        if (subscribedGuardShield != null)  subscribedGuardShield.OnChangedEvent  += OnGuardShieldDataChanged;
        if (subscribedCounterBlast != null) subscribedCounterBlast.OnChangedEvent += OnCounterBlastDataChanged;
        if (subscribedWeaponTrail != null)  subscribedWeaponTrail.OnChangedEvent  += OnWeaponTrailDataChanged;
        if (subscribedHit != null)          subscribedHit.OnChangedEvent          += OnHitDataChanged;
        if (subscribedLaunch != null)       subscribedLaunch.OnChangedEvent       += OnLaunchDataChanged;
        #endif
    }

    private void UnsubscribeFromEntries()
    {
        #if UNITY_EDITOR
        if (subscribedGuardShield != null)  subscribedGuardShield.OnChangedEvent  -= OnGuardShieldDataChanged;
        if (subscribedCounterBlast != null) subscribedCounterBlast.OnChangedEvent -= OnCounterBlastDataChanged;
        if (subscribedWeaponTrail != null)  subscribedWeaponTrail.OnChangedEvent  -= OnWeaponTrailDataChanged;
        if (subscribedHit != null)          subscribedHit.OnChangedEvent          -= OnHitDataChanged;
        if (subscribedLaunch != null)       subscribedLaunch.OnChangedEvent       -= OnLaunchDataChanged;

        subscribedGuardShield = null;
        subscribedCounterBlast = null;
        subscribedWeaponTrail = null;
        subscribedHit = null;
        subscribedLaunch = null;
        #endif
    }

    private void OnCollectionChanged()
    {
        #if UNITY_EDITOR
        UnsubscribeFromEntries();
        SubscribeToEntries();
        #endif

        dirtyGroups = EffectGroup.All;
    }

    private void OnGuardShieldDataChanged()  => dirtyGroups |= EffectGroup.GuardShield;
    private void OnCounterBlastDataChanged() => dirtyGroups |= EffectGroup.CounterBlast;
    private void OnWeaponTrailDataChanged()  => dirtyGroups |= EffectGroup.WeaponTrail;
    private void OnHitDataChanged()          => dirtyGroups |= EffectGroup.Hit;
    private void OnLaunchDataChanged()       => dirtyGroups |= EffectGroup.Launch;

    /// <summary>Tears down and sets up every effect whose data changed since the last frame.</summary>
    private void RebuildDirtyEffects()
    {
        EffectGroup groups = dirtyGroups;
        dirtyGroups = EffectGroup.None;

        TeardownEffects(groups);
        SetupEffects(groups);

        if ((groups & EffectGroup.WeaponTrail) != 0)
            StartWeaponTrails();
    }

    #endregion
}
