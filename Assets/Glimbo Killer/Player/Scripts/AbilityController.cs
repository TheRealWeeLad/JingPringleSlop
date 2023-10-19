using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityController : MonoBehaviour
{
    public List<Ability> abilities = new();
    Camera mainCam;

    // Projectile Death Effects
    public GameObject FireballDeathEffect;
    public GameObject BombDeathEffect;

    float[] _timesUntilReady;
    const float _spawnOffset = 0.7f;
    const int _maxAbilityLifespan = 5;

    // Keep track of Activated Abilities and their lifespans in seconds
    public static Dictionary<Ability, float> ActiveAbilities = new();
    public static List<Ability> StaleAbilities = new();

    // For Updating HUD
    readonly MethodInfo _resetCooldown = typeof(HUDManagement).GetMethod(nameof(HUDManagement.ResetCooldown), BindingFlags.Static | BindingFlags.Public);
    readonly MethodInfo _lightUpIcon = typeof(HUDManagement).GetMethod(nameof(HUDManagement.LightUpIcon), BindingFlags.Static | BindingFlags.Public);

    // To Determine Which Ability Function to Call
    readonly Dictionary<FireMode, Action<Ability, FireInfo>> _abilityFuncs = new();
    struct FireInfo
    {
        public int phase;
        public float timeUntil;
        public int idx;
    }

    private void Awake()
    {
        // Reset Lists
        abilities.Clear();
        ActiveAbilities.Clear();
        StaleAbilities.Clear();
        _abilityFuncs.Clear();

        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        // Set Ability Functions
        _abilityFuncs[FireMode.HOLD] = UseHold;
        _abilityFuncs[FireMode.HOLDANDRELEASE] = UseHoldAndRelease;
        _abilityFuncs[FireMode.PRESS] = UsePress;
    }

    private void Update()
    {
        if (_timesUntilReady != null)
        {
            UpdateCooldowns();
            UpdateAbilityLifespans();
            RemoveStaleAbilities();
        }
    }

    public void ChooseAbilities()
    {
        abilities = AbilityChooser.abilities;
        _timesUntilReady = new float[abilities.Count];
        for (int i = 0; i < _timesUntilReady.Length; i++) _timesUntilReady[i] = 0;
    }

    public void UseAbility(InputAction.CallbackContext context)
    {
        if (UIManagement.IsPaused()) return;

        int idx = int.Parse(context.action.name.Split(' ')[^1]) - 1;
        if (idx >= abilities.Count) return;
        Ability ab = abilities[idx];
        float timeUntil = _timesUntilReady[idx];
        int phase = (int)context.phase; // 2 -> STARTED, 3 -> PERFORMED, 4 -> CANCELED
        FireInfo info = new() { phase = phase, timeUntil = timeUntil, idx = idx };

        _abilityFuncs[ab.FiringMode].Invoke(ab, info);
    }
    void UseHold(Ability ab, FireInfo info)
    {
        MethodInfo lightUp = _lightUpIcon.MakeGenericMethod(ab.GetType());

        if (info.phase == 2)
        {
            // Spawn Ability
            Vector3 spawnPos = mainCam.transform.position;
            Ability ability = Instantiate(ab.gameObject, spawnPos, mainCam.transform.rotation).GetComponent<Ability>();
            ActiveAbilities.Add(ability, 0);

            // Light up HUD icon
            lightUp.Invoke(null, new object[] { true });
        }
        else if (info.phase == 4)
        {
            // REMOVE ABILITY
            Type type = ab.GetType();
            Ability ability = ActiveAbilities.FirstOrDefault(x => x.Key.GetType().IsEquivalentTo(type)).Key;
            if (!ability) return;
            ActiveAbilities.Remove(ability);
            Destroy(ability.gameObject);

            // Disappear HUD icon
            lightUp.Invoke(null, new object[] { false });
        }
    }
    void UseHoldAndRelease(Ability ab, FireInfo info)
    {
        if (OnCooldown(info.timeUntil)) return;

        if (info.phase == 2)
        {
            // Show Path Prediction
            (ab as ArcingProjectile).Aim();
        }
        else if (info.phase == 4)
        {
            // Hide Prediction and Spawn Ability
            UsePress(ab, info);
        }
    }
    void UsePress(Ability ab, FireInfo info)
    {
        if (OnCooldown(info.timeUntil)) return;
        Vector3 spawnPos = mainCam.transform.position;
        if (ab is ProjectileAbility) spawnPos += mainCam.transform.forward * _spawnOffset;

        Ability ability = Instantiate(ab.gameObject, spawnPos, Quaternion.identity).GetComponent<Ability>();
        ActiveAbilities.Add(ability, 0);

        if (ability is ArcingProjectile arcAb)
        {
            (ab as ArcingProjectile).StopAiming(arcAb);
        }
        if (ability is ProjectileAbility projAb) GiveDeathPrefab(projAb);

        _timesUntilReady[info.idx] = ability.Cooldown;

        // Update HUD
        MethodInfo reset = _resetCooldown.MakeGenericMethod(ability.GetType());
        reset.Invoke(null, null);
    }

    void GiveDeathPrefab(ProjectileAbility ability)
    {
        ability.SetDeathEffect(typeof(AbilityController).GetField(ability.GetType() + "DeathEffect").GetValue(this) as GameObject);
    }

    void UpdateAbilityLifespans()
    {
        foreach (Ability ab in ActiveAbilities.Keys.ToList())
        {
            if (ab.FiringMode == FireMode.HOLD) continue;

            ActiveAbilities[ab] += Time.deltaTime;

            if (ActiveAbilities[ab] >= _maxAbilityLifespan)
            {
                StaleAbilities.Add(ab);
            }
        }
    }
    void RemoveStaleAbilities()
    {
        foreach (Ability ab in StaleAbilities)
        {
            ActiveAbilities.Remove(ab);
            Destroy(ab.gameObject);
        }

        StaleAbilities.Clear();
    }

    void UpdateCooldowns()
    {
        for (int i = 0; i < _timesUntilReady.Length; i++)
        {
            if (_timesUntilReady[i] > 0) _timesUntilReady[i] -= Time.deltaTime;
        }
    }
    bool OnCooldown(float timeUntil) { return timeUntil > 0; }
}
