using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseItem : MonoBehaviour
{
    public Color itemColor;

    private void Awake() => itemColor = GetComponent<Image>().color;
}
