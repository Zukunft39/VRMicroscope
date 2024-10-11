using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TInstance<T> : MonoBehaviour where T :TInstance<T>
{
    public static T Instance => _instance;

    private static T _instance;

    protected void Awake()
    {
        if (Instance == null)
        {
            _instance = (T)this;
        }
        else
            Destroy(gameObject);
    }
}