using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 平行光源，光方向为forward
/// </summary>
public class SquareLightSource : MyLightSource
{
    public int sampleCount = 50;
    public int reflectionBounces = 3;//反射次数
    public float duration = 0;
    public float width;
    public AnimationCurve linedistribution;
    public override void DrawLightLine()
    {
        GameObject temp;
        List<Vector3> starts=GetPoints();
        foreach (var s in starts)
        {
            temp=Instantiate(LightLine, transform.position, Quaternion.identity,transform);
            LightLines.Add(temp);
            temp.GetComponent<LightLine>()
                .InitializeDotween(s, transform.forward, lightColor, reflectionBounces + 1, duration);
        }
    }
    private List<Vector3> GetPoints()
    {
        List<Vector3> startPoints = new List<Vector3>();
        Vector3 startPos=new Vector3(transform.position.x,transform.position.y,transform.position.z-width/2f);
        float lenPerSegment = width/sampleCount;//每段采样单位的长度
        float lineGap;//单位内光线间隔
        int lineCount;
        for (int i = 0; i < sampleCount+1; i++)
        {
            lineCount=(int)(linedistribution.Evaluate((float)i/sampleCount)*lightIntensity);
            lineGap=lenPerSegment/lineCount;
            for (int j = 0; j < lineCount; j++)
            {
                startPoints.Add(startPos);
                startPos+=new Vector3(0,0,lineGap);
            }
        }
        return startPoints;
    }
    public void RemoveLightLine()
    {
        foreach (var g in LightLines)
        {
            Destroy(g);
        }
        LightLines.Clear();
    }
}
