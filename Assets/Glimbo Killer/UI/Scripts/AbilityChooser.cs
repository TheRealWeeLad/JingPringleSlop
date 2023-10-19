using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class AbilityChooser : MonoBehaviour
{
    public GameObject continueButton;

    const ushort MAXABILITIES = 3;

    readonly List<GameObject> _abilityObjects = new();
    public static readonly List<Ability> abilities = new();

    ushort idx = 0;

    private void Awake()
    {
        // Reset Lists
        _abilityObjects.Clear();
        abilities.Clear();
    }

    public void Choose(Ability ability)
    {
        GameObject abilityUIElement = GameObject.Find(string.Format("{0} Ability", ability.GetType()));
        if (abilityUIElement == null) { Debug.LogWarning("Ability Not Found"); return; }
        if (_abilityObjects.Contains(abilityUIElement)) return;
        abilityUIElement.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (idx + 1).ToString();

        if (_abilityObjects.Count == MAXABILITIES) // Loop Around
        {
            _abilityObjects[idx].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            _abilityObjects.RemoveAt(idx);
            abilities.RemoveAt(idx);
        }

        _abilityObjects.Insert(idx, abilityUIElement);
        abilities.Insert(idx, ability);
        idx++;
        idx %= MAXABILITIES;
    }

    public void ResetAbilities()
    {
        foreach (GameObject ability in _abilityObjects)
        {
            ability.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
        }
        _abilityObjects.Clear();
        abilities.Clear();
        idx = 0;
    }
}
