using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Idea from: SEBASTIAN LAGUE
public class PortalEntity
{
    readonly Transform transform;
    
    public PortalEntity(Transform transform)
    {
        this.transform = transform;
    }

    // TODO: FIX
    public override int GetHashCode()
    {
        return transform.GetHashCode();
    }
}
