using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MicroUI : MonoBehaviour
{
    public static bool setTrue;
    Text text;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        if (setTrue)
        {
            text.enabled = true;
        }
        else
        {
            text.enabled = false;
        }
    }
}
