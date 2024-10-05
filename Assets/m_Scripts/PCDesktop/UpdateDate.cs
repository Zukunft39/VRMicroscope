using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpdateDate : MonoBehaviour
{
    void Update()
    {
        GetComponent<TextMeshProUGUI>().text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
