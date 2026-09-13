using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using JetBrains.Annotations;
using Unity.Mathematics;
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
    public GameObject lookCameraCanvas; //观察相机Canvas
    public GameObject screen;
    public GameObject microscopeCamera;
    public GameObject glass4;
    public Slider slider;
    public List<int> glass4Rotation = new List<int>();
    int[] glass4Size = { 5, 10, 50, 100 };
    int[] aperture = { 16, 10, 6, 1 };

    float[] values = { 0.79f, 0.67f, 0.53f, 0.34f };  //slider的value对应焦距

    float[] values1 = { 0.17f, 0.13f, 0.07f, 0.04f };  //物体显示区间，显微镜可以看见物体，在区间内focal length生效

    float[] values2 = { 0.09f, 0.07f, 0.04f, 0.025f };  //透明度区间

    int glass4Choice = 0;
    GameObject Object; //玩家放的物体
    public GameObject Cam;
    GameObject player;
    private LineRenderer lineRenderer;
    public Material screenMaterial;

    public Tutorial TutorialUI; //教程UI

    LookOperation lookOperation;

    public GameObject knobChild0; // 用于显示粗调的子对象
    public GameObject knobChild1; // 用于显示细调的子对象

    public GameObject Lightmain;
    public Material mainMaterial;
    #endregion

    #region 数值和工具类变量

    public int pointer;
    public float widthChangeAmount = -0.05f; // 每次滚动改变的宽度量
    public float minWidth = 0.01f; // 最小宽度
    public float maxWidth = 0.1f; // 最大宽度
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

    #endregion

    #region 焦距调整相关变量

    private Volume volume; // 后处理Volume组件
    private DepthOfField depthOfField; // 景深效果组件
    private bool isCoarseAdjust = true; // 是否为粗调模式
    float coarseStep = 0.2f; // 粗调步长
    float fineStep = 0.02f; // 细调步长
    // float minFocal = 1f; // 最小焦距
    // float maxFocal = 50f; // 最大焦距
    float distance; //目镜和底座距离
    float currentFocal; // 当前焦距值
    int focalChangeSpeed = 1; // 速率
    float focalChangeInterval = 0.01f; // 每次调整间隔
    float lastAdjustmentTimeB = 0f;
    float lastAdjustmentTimeN = 0f;
    Vector3 ObjectInitialScale = Vector3.zero;

    #endregion

    // Start 在游戏开始时调用一次
    void Start()
    {
        if (lookCameraCanvas != null)
        {
            lookOperation = lookCameraCanvas.GetComponent<LookOperation>();
        }
        pointer = 0;
        lineRenderer = Line.GetComponent<LineRenderer>(); // 获取 LineRenderer 组件
        lineRenderer.enabled = false;
        SetLaserPositions();
        Light.SetActive(false);
        show.SetActive(false);
        Cam.SetActive(false);
        screen.SetActive(false);
        StartCoroutine(SwitchObjectiveLens());
        volume = lookCamera.GetComponent<Volume>();
        if (volume != null && volume.profile.TryGet(out depthOfField))
        {
            currentFocal = depthOfField.focusDistance.value;
        }

        MeshRenderer meshRenderer = screen.GetComponent<MeshRenderer>();
        meshRenderer.material = screenMaterial;
        mainMaterial = Lightmain.GetComponent<Renderer>().material;
        // 在启动时就找到并缓存这些引用
        if (lookCameraCanvas != null)
        {
            Transform knobTransform = lookCameraCanvas.transform.Find("Knob");
            if (knobTransform != null)
            {
                // 检查子对象数量
                if (knobTransform.childCount >= 2)
                {
                    knobChild0 = knobTransform.GetChild(0).gameObject;
                    knobChild1 = knobTransform.GetChild(1).gameObject;
                }
                else
                {
                    Debug.LogError("'Knob' 对象需要至少2个子对象！");
                }
            }
            else
            {
                Debug.LogError("在 lookCameraCanvas 下找不到 'Knob' 对象！");
            }
        }
        else
        {
            Debug.LogError("lookCameraCanvas 未被赋值！");
        }
    }
    /// <summary>
    /// 灯光开关
    /// </summary>
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
                        // 样本被重新放上载物台时，先确保对象恢复激活，再由焦距逻辑决定最终可见性。
                        Object.SetActive(true);
                        show.SetActive(true);
                        ShowObject showObject = Object.GetComponent<ShowObject>();
                        if (showObject != null)
                        {
                            showObject.microscope = this;
                        }
                        // 无论是键盘还是手柄输入路径，放置后都统一执行一次焦距可见性评估。
                        SetcurrentFocal();
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
                    player.SetActive(false);
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
                // 防止在“失焦隐藏状态”下被取下后仍然保持 inactive，造成再次放置后看不到。
                Object.SetActive(true);
                Object.transform.localScale = new
                    Vector3(ObjectInitialScale.x,
                        ObjectInitialScale.y,
                        ObjectInitialScale.z);
                Object.transform.SetParent(player.transform);
                ShowObject showObject = Object.GetComponent<ShowObject>();
                if (showObject != null)
                {
                    showObject.microscope = null;
                }
                Object = null;
                show.SetActive(false);
            }
        }
    }

    public bool HasPlacedSample()
    {
        return Object != null;
    }

    public void RotateGlass()
    {
        if (MicroUI.setTrue && isNear && !isRotating)
        {
            int maxChoice = Mathf.Min(glass4Rotation.Count, glass4Size.Length);
            targetGlass4Choice = (glass4Choice + 1) % maxChoice;
            glass4Choice = targetGlass4Choice;
            SetcurrentFocal();
            StartCoroutine(SwitchObjectiveLens());
        }
    }

    public void RotateGlassOnObserving()
    {
        if(!lookCamera.activeSelf)return;
        if (depthOfField != null)
        {
            depthOfField.focusDistance.value = 1;
        }

        int maxChoice = Mathf.Min(glass4Rotation.Count, glass4Size.Length);
        targetGlass4Choice = (glass4Choice + 1) % maxChoice;
        glass4Choice = targetGlass4Choice;
        Object.SetActive(false);

        StartCoroutine(SwitchObjectiveLens());
    }

    /// <summary>
    /// 灯光调整
    /// </summary>
    /// <param name="scrollInput"></param>
    public void AdjustLight(float scrollInput)
    {
        if ((MicroUI.setTrue && isNear) || lookCamera.activeSelf)
        {
            if (scrollInput == 0)
            {
                scrollInput = Input.GetAxis("Mouse ScrollWheel");
            }
            if (scrollInput != 0)
            {
                float newWidth = Mathf.Clamp(lineRenderer.startWidth + scrollInput * widthChangeAmount * 0.25f, 
                                             minWidth, maxWidth);
                lineRenderer.startWidth = newWidth;
                lineRenderer.endWidth = newWidth;
                SetLight();

                float rotationAmount = scrollInput * rotationChangeRatio;
                Vector3 currentRotation = knob1.transform.rotation.eulerAngles;

                float newRotationZ = currentRotation.z + rotationAmount;

                newRotationZ = Mathf.Clamp(newRotationZ, minRotationZ, maxRotationZ);

                knob1.transform.rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, newRotationZ);
            }
        }
    }

    /// <summary>
    /// 退出观察
    /// </summary>
    public void QuitObserve()
    {
        
        lookCamera.SetActive(false);
        pointer = 0;
        MicroUI.setTrue = true;
        player.SetActive(true);
        //player.GetComponent<Camera>().cullingMask = ~player.GetComponent<Camera>().cullingMask;
        Cam.SetActive(false);
        
    }
    
    /// <summary>
    /// 切换粗细调节模式
    /// </summary>
    public void SwitchModeOfChange()
    {
        if (lookCamera == null || 
        !lookCamera.activeSelf ||
         knobChild0 == null || 
         knobChild1 == null)
        {
            return;
        }
        isCoarseAdjust = !isCoarseAdjust;
    }
    public void ChangeFocal(float mode)
    {
        if(!lookCamera.activeSelf) return;
        float knobRotationSpeed = 20f;
        float rotationAngle = knobRotationSpeed * Time.deltaTime;
        if (mode < 0)
        {
            if (Time.time - lastAdjustmentTimeB >= focalChangeInterval)
            {
                AdjustFocal(-focalChangeSpeed);
                lastAdjustmentTimeB = Time.time;
            }
            
            if(isCoarseAdjust)
            {
                knobChild0.transform.Rotate(Vector3.forward, -rotationAngle);
            }
            else
            {
                knobChild1.transform.Rotate(Vector3.forward, -rotationAngle);
            }
        }
        else if(mode > 0)
        {
            if (Time.time - lastAdjustmentTimeN >= focalChangeInterval)
            {
                AdjustFocal(focalChangeSpeed);
                lastAdjustmentTimeN = Time.time;
            }
            if(isCoarseAdjust)
            {
                knobChild0.transform.Rotate(Vector3.forward, rotationAngle);
            }
            else
            {
                knobChild1.transform.Rotate(Vector3.forward, rotationAngle);
            }
        }
    }
    
    void Update()
    {
        
        AdjustLight(0);
       
        if (showCamera.activeSelf)
        {
            // ========== 注释掉原有协程平滑移动代码 ==========
            // // 目标位置和旋转
            // Vector3 targetPosition;
            // Quaternion targetRotation;

            // // 根据pointer的值决定目标位置和旋转
            // if (pointer == 1)
            // {
            //     targetPosition = point1.transform.position;
            //     targetRotation = point1.transform.rotation;
            //     Camera camera = showCamera.GetComponent<Camera>();
            //     Camera camera1 = player.GetComponent<Camera>();
            //     camera.fieldOfView = camera1.fieldOfView;
            // }
            // else
            // {
            //     targetPosition = point2.transform.position;
            //     targetRotation = point2.transform.rotation;
            //     if (p == 0)
            //     {
            //         MicroBlack.ToBlack = true;
            //         p++;
            //     }

            // }
            // // 使用MoveTowards平滑过渡位置
            // showCamera.transform.position = Vector3.MoveTowards(showCamera.transform.position, targetPosition,
            //     10 * Time.deltaTime * transform.lossyScale.x);

            // // 使用RotateTowards平滑过渡旋转
            // showCamera.transform.rotation = Quaternion.RotateTowards(showCamera.transform.rotation, targetRotation, 30 * Time.deltaTime);
            // if (Vector3.Distance(showCamera.transform.position, point2.transform.position) < 0.01f)
            // ==============================================

            // ========== 新增瞬间移动代码 ==========
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
                
                // 瞬间移动到point1位置
                showCamera.transform.position = targetPosition;
                showCamera.transform.rotation = targetRotation;
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

                // 瞬间移动到point2位置
                showCamera.transform.position = targetPosition;
                showCamera.transform.rotation = targetRotation;
            }

            // 瞬间判断是否到达目标位置（直接触发后续逻辑）
            if (pointer != 1) // 只有指向point2时才切换到观察相机
            // ==============================================
            {
                showCamera.SetActive(false);
                lookCamera.SetActive(true);
                MicroUI.setTrue = false;
                p = 0;
                Interactor.Instance.ChangeState(Interactor.GameState.Observing);
                
                //取反 只渲染Microscope的ui
                //player.GetComponent<Camera>().cullingMask = ~player.GetComponent<Camera>().cullingMask;
            }
        }
        
    }

    public bool AssistantCanInteract => MicroUI.setTrue && isNear;
    public bool AssistantObserving => lookCamera != null && lookCamera.activeInHierarchy;
    public bool AssistantObjectiveMoving => isRotating;
    public bool AssistantHasHandObject
    {
        get
        {
            if (player == null) return false;
            foreach (Transform child in player.transform) if (child.CompareTag("ObserveObjects")) return true;
            return false;
        }
    }
    // 焦距调整方法
    private void AdjustFocal(int direction)
    {
        // 根据模式选择步长
        float step = isCoarseAdjust ? coarseStep : fineStep;

        currentFocal = depthOfField.focalLength.value;
        distance += direction * step;

        if (isCoarseAdjust)
        {
            slider.value += direction * -0.1f*focalChangeInterval*10;
        }
        else
        {
            slider.value += direction * -0.04f * 0.1f*focalChangeInterval*10;
        }

        // Debug.Log("slider.value"+slider.value);
        // Debug.Log("glass4Choice"+glass4Choice);
        // Debug.Log("values[glass4Choice]"+values[glass4Choice]);
        // Debug.Log("values1[glass4Choice]"+values1[glass4Choice]);
        //调整物体是否显示
        SetcurrentFocal();

        if (depthOfField != null)
        {
            depthOfField.focalLength.value = currentFocal;
        }
    }

    void SetcurrentFocal()
    {
        if (slider.value > values[glass4Choice] + values1[glass4Choice] ||
            slider.value < values[glass4Choice] - values1[glass4Choice])
        {
            Object?.SetActive(false);
        }
        else
        {
            Object.SetActive(true);
            ShowObject showObject = Object.GetComponent<ShowObject>();

            showObject.SetColor(SetTarget());

            //调整清晰
            currentFocal = math.abs(values[glass4Choice] - slider.value) / values1[glass4Choice] * 300;
        }
    }

    void SetLight()
    {
        if (Object == null) return;
        ShowObject showObject = Object.GetComponent<ShowObject>();
        showObject.SetLight(GetLight());
    }

    //设置透明度
    float SetTarget()
    {
        if (math.abs(values[glass4Choice] - slider.value) < values2[glass4Choice])
        {
            return 1;
        }
        else
        {
            return 1 - (math.abs(slider.value - math.abs(values[glass4Choice])) - values2[glass4Choice]) /
            (values1[glass4Choice] - values2[glass4Choice]);
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
        if (other.CompareTag("MainCamera"))
        {
            player = other.gameObject;
            if (player.transform.parent != null && player.transform.parent.TryGetComponent<Interactor>(out Interactor temp))
            {
                temp.OnMicroscopeIn(this);
            }
            MicroUI.setTrue = true;
            SetLighting();
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            player = other.gameObject;
            isNear = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera"))
        {
            if (player.transform.parent != null && player.transform.parent.TryGetComponent<Interactor>(out Interactor temp))
            {
                temp.OnMicroscopeOut(this);
            }
            MicroUI.setTrue = false;
            isNear = false;
            SetNormal();
        }
    }

    // 协程处理平滑过渡
    IEnumerator SwitchObjectiveLens()
    {
        isRotating = true;

        // 记录初始值和目标值
        float startSize = microscopeCamera.GetComponent<Camera>().orthographicSize;
        float targetSize = glass4Size[glass4Size.Count() - targetGlass4Choice - 1] / 5;
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
        if (Object != null) Object.SetActive(true);
        SetcurrentFocal();
    }

    public int GetScale()
    {
        return glass4Size[glass4Choice];
    }

    public float GetDistance()
    {
        return distance;
    }

    public float GetLight()
    {
        return 0.8f*(lineRenderer.endWidth-0.01f)/0.01f+0.1f;
    }

    #region 高亮设置
    public void SetBlink()
    {
        Lightmain.SetActive(true);
        mainMaterial?.SetFloat("_boolean", 1f);
    }

    public void SetNormal()
    {
        Lightmain.SetActive(false);
    }

    public void SetLighting()
    {
        Lightmain.SetActive(true);
        mainMaterial.SetFloat("_boolean", 0f);
    }
    #endregion
}
