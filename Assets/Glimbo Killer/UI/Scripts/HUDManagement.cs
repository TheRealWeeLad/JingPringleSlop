using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManagement : MonoBehaviour
{
    [Header("Ability Images")] // NAME = "{AbilityName}Image"
    public Sprite FireballImage;
    public Sprite LightningImage;
    public Sprite BombImage;

    [Header("HUD Icon Prefab")]
    public GameObject IconPrefab;

    // Ability Items
    static readonly List<RectTransform> _abilityOverlays = new();
    static readonly List<TextMeshProUGUI> _abilityTexts = new();

    public static Dictionary<ushort, Type> AbilityIds = new();

    // Active Abilities (Main, Secondary, etc.)
    static readonly List<Type> _activeAbilities = new();

    // Cooldown Control
    static readonly List<float> _abilityCooldowns = new();
    static readonly List<float> _abilityCooldownTimers = new();

    // CONSTANTS
    const float RECTHEIGHT = 50;
    static readonly float LIGHTALPHA = 100f / 255f;
    static readonly float DARKALPHA = 165f / 255f;

    void Awake()
    {
        // RESET LISTS
        _abilityOverlays.Clear();
        _abilityTexts.Clear();
        AbilityIds.Clear();
        _activeAbilities.Clear();
        _abilityCooldowns.Clear();
        _abilityCooldownTimers.Clear();

        // Set Abilities
        List<Ability> abilities = AbilityChooser.abilities;
        GameObject abilityBar = GameObject.Find("Ability Bar");
        for (ushort i = 0; i < abilities.Count; i++)
        {
            AbilityIds[i] = abilities[i].GetType();
            _activeAbilities.Add(AbilityIds[i]);

            // Initialize Ability Objects
            GameObject abilityObj = Instantiate(IconPrefab, abilityBar.transform);
            abilityObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-250 + 75 * i, 0);
            RectTransform abilityOverlay = abilityObj.transform.GetChild(1).GetComponent<RectTransform>();
            TextMeshProUGUI abilityText = abilityObj.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            _abilityOverlays.Add(abilityOverlay);
            _abilityTexts.Add(abilityText);

            // Set ability Images
            abilityObj.transform.GetChild(0).GetComponent<Image>().sprite = 
                typeof(HUDManagement).GetField(AbilityIds[i].Name + "Image").GetValue(this) as Sprite;
        }

        // Initialize Cooldown List
        foreach (Ability ability in abilities) {
            // Hold abilities dont have cooldowns
            if (ability.FiringMode == FireMode.HOLD)
            {
                // Maintain correct order in lists
                _abilityCooldowns.Add(0);
                _abilityCooldownTimers.Add(0);
                continue;
            }

            float cd = ability.Cooldown;
            _abilityCooldowns.Add(cd);
            _abilityCooldownTimers.Add(0);
        }
    }

    void Update()
    {
        if (UIManagement.IsPaused()) return;

        UpdateAbilityCooldowns();
    }

    void UpdateAbilityCooldowns()
    {
        for (ushort i = 0; i < _abilityCooldownTimers.Count; i++)
        {
            Type ab = AbilityIds[i];
            if (_activeAbilities.Find(x => x.IsEquivalentTo(ab)) == null) continue;
            if (_abilityCooldownTimers[i] > 0)
            {
                _abilityCooldownTimers[i] -= Time.deltaTime;
                // Shorten Overlay
                float currHeight = _abilityOverlays[i].rect.height;
                float offset = Time.deltaTime / _abilityCooldowns[i] * RECTHEIGHT;
                _abilityOverlays[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currHeight - offset);
                _abilityOverlays[i].anchoredPosition += offset * 0.5f * Vector2.down;
                _abilityTexts[i].text = ((int)_abilityCooldownTimers[i]).ToString();
            }
            else
            {
                _abilityCooldownTimers[i] = 0;
                _abilityTexts[i].text = "";
            }
        }
    }

    public static void ResetCooldown<T>() where T : Ability
    {
        ushort id = AbilityIds.FirstOrDefault(x => x.Value.IsEquivalentTo(typeof(T))).Key;
        _abilityCooldownTimers[id] = _abilityCooldowns[id];
        _abilityOverlays[id].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RECTHEIGHT);
        _abilityOverlays[id].anchoredPosition = RECTHEIGHT * 0.5f * Vector2.up;
        _abilityTexts[id].text = ((int)_abilityCooldowns[id]).ToString();
    }

    // PARAM light => whether to light up icon or darken
    public static void LightUpIcon<T>(bool light) where T : Ability
    {
        ushort id = AbilityIds.FirstOrDefault(x => x.Value.IsEquivalentTo(typeof(T))).Key;
        Image img = _abilityOverlays[id].GetComponent<Image>();

        if (light)
        {
            img.color = new Color(0, 1, 1, LIGHTALPHA);
            _abilityOverlays[id].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RECTHEIGHT);
            _abilityOverlays[id].anchoredPosition = RECTHEIGHT * 0.5f * Vector2.up;
        }
        else
        {
            img.color = new Color(0, 0, 0, DARKALPHA);
            _abilityOverlays[id].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
            _abilityOverlays[id].anchoredPosition += RECTHEIGHT * Vector2.down;
        }
    }
}
