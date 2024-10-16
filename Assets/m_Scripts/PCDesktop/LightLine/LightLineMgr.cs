using System.Collections.Generic;
using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

public class LightLineMgr : TInstance<LightLineMgr>
{
    public static GameObject lightLine;//lineRenderer预制体
    public static List<GameObject> lightLines = new List<GameObject>();
    public Vector3 direction;
    public int num;
    public Vector3 start;
    [SerializeField]
    public float duration=1000f;


    protected void Awake()
    {
        base.Awake();
        lightLine=Resources.Load("Prefab/LightLineRenderer") as GameObject;
    }
    #region 动画绘制
    // public static void DrawLineWithAnim(Vector3 start,Vector3 end, Color color,int num,float width, float duration)
    // {
    //     GameObject temp;
    //     if (num == 1)
    //     {
    //         temp = Instantiate(lightLine, LightLineMgr.Instance.transform.position, Quaternion.identity,
    //                     LightLineMgr.Instance.transform);
    //         temp.GetComponent<LightLine>().InitializeDotween(start,end,color,duration);
    //         
    //         lightLines.Add(temp);
    //     }
    //     else
    //     {
    //         Vector3[] starts;
    //         Vector3[] ends;
    //         (starts,ends)= GetPoints(start, end, width, num);
    //         for (int i = 0; i < num; i++)
    //         {
    //             temp= Instantiate(lightLine, LightLineMgr.Instance.transform.position, Quaternion.identity,
    //                 LightLineMgr.Instance.transform);
    //             lightLines.Add(temp);
    //             temp.GetComponent<LightLine>().InitializeDotween(starts[i], ends[i], color, duration);
    //         }
    //     }
    // }
    public static void DrawLineWithAnim(Vector3 start,Vector3 direction, Color color,int num,float width, float duration,int segments)
    {
        GameObject temp;
        if (num == 1)
        {
            temp = Instantiate(lightLine, LightLineMgr.Instance.transform.position, Quaternion.identity,
                LightLineMgr.Instance.transform);
            lightLines.Add(temp);
            temp.GetComponent<LightLine>().InitializeDotween(start,direction,color,segments,duration);
        }
        else
        {
            Vector3[] starts=GetPoints(start, direction, width, num,1f);
            foreach (var s in starts)
            {
                temp=Instantiate(lightLine, LightLineMgr.Instance.transform.position, Quaternion.identity,
                        LightLineMgr.Instance.transform);
                lightLines.Add(temp);
                temp.GetComponent<LightLine>().InitializeDotween(s,direction,color,segments,duration);
            }
            
        }
    }

    #endregion
    
    
    public void RemoveLightLine()
    {
        foreach (var g in lightLines)
        {
            Destroy(g);
        }
        lightLines.Clear();
    }

    private static Vector3[] GetPoints(Vector3 start, Vector3 direction, float width,int segments,float duration)
    {
        Vector3[] startPoints = new Vector3[segments];
        float lenPerClip=width / segments;
        Vector3 startPoint;
        Vector3 endPoint;
        if (segments%2==0)
        {
            startPoint=start+direction*((segments-1)/2+0.5f)*lenPerClip;
        }
        else
        {
            startPoint=start+direction*((segments-1)/2)*lenPerClip;
        }
        for (int i = 0; i < segments; i++)
        {
            startPoints[i] = new Vector3(startPoint.x,startPoint.y,startPoint.z);
            startPoint-=direction*lenPerClip;
        }

        return startPoints;
    }
    // private static (Vector3[], Vector3[]) GetPoints(Vector3 start, Vector3 end, float width, int segments)
    // {
    //     Vector3[] startPoints = new Vector3[segments];
    //     Vector3[] endPoints = new Vector3[segments];
    //     Vector3 direction = Quaternion.Euler(90,0,0)*(end - start).normalized;
    //     float lenPerClip=width / segments;
    //     Vector3 startPoint;
    //     Vector3 endPoint;
    //     if (segments%2==0)
    //     {
    //         startPoint=start+direction*((segments-1)/2+0.5f)*lenPerClip;
    //         endPoint=end+direction*((segments-1)/2+0.5f)*lenPerClip;
    //     }
    //     else
    //     {
    //         startPoint=start+direction*((segments-1)/2)*lenPerClip;
    //         endPoint=end+direction*((segments-1)/2)*lenPerClip;
    //     }
    //     for (int i = 0; i < segments; i++)
    //     {
    //         startPoints[i] = new Vector3(startPoint.x,startPoint.y,startPoint.z);
    //         endPoints[i] = new Vector3(endPoint.x,endPoint.y,endPoint.z);
    //         startPoint-=direction*lenPerClip;
    //         endPoint-=direction*lenPerClip;
    //     }
    //     return (startPoints,endPoints);
    // }
}
