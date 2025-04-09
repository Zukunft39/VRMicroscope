using System;
using UnityEngine;
using UnityEngine.Rendering;
public class SemitransparentShadowsCtrl : MonoBehaviour
{
    // 抖动序列参数
    [SerializeField] private int _jitterSequenceLength = 8; // 8帧循环
    [SerializeField] private float _jitterScale = 0.5f;     // 抖动幅度（像素）
    
    private int _frameIndex;
    private Material _material;
    void Start()
    {
        _material=GetComponent<Renderer>().material;
    }
    void LateUpdate()
    {
        // 计算当前帧的抖动值
        Vector2 jitter = CalculateHaltonJitter(_frameIndex);
        _material.SetVector("_TAA_Jitter",jitter);
        // 更新帧索引
        _frameIndex = (_frameIndex + 1) % _jitterSequenceLength;
    }

    private Vector2 CalculateHaltonJitter(int index)
    {
        return new Vector2(
            (Halton((index % _jitterSequenceLength) + 1, 2) - 0.5f) * _jitterScale,
            (Halton((index % _jitterSequenceLength) + 1, 3) - 0.5f) * _jitterScale
        );
    }

    private float Halton(int index, int baseVal)
    {
        float result = 0f;
        float fraction = 1f / baseVal;
        while (index > 0)
        {
            result += (index % baseVal) * fraction;
            index /= baseVal;
            fraction /= baseVal;
        }
        return result;
    }
}