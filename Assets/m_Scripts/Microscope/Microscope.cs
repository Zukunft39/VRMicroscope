using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class Microscope : MonoBehaviour
{
    #region 物体和组件

    public GameObject Line; //光线物体
    public GameObject Light; //灯光
    public GameObject show;
    public GameObject point1; //相机点位坐标
    public GameObject point2;
    public GameObject knob1;
    public GameObject showCamera;
    public GameObject lookCamera;
    public GameObject screen;
    public GameObject microscopeCamera;
    public GameObject glass4;
    public Slider slider;
    public List<int> glass4Rotation = new List<int>();
    int[] glass4Size = { 5, 10, 50, 100 };
    int[] aperture = { 16, 10, 6, 1 };
    int glass4Choice = 0;
    GameObject Object; //玩家放的物体
    public GameObject Cam;
    GameObject player;
    private LineRenderer lineRenderer;
    public Material screenMaterial;

    #endregion

    #region 数值和工具类变量

    public int pointer;
    public float widthChangeAmount = 0.05f; // 每次滚动改变的宽度量
    public float minWidth = 0.01f; // 最小宽度
    public float maxWidth = 0.4f; // 最大宽度
    public float rotationChangeRatio = 10f; // 每次宽度变化对应的旋转增量
    public float minRotationZ = 45f; // z轴旋转的最小值
    public float maxRotationZ = 135f; // z轴旋转的最大值
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

    #region 焦距调整相关变量

    private Volume volume; // 后处理Volume组件
    private DepthOfField depthOfField; // 景深效果组件
    private bool isCoarseAdjust = true; // 是否为粗调模式
    float coarseStep = 2f; // 粗调步长
    float fineStep = 0.2f; // 细调步长
    float minFocal = 1f; // 最小焦距
    float maxFocal = 50f; // 最大焦距
    float distance; //目镜和底座距离
    float currentFocal; // 当前焦距值
    bool change;
    int focalChangeSpeed = 1; // 速率
    float focalChangeInterval = 0.1f; // 每次调整间隔
    float lastAdjustmentTimeB = 0f;
    float lastAdjustmentTimeN = 0f;
    Vector3 ObjectInitialScale = Vector3.zero;

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
        StartCoroutine(SwitchObjectiveLens());
        change = false;
        volume = lookCamera.GetComponent<Volume>();
        if (volume != null && volume.profile.TryGet(out depthOfField))
        {
            currentFocal = depthOfField.focusDistance.value;
        }

        MeshRenderer meshRenderer = screen.GetComponent<MeshRenderer>();
        meshRenderer.material = screenMaterial;
    }

    public void LightSwitch()
    {
        if (MicroUI.setTrue && isNear)
        {
            lineRenderer.enabled = !lineRenderer.enabled;
            Light.SetActive(!Light.activeSelf);
            screen.SetActive(!screen.activeSelf);
        }
    }

    public void PutAndObserve()
    {
        if (MicroUI.setTrue && isNear)
        {
            if (Object == null)
            {
                if (player.transform.childCount > 0)
                {
                    bool flag = false;
                    int i;
                    for (i = 0; i < player.transform.childCount; i++)
                    {
                        if (player.transform.GetChild(i).CompareTag("ObserveObjects"))
                        {
                            ObjectInitialScale = player.transform.GetChild(i).gameObject.transform.localScale;
                            flag = true;
                            break;
                        }
                    }

                    if (flag)
                    {
                        Cam.SetActive(true);
                        Object = player.transform.GetChild(i).gameObject;
                        Object.transform.SetParent(Cam.transform);
                        Object.transform.localPosition = Vector3.zero;
                        Object.transform.localRotation = Quaternion.identity;
                        Object.transform.localScale = new
                            Vector3(ObjectInitialScale.x / gameObject.transform.localScale.x,
                                ObjectInitialScale.y / gameObject.transform.localScale.x,
                                ObjectInitialScale.z / gameObject.transform.localScale.x);
                        show.SetActive(true);
                    }
                    else
                    {
                        Debug.Log("没有可释放物体");
                    }
                }
            }
            else
            {
                if (pointer == 0)
                {
                    showCamera.SetActive(true);
                    Cam.SetActive(true);
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
    }

    public void TakeOutobj()
    {
        if (MicroUI.setTrue && isNear)
        {
            if (Object != null)
            {
                Object.transform.localScale = new
                    Vector3(ObjectInitialScale.x,
                        ObjectInitialScale.y,
                        ObjectInitialScale.z);
                Object.transform.SetParent(player.transform);
                Object = null;
                show.SetActive(false);
            }
        }
    }

    public void RotateGlass()
    {
        if (MicroUI.setTrue && isNear && !isRotating)
        {
            // 确保数组长度一致并循环索引
            int maxChoice = Mathf.Min(glass4Rotation.Count, glass4Size.Length);
            targetGlass4Choice = (glass4Choice + 1) % maxChoice;

            // 启动旋转过渡
            StartCoroutine(SwitchObjectiveLens());
            glass4Choice = targetGlass4Choice;
        }
    }

    public void RotateGlassOnObserving()
    {
        if(!lookCamera.activeSelf)return;
        if (depthOfField != null)
        {
            depthOfField.focusDistance.value = 1;
        }
        // 确保数组长度一致并循环索引
        int maxChoice = Mathf.Min(glass4Rotation.Count, glass4Size.Length);
        targetGlass4Choice = (glass4Choice + 1) % maxChoice;

        // 启动旋转过渡
        StartCoroutine(SwitchObjectiveLens());
        glass4Choice = targetGlass4Choice;
    }

    public void AdjustLight(float scrollInput)
    {
        if (MicroUI.setTrue && isNear)
        {
            if (scrollInput == 0)
            {
                // 获取鼠标滚轮输入
                scrollInput = Input.GetAxis("Mouse ScrollWheel");
            }
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
    }

    public void QuitObserve()
    {
        lookCamera.SetActive(false);
        pointer = 0;
        MicroUI.setTrue = true;
        player.SetActive(true);
        Cam.SetActive(false);
    }

    public void SwitchModeOfChange()
    {
        if(!lookCamera.activeSelf)return;
        isCoarseAdjust = !isCoarseAdjust;
    }

    public void ChangeFocal(float mode)
    {
        if(!lookCamera.activeSelf)return;
        if (mode < 0)
        {
            if (Time.time - lastAdjustmentTimeB >= focalChangeInterval) // 控制调整速率
            {
                AdjustFocal(-focalChangeSpeed);
                lastAdjustmentTimeB = Time.time;
            }
        }
        else if(mode > 0)
        {
            if (Time.time - lastAdjustmentTimeN >= focalChangeInterval) // 控制调整速率
            {
                AdjustFocal(focalChangeSpeed);
                lastAdjustmentTimeN = Time.time;
            }
        }
    }
    void Update()
    {
        //光源开关
        if(Input.GetKeyDown(KeyCode.Q))LightSwitch();
        
        //交互
        if (Input.GetKeyDown(KeyCode.E))
        {
            PutAndObserve();
            if(lookCamera.activeSelf)QuitObserve();
        }
        //取下物体
        if(Input.GetKeyDown(KeyCode.R))TakeOutobj();

        // 修改原有的T键检测部分
        if( Input.GetKeyDown(KeyCode.T))RotateGlass();
        AdjustLight(0);
       

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
                Camera camera = showCamera.GetComponent<Camera>();
                Camera camera1 = player.GetComponent<Camera>();
                camera.fieldOfView = camera1.fieldOfView;
            }
            else
            {
                targetPosition = point2.transform.position;
                targetRotation = point2.transform.rotation;
                if (p == 0)
                {
                    MicroBlack.ToBlack = true;
                    p++;
                }

            }
            // 使用MoveTowards平滑过渡位置
            showCamera.transform.position = Vector3.MoveTowards(showCamera.transform.position, targetPosition,
                10 * Time.deltaTime * transform.lossyScale.x);

            // 使用RotateTowards平滑过渡旋转
            showCamera.transform.rotation = Quaternion.RotateTowards(showCamera.transform.rotation, targetRotation, 30 * Time.deltaTime);
            if (Vector3.Distance(showCamera.transform.position, point2.transform.position) < 0.01f)
            {
                showCamera.SetActive(false);
                lookCamera.SetActive(true);
                MicroUI.setTrue = false;
                p = 0;
                player.SetActive(false);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.T))RotateGlassOnObserving();

        // 切换粗细调节模式
        if (Input.GetKeyDown(KeyCode.M))SwitchModeOfChange();

        // 焦距减少（B键）
        if (Input.GetKey(KeyCode.B))ChangeFocal(-1);

        // 焦距增加（N键）
        if (Input.GetKey(KeyCode.N))ChangeFocal(1);
    }
    // 焦距调整方法
    private void AdjustFocal(int direction)
    {
        // 根据模式选择步长
        float step = isCoarseAdjust ? coarseStep : fineStep;

        currentFocal = depthOfField.focusDistance.value;
        float newFocal = currentFocal + direction * step;
        
        if (newFocal < 1)
        {
            newFocal = 1;
        }
        if (newFocal>300)
        {
            newFocal = 300;
        }

        distance += direction * step;

        if (isCoarseAdjust)
        {
            slider.value += direction * -0.02f;
        }
        else
        {
            slider.value += direction * -0.02f * 0.05f;
        }
        
        // 更新景深参数
        currentFocal = newFocal;

        if (depthOfField != null)
        {
            depthOfField.focusDistance.value = currentFocal;
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
            InteractWithMicroscope temp=new();
            if (player.transform.parent?.TryGetComponent<InteractWithMicroscope>(out temp)==true)
            {
                temp?.EnableInteract(this);
            }
            MicroUI.setTrue = true;
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
            InteractWithMicroscope temp=new();
            if (player.transform.parent?.TryGetComponent<InteractWithMicroscope>(out temp)==true)
            {
                temp?.DisableInteract(this);
            }
            MicroUI.setTrue = false;
            isNear = false;
        }
    }

    // 协程处理平滑过渡
    IEnumerator SwitchObjectiveLens()
    {
        isRotating = true;

        // 记录初始值和目标值
        float startSize = microscopeCamera.GetComponent<Camera>().orthographicSize;
        float targetSize = glass4Size[glass4Size.Count()-targetGlass4Choice-1] / 5;
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

    public int GetScale()
    {
        return glass4Size[glass4Choice];
    }

    public float GetDistance()
    {
        return distance;
    }
}
