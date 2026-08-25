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
                float horizontal = 0f;
                if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
                if (Input.GetKey(KeyCode.D)) horizontal += 1f;

                float vertical = 0f;
                if (Input.GetKey(KeyCode.S)) vertical -= 1f;
                if (Input.GetKey(KeyCode.W)) vertical += 1f;
                // 计算移动方向
                Vector3 moveDirection = new Vector3(horizontal, vertical, 0f).normalized;
                // 移动物体
                Object.transform.Translate(moveDirection * (moveSpeed * Time.deltaTime));
            }
        }
    }
}
