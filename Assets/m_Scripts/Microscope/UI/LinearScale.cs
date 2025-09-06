using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LinearScale : MonoBehaviour
{
    [SerializeField] private Microscope microscope;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private bool isScale;
    
    private float timer;
    private int scale;
    private int lastScale = 0;
    
    // 基准值：假设在100倍放大下，1厘米代表10微米
    private const float BASE_MAGNIFICATION = 100f;
    private const float BASE_MICRONS_PER_CM = 10f;
    
    // Start is called before the first frame update
    void Start()
    {
        if (text == null)
        {
            text = gameObject.GetComponent<TextMeshProUGUI>();
        }
        
        // 初始更新一次文本
        UpdateScaleText();
    }

    // Update is called once per frame
    void Update()
    {
        if (microscope == null) return;
        
        scale = microscope.GetScale();
        
        // 只有当放大倍数发生变化时才更新文本
        if (scale != lastScale)
        {
            lastScale = scale;
            UpdateScaleText();
        }
    }
    
    private void UpdateScaleText()
    {
        if (text == null) return;

        if (isScale)
        {
            text.text = "×" +scale.ToString();
        }
        else
        {
            // 计算每厘米代表的实际长度（微米）
            float micronsPerCm = BASE_MICRONS_PER_CM * (BASE_MAGNIFICATION / scale);

            // 转换为纳米
            float nanometersPerCm = micronsPerCm * 1000f;

            // 根据数值大小选择合适的单位
            if (nanometersPerCm >= 1000)
            {
                // 使用微米单位
                text.text = $"{micronsPerCm:F1} μm";
            }
            else
            {
                // 使用纳米单位
                text.text = $"{nanometersPerCm:F0} nm";
            }
        }
    }
}