using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class LightLine : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    private void Awake()=>_lineRenderer = GetComponent<LineRenderer>();

    public void Initialize(Vector3 startPoint, Vector3 endPoint,Color color)
    {
        _lineRenderer.startColor=color;
        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, startPoint);
        _lineRenderer.SetPosition(1, endPoint);
    }
    /// <summary>
    /// 反射生成线
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="startPoint"></param>
    /// <param name="segments">光线段数，为反射次数加一</param>
    /// <param name="color"></param>
    public void Initialize(Vector3 direction, Vector3 startPoint,int segments, Color color)
    {
        try
        {
            List<Vector3> points = new List<Vector3>(){startPoint};
            
            ReflectAndGetPoint(points, startPoint, direction, segments, 1);
            foreach (var VARIABLE in points)
           {
               Debug.Log(VARIABLE.ToString());
           }
            _lineRenderer.startColor=color;
            _lineRenderer.positionCount = points.Count;
            
            for (int i = 0; i < _lineRenderer.positionCount; i++)
            {
                _lineRenderer.SetPosition(i, points[i]);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("2222222222222");
            throw;
        }
    }
    public void InitializeDotween(Vector3 startPoint, Vector3 endPoint,Color color,float duration)
    {
        _lineRenderer.startColor=color;
        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, startPoint);
        _lineRenderer.SetPosition(1, endPoint);
    }
    /// <summary>
    /// 反射生成线
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="startPoint"></param>
    /// <param name="segments">光线段数，为反射次数加一</param>
    /// <param name="color"></param>
    public void InitializeDotween(Vector3 direction, Vector3 startPoint,int segments, Color color,float duration)
    {
        List<Vector3> points = new List<Vector3>(){startPoint};
        ReflectAndGetPoint(points, startPoint, direction, segments, 1);
        foreach (var v in points)
        {
            Debug.Log(v.ToString());
        }
        _lineRenderer.startColor=color;
        _lineRenderer.positionCount = 2;
        _lineRenderer.SetPosition(0, startPoint);
        Sequence sequence = DOTween.Sequence();
        for (int i = 1; i < points.Count; ++i)
        {
            Vector3 start = points[i - 1];
            Vector3 end = points[i];
            int select = i;
            sequence.Append(DOTween.To(() => 0, x =>
            {
                Vector3 targetPos = (end - start).normalized * x + start;
                if (_lineRenderer.positionCount <= select)
                    _lineRenderer.positionCount += 1;
                _lineRenderer.SetPosition(select, targetPos);
                Console.WriteLine(targetPos.ToString());
            }, (end - start).magnitude, duration/segments).SetEase(Ease.Linear));
        }

        foreach (var v in points)
        {
            Console.WriteLine(v.ToString());
        }
        sequence.SetAutoKill(false);
        sequence.Play();
    }
    private void ReflectAndGetPoint(List<Vector3> result,Vector3 point, Vector3 direction,int segments,int currentSegment)
    {
        try
        {
            RaycastHit hit;
            if (currentSegment < segments)
            {
                if (Physics.Raycast(point, direction, out hit))
                {
                    Debug.Log(currentSegment+"reflection");
                    result.Add(hit.point);
                    Vector3 nextDirection = direction - 2 * Vector3.Dot(direction , hit.normal) * hit.normal;
                    ReflectAndGetPoint(result, hit.point, nextDirection, segments, currentSegment + 1);
                }

                
            }
            else if (currentSegment == segments)
            {
                if (Physics.Raycast(point, direction, out hit))
                {
                    result.Add(hit.point);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("1111111111");
            throw;
        }
        
    }

    private bool ss = true;
    private void Update()
    {
        if (ss)
        {
            InitializeDotween(new Vector3(1,0,0),transform.position,3,Color.blue,10f);
            ss = false;
        }
       
    }
}
