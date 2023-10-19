using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    // Cooldown in Seconds
    public virtual ushort Cooldown { get; }
    public virtual float AOERadius { get; }
    public abstract FireMode FiringMode { get; }
}

public enum FireMode { PRESS, HOLDANDRELEASE, HOLD }