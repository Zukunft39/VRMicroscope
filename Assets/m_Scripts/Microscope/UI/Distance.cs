using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Distance : MonoBehaviour
{
    [SerializeField] private Microscope microscope;
    [SerializeField] private TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        if (text == null)
        {
            text = gameObject.GetComponent<TextMeshProUGUI>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        text.text = microscope.GetDistance().ToString() + "dm";
    }
}
