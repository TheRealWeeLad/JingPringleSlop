using UnityEngine;

public class AbilityProperties : MonoBehaviour
{
    public static LayerMask HitLayers { get; private set; }

    private void Awake()
    {
        HitLayers = LayerMask.GetMask("Default", "Ground", "Solid", "Enemy");
    }
}
