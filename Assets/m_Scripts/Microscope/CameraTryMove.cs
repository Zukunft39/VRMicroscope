using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTryMove : MonoBehaviour
{
    public float moveSpeed = 5f;  // 移动速度
    public float rotationSpeed = 2f;  // 旋转速度

    private float pitch = 0f;  // 垂直旋转角度
    private float yaw = 0f;  // 水平旋转角度


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;  // 锁定鼠标到屏幕中央
        Cursor.visible = false;  // 隐藏鼠标
    }

    // Update is called once per frame
    void Update()
    {
        MoveCamera();
        RotateCamera();
    }

    // 控制相机的移动
    void MoveCamera()
    {
        float moveX = Input.GetAxis("Horizontal");  // 获取水平轴的输入（A、D 或 左右箭头）
        float moveZ = Input.GetAxis("Vertical");    // 获取垂直轴的输入（W、S 或 上下箭头）

        // 根据输入来计算相机的移动方向
        Vector3 moveDirection = (transform.right * moveX + transform.forward * moveZ).normalized;

        // 移动相机
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }

    // 控制相机的旋转
    void RotateCamera()
    {
        // 获取鼠标的移动量
        float mouseX = Input.GetAxis("Mouse X");  // 水平移动（左右）
        float mouseY = Input.GetAxis("Mouse Y");  // 垂直移动（上下）

        // 调整旋转角度
        yaw += mouseX * rotationSpeed;  // 水平旋转
        pitch -= mouseY * rotationSpeed;  // 垂直旋转

        // 限制垂直旋转的角度（防止超出上下旋转范围）
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        // 应用旋转
        transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }
}
