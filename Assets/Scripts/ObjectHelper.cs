using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectHelper
{
    public static Transform Clear(Transform transform)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        return transform;
    }
}
