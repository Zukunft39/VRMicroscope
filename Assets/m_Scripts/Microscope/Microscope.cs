using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;

public class Microscope : MonoBehaviour
{
    #region 物体和组件
    public GameObject Line;    //光线物体
    public GameObject Light;  //灯光
    public GameObject show;
    public GameObject point1; //相机点位坐标
    public GameObject point2;
    public GameObject knob1;
    public GameObject showCamera;  
    public GameObject lookCamera;  
    public GameObject screen;
    public GameObject microscopeCamera;
    public GameObject glass4;
    public List<int> glass4Rotation = new List<int>();
    int[] glass4Size = { 10, 2, 1 };
    int glass4Choice = 0;
    GameObject Object;  //玩家放的物体
    public GameObject Cam; 
    GameObject player;
    private LineRenderer lineRenderer;
    #endregion

    #region 数值和工具类变量
    public int pointer; 
    public float widthChangeAmount = 0.05f; // 每次滚动改变的宽度量
    public float minWidth = 0.01f; // 最小宽度
    public float maxWidth = 0.4f; // 最大宽度
    public float rotationChangeRatio = 10f; // 每次宽度变化对应的旋转增量
    public float minRotationZ = 45f; // z轴旋转的最小值
    public float maxRotationZ = 135f;  // z轴旋转的最大值
    int p = 0;
    bool isNear;
    public float rotationAmount = 10f; // 每次滚动旋转的角度

    private bool isRotating = false;
    private int targetGlass4Choice;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float rotationProgress;
    private float sizeChangeSpeed = 2.0f;
    private float rotationSpeed = 90.0f; // 每秒旋转90度
    #endregion

    // Start 在游戏开始时调用一次
    void Start()
    {
        pointer = 0;
        lineRenderer = Line.GetComponent<LineRenderer>(); // 获取 LineRenderer 组件
        lineRenderer.enabled = false;
        SetLaserPositions();
        Light.SetActive(false);
        show.SetActive(false);
        Cam.SetActive(false);
        screen.SetActive(false);
    }
    void Update()
    {
        //光源开关
        if (MicroUI.setTrue && Input.GetKeyDown(KeyCode.Q) && isNear)
        {
            lineRenderer.enabled = !lineRenderer.enabled;
            Light.SetActive(!Light.activeSelf);
            Cam.SetActive(!Cam.activeSelf);
            screen.SetActive(!screen.activeSelf);
        }

        //交互
        if (MicroUI.setTrue && Input.GetKeyDown(KeyCode.E) && isNear)
        {
            if (Object == null)
            {
                if (player.transform.childCount > 0)
                {
                    Object = player.transform.GetChild(0).gameObject;
                    Object.transform.SetParent(Cam.transform);
                    Object.transform.localPosition = Vector3.zero;
                    Object.transform.localRotation = Quaternion.identity;
                    show.SetActive(true);
                }
            }
            else
            {
                if (pointer == 0)
                {
                    showCamera.SetActive(true);
                    showCamera.transform.position = player.transform.position;
                    showCamera.transform.rotation = player.transform.rotation;
                    pointer++;
                }
                else
                {
                    pointer++;
                }
            }
        }

        //取下物体
        if (MicroUI.setTrue && Input.GetKeyDown(KeyCode.R) && isNear)
        {
            if (Object != null)
            {
                Object.transform.SetParent(player.transform);
                Object = null;
                show.SetActive(false);
            }
        }

        // 修改原有的T键检测部分
        if (MicroUI.setTrue && Input.GetKeyDown(KeyCode.T) && isNear && !isRotating)
        {
            // 确保数组长度一致并循环索引
            int maxChoice = Mathf.Min(glass4Rotation.Count, glass4Size.Length);
            targetGlass4Choice = (glass4Choice + 1) % maxChoice;

            // 启动旋转过渡
            StartCoroutine(SwitchObjectiveLens());
            glass4Choice = targetGlass4Choice;
        }

        if (MicroUI.setTrue && isNear)
        {
            // 获取鼠标滚轮输入
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (scrollInput != 0) // 如果滚动量不为零
            {
                // 根据滚轮方向调整激光的宽度
                float newWidth = Mathf.Clamp(lineRenderer.startWidth + scrollInput * widthChangeAmount, minWidth, maxWidth);
                lineRenderer.startWidth = newWidth;
                lineRenderer.endWidth = newWidth;

                // 计算与宽度变化相对应的旋转角度
                float rotationAmount = scrollInput * rotationChangeRatio;
                Vector3 currentRotation = knob1.transform.rotation.eulerAngles;

                // 计算新的旋转角度
                float newRotationZ = currentRotation.z + rotationAmount;

                // 限制旋转角度在[minRotationZ, maxRotationZ]之间
                newRotationZ = Mathf.Clamp(newRotationZ, minRotationZ, maxRotationZ);

                // 设置旋转，只改变z轴，保持x和y不变
                knob1.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, newRotationZ);
            }
        }

        if (showCamera.activeSelf)
        {
            // 目标位置和旋转
            Vector3 targetPosition;
            Quaternion targetRotation;

            // 根据pointer的值决定目标位置和旋转
            if (pointer == 1)
            {
                targetPosition = point1.transform.position;
                targetRotation = point1.transform.rotation;
            }
            else
            {
                targetPosition = point2.transform.position; // 假设有point2作为备用目标位置
                targetRotation = point2.transform.rotation; // 假设point2也有旋转
                if (p==0)
                {
                    MicroBlack.ToBlack = true;
                    p++;
                }

            }
            // 使用MoveTowards平滑过渡位置
            showCamera.transform.position = Vector3.MoveTowards(showCamera.transform.position, targetPosition, 10 * Time.deltaTime);

            // 使用RotateTowards平滑过渡旋转
            showCamera.transform.rotation = Quaternion.RotateTowards(showCamera.transform.rotation, targetRotation, 120 * Time.deltaTime);
            if (Vector3.Distance(showCamera.transform.position,point2.transform.position)<0.1f)
            {
                showCamera.SetActive(false);
                lookCamera.SetActive(true);
                //GameObject Object = microscopeCamera.transform.GetChild(0).gameObject;
                //Vector3 currentPosition = Object.transform.localPosition;
                //currentPosition.x = 0f;
                //currentPosition.y = 0f;
                //Object.transform.localPosition = currentPosition;
                MicroUI.setTrue = false;
                p = 0;
                player.SetActive(false);
            }
        }

        if(lookCamera.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                lookCamera.SetActive(false);
                pointer = 0;
                MicroUI.setTrue = true;
                player.SetActive(true);
            }
        }
    }

    void SetLaserPositions()
    {
        // 确保 Line 至少有两个子物体
        if (Line.transform.childCount >= 2)
        {
            // 获取第一个和第二个子物体的位置
            Vector3 startPoint = Line.transform.GetChild(0).position;
            Vector3 endPoint = Line.transform.GetChild(1).position;

            // 设置 LineRenderer 的起始点和结束点
            lineRenderer.SetPosition(0, startPoint); // 设置起始点
            lineRenderer.SetPosition(1, endPoint);   // 设置结束点
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
            MicroUI.setTrue=true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.gameObject;
            isNear = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MicroUI.setTrue = false;
            isNear = false;
        }
    }

    // 新增协程处理平滑过渡
    IEnumerator SwitchObjectiveLens()
    {
        isRotating = true;

        // 记录初始值和目标值
        float startSize = microscopeCamera.GetComponent<Camera>().orthographicSize;
        float targetSize = glass4Size[targetGlass4Choice];
        Quaternion startRot = glass4.transform.localRotation;
        Quaternion targetRot = Quaternion.Euler(
            glass4.transform.localEulerAngles.x,
            glass4.transform.localEulerAngles.y,
            glass4Rotation[targetGlass4Choice]
        );

        float duration = 0.5f; // 过渡总时长
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 平滑旋转
            glass4.transform.localRotation = Quaternion.Slerp(
                startRot,
                targetRot,
                t
            );

            // 平滑改变相机尺寸
            microscopeCamera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(
                startSize,
                targetSize,
                t
            );

            yield return null;
        }

        // 确保最终值准确
        glass4.transform.localRotation = targetRot;
        microscopeCamera.GetComponent<Camera>().orthographicSize = targetSize;
        isRotating = false;
    }
}
