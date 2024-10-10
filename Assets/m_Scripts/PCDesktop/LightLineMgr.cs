using UnityEngine;

public class LightLineMgr : MonoBehaviour
{
    public GameObject lightLine;//lineRenderer预制体
    /// <summary>
    /// 光线管理
    /// </summary>
    /// <param name="start">起始位置</param>
    /// <param name="end">终止位置</param>
    /// <param name="color"></param>
    /// <param name="duration">持续时间，0即直接画出，默认为0</param>
    /// <param name="segments">平行线个数，默认为1</param>
    /// <param name="width">总宽度，仅段数时大于1生效</param>
    /// <param name="thickness">单线宽度</param>
    /// <param name="reflectTimes">反射次数，默认不反射为0</param>
    public static void DrawLine(LineRenderer lightSource, Vector3 end, Color color, float width,float thickness, float duration=0, int segments=1,int reflectTimes=0)
    {
        if (duration!=0)
        {
            
        }
        else
        {
            
        }
    }
    /// <summary>
    /// 画出单条光线
    /// </summary>
    /// <param name="thickness"></param>
    /// <param name="reflectTimes">反射次数</param>
    private static void DrawSingleLine(Vector3 start, Vector3 end, Color color, float thickness, float duration,int reflectTimes)
    {
        
    }
    private static void DrawSingleLine(Vector3 direction, Color color, float thickness, float duration, int reflectTimes)
    {
        
    }
    private static (Vector3[], Vector3[]) GetPoints(Vector3 start, Vector3 end, float width, int segments)
    {
        Vector3[] startPoints = new Vector3[segments];
        Vector3[] endPoints = new Vector3[segments];
        Vector3 direction = Quaternion.Euler(90,0,0)*(end - start).normalized;
        float lenPerClip=width / segments;
        Vector3 startPoint;
        Vector3 endPoint;
        if (segments%2==0)
        {
            startPoint=start+direction*((segments-1)/2+0.5f)*lenPerClip;
            endPoint=end+direction*((segments-1)/2+0.5f)*lenPerClip;
        }
        else
        {
            startPoint=start+direction*((segments-1)/2)*lenPerClip;
            endPoint=end+direction*((segments-1)/2)*lenPerClip;
        }
        for (int i = 0; i < segments; i++)
        {
            startPoints[i] = new Vector3(startPoint.x,startPoint.y,startPoint.z);
            endPoints[i] = new Vector3(endPoint.x,endPoint.y,endPoint.z);
            startPoint-=direction*lenPerClip;
            endPoint-=direction*lenPerClip;
        }
        return (startPoints,endPoints);
    }
}
