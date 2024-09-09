using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TInstance<T> : MonoBehaviour where T :TInstance<T>
{
    public static T Instance
    {
        get 
        { return instance; }
    }

    private static T instance;

    private void Awake()
    {
        if (Instance == null)
        {
            instance = (T)this;
        }
        else
        Destroy(gameObject);
    }
}