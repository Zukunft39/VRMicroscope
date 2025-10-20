using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookOperation : MonoBehaviour
{
    public GameObject panel;
    public bool showOperation;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void toOperation()
    {
        panel.SetActive(true);
        showOperation = true;
    }
    
    public void hideOperation()
    {
        panel.SetActive(false);
        showOperation = false;
    }
}
