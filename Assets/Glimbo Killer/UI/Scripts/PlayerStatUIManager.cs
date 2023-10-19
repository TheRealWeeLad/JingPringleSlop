using UnityEngine;
using TMPro;

public class PlayerStatUIManager : MonoBehaviour
{
    public TextMeshProUGUI MoneyText;

    [HideInInspector] public static PlayerStatUIManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateInfo()
    {
        MoneyText.text = string.Format("${0}", PlayerStatManager.PlayerStats.Money);
    }
}
