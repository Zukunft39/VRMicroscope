using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenMove : MonoBehaviour
{
    public float moveSpeed = 5f; // 控制移动速度

    void Update()
    {
        if (gameObject.transform.childCount>0)
        {
            GameObject Object=gameObject.transform.GetChild(0).gameObject;
            if (Object!=null)
            {
                // 获取水平方向 (A 和 D 或左右箭头)
                float horizontal = Input.GetAxisRaw("Horizontal");
                // 获取垂直方向 (W 和 S 或上下箭头)
                float vertical = Input.GetAxisRaw("Vertical");
                // 计算移动方向
                Vector3 moveDirection = new Vector3(horizontal, vertical, 0f).normalized;
                // 移动物体
                Object.transform.Translate(moveDirection * (moveSpeed * Time.deltaTime));
            }
        }
    }
}