using System.Linq;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class LightLine : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    private void Awake()=>_lineRenderer = GetComponent<LineRenderer>();
    public class BinaryTreeNode<T>
    {
        public T Data;
        public BinaryTreeNode<T> Left;
        public BinaryTreeNode<T> Right;
        public Color ReflectColor;
        public Color TransmitColor;
        public BinaryTreeNode(T data)
        {
            Data = data;
            Left = null;
            Right = null;
            ReflectColor=Color.white;
            TransmitColor=Color.black;
        }
    }   
    /// <summary>
     /// 反射生成线，动画
     /// </summary>
     /// <param name="direction"></param>
     /// <param name="startPoint"></param>
     /// <param name="segments">光线段数，为反射次数加一</param>
     /// <param name="color">入射光颜色</param>
     /// <param name="duration">总时间</param>
    public void InitializeDotween(Vector3 startPoint,Vector3 direction,  Color color,int segments,float duration)
     {
         BinaryTreeNode<Vector3> lightPoint = new BinaryTreeNode<Vector3>(startPoint);
         lightPoint.ReflectColor=color;
         ReflectAndGetPoint(lightPoint, direction, segments,1, color,false,default(RaycastHit));
         RenderLineWithDotween(lightPoint,segments,duration);
     }
    /// <summary>
    /// 起点终点动画
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <param name="color"></param>
    /// <param name="duration"></param>
    /// <param name="callback">结束后回调</param>
    private void InitializeDotween(Vector3 startPoint, Vector3 endPoint,Color color,float duration,TweenCallback callback)
    {
        _lineRenderer.startColor=color;
        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, startPoint);

        DOTween.To(() => 0, x =>
        {
            Vector3 targetPos = (endPoint- startPoint).normalized * x +startPoint;
            _lineRenderer.SetPosition(1, targetPos);
        }, (endPoint- startPoint).magnitude, duration).SetEase(Ease.Linear).onComplete+=callback;
    }
    private void RenderLineWithDotween(BinaryTreeNode<Vector3> node,int segments ,float duration)
    {
         PlayDotween(node,duration/segments);
    }
    private void PlayDotween(BinaryTreeNode<Vector3> node,float durationPerLine)
    {
        GameObject obj;
        LineRenderer lineRenderer;
        if (node.Left != null)
        {
            obj = Instantiate(gameObject, transform.position, quaternion.identity, transform);
            lineRenderer=obj.GetComponent<LineRenderer>();
            lineRenderer.material.color=node.ReflectColor;
            GameObject backup = obj;
            lineRenderer.GetComponent<LightLine>().InitializeDotween(node.Data,node.Left.Data,node.ReflectColor,durationPerLine,()=>
            {
               backup.GetComponent<LightLine>().PlayDotween(node.Left, durationPerLine);
            });
        }

        if (node.Right != null)
        {
            obj = Instantiate(gameObject, transform.position, quaternion.identity, transform);
            lineRenderer=obj.GetComponent<LineRenderer>();
            lineRenderer.material.color=node.TransmitColor;
            lineRenderer.GetComponent<LightLine>().InitializeDotween(node.Data,node.Right.Data,node.TransmitColor,durationPerLine,()=>
            {
                obj.GetComponent<LightLine>().PlayDotween(node.Right, durationPerLine);
            });
        }
    }
    /// <summary>
    /// 每次调用是以某节点开始找下一个点
    /// </summary>
    /// <param name="node">本节点</param>
    /// <param name="direction">下一个方向</param>
    /// <param name="segments">总段数</param>
    /// <param name="currentSegment">下一条线的段索引</param>
    /// <param name="inColor">上一条的光颜色</param>
    /// <param name="isTransmit">下一条线是不是透光</param>
    private void ReflectAndGetPoint(BinaryTreeNode<Vector3> node, Vector3 direction,int segments,int currentSegment,Color inColor,bool isTransmit,RaycastHit lastHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(node.Data, direction, 1000f).OrderBy(h => h.distance).ToArray();
        if (hits.Length == 0||currentSegment == segments) return;
        
        RaycastHit hit = hits[isTransmit ? 1 : 0];

        Vector3 nextDirection = direction - 2 * Vector3.Dot(direction , hit.normal) * hit.normal;
        
        if (!isTransmit)
        {
            node.Left = new BinaryTreeNode<Vector3>(hit.point);
        }
        else
        {
            node.Right=new BinaryTreeNode<Vector3>(hit.point); 
        }
        node.ReflectColor = lastHit.collider? CalculateReflectedColor(inColor, lastHit.transform.GetComponent<BaseItem>().itemColor):inColor;
        node.TransmitColor = lastHit.collider? CalculateTransmittedColor(inColor, lastHit.transform.GetComponent<BaseItem>().itemColor):inColor;
        
        switch (hit.collider.gameObject.tag)
        {
            case "Edge":
                if (node.TransmitColor == Color.black && isTransmit)node.Right = null;
                if(node.ReflectColor==Color.black&&!isTransmit) node.Left = null;
                return;
            case "DichroicMirror"://二色镜
                if (isTransmit)
                {
                    if(node.ReflectColor!=Color.black)ReflectAndGetPoint(node.Right,nextDirection,segments,currentSegment+1,node.TransmitColor, false,hit);
                    if(node.ReflectColor!=Color.black) ReflectAndGetPoint(node.Right,direction,segments,currentSegment+1,node.TransmitColor, true,hit);
                }
                else
                {
                    if(node.ReflectColor!=Color.black)ReflectAndGetPoint(node.Left,nextDirection,segments,currentSegment+1,node.ReflectColor, false,hit);
                    if(node.ReflectColor!=Color.black) ReflectAndGetPoint(node.Left,direction,segments,currentSegment+1,node.ReflectColor, true,hit);
                }
                return;
            case "Filter":
                if (isTransmit)
                {
                    if(node.TransmitColor!=Color.black) ReflectAndGetPoint(node.Right,direction,segments,currentSegment+1,node.TransmitColor, true,hit);
                }
                else
                {
                    if(node.ReflectColor!=Color.black) ReflectAndGetPoint(node.Left,nextDirection,segments,currentSegment+1,node.ReflectColor, true,hit);
                }
                return;
            default:
                if (!isTransmit)
                {
                    if(node.ReflectColor!=Color.black)ReflectAndGetPoint(node.Left,nextDirection,segments,currentSegment+1,node.ReflectColor, false,hit);
                }
                else
                {
                    if(node.TransmitColor!=Color.black) ReflectAndGetPoint(node.Right,direction,segments,currentSegment+1,node.TransmitColor, true,hit);
                }
                return;
        }
    }
    private Color CalculateTransmittedColor(Color lightc, Color filterc)
    {
        // 如果入射光中包含滤光片允许的波长
        float transmittedR = lightc.r * filterc.r; // 透过的部分
        float transmittedG = lightc.g * filterc.g;
        float transmittedB = lightc.b * filterc.b;
        
        return new Color(transmittedR, transmittedG, transmittedB);
    }
    private Color CalculateReflectedColor(Color lightc, Color transmittedc)
    {
        float reflectedR = lightc.r - transmittedc.r;
        float reflectedG = lightc.g - transmittedc.g;
        float reflectedB = lightc.b - transmittedc.b;

        // 确保反射的 RGB 值不小于 0
        reflectedR = Mathf.Max(reflectedR, 0);
        reflectedG = Mathf.Max(reflectedG, 0);
        reflectedB = Mathf.Max(reflectedB, 0);

        return new Color(reflectedR, reflectedG, reflectedB);
    }

    // private bool ss = true;
    // private void Update()
    // {
    //     if (ss)
    //     {
    //         InitializeDotween(new Vector3(1,0,0),transform.position,3,Color.blue,10f);
    //         ss = false;
    //     }
    //    
    // }
}
