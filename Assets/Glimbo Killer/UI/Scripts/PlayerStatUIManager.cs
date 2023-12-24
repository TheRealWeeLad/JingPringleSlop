using UnityEngine;
using TMPro;

public class PlayerStatUIManager : MonoBehaviour
{
    public TextMeshProUGUI MoneyText;
    public TextMeshProUGUI SpeedText;

    [HideInInspector] public static PlayerStatUIManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateInfo()
    {
        MoneyText.text = string.Format("${0}", PlayerStatManager.PlayerStats.Money);

        // Round speed to 3 digits
        float speed = PlayerStatManager.PlayerStats.Speed;
        if (speed >= 1 && speed < 1000)
        {
            int numDigits = Mathf.CeilToInt(Mathf.Log10(speed));
            speed *= Mathf.Pow(10, 3 - numDigits);
            speed = (int)speed / Mathf.Pow(10, 3 - numDigits);
        }
        else if (speed < 1)
        {
            speed = (int)(speed * 100) / 100;
        }
        else speed = (int)speed;
        SpeedText.text = speed.ToString();
    }
}
