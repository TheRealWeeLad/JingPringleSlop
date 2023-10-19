using UnityEngine;

public class PlayerStatManager : MonoBehaviour
{
    static readonly PlayerStats DefaultStats = new(100, 100, 1f, 0);
    public static PlayerStats PlayerStats = DefaultStats;
}

public struct PlayerStats
{
    public int Health { get; set; }
    public uint MaxHealth { get; set; }
    public float Speed { get; set; }
    public uint Money { get; set; }

    public PlayerStats(int health, uint maxHealth, float speed, uint money)
    {
        Health = health;
        MaxHealth = maxHealth;
        Speed = speed;
        Money = money;
    }
}