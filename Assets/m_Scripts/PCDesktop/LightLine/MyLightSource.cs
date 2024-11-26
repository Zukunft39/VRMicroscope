using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MyLightSource : MonoBehaviour
{
    protected GameObject LightLine;
    protected List<GameObject> LightLines;
    public Color lightColor=Color.white;
    protected void Awake()
    {
        LightLine=Resources.Load("Prefab/LightLineRenderer") as GameObject;
        LightLines = new List<GameObject>();
    }
    public abstract void DrawLightLine();
    public int lightIntensity=10;//每采样区单位光线数（即采样值为1时）
}
