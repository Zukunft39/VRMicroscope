using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Microscope : MonoBehaviour
{
    public GameObject Line;
    public GameObject Light;
    public GameObject show;
    public GameObject point1;
    public GameObject point2;

    private LineRenderer lineRenderer;

    GameObject Object;
    public GameObject Cam;
    public int pointer;
    GameObject player;

    public GameObject showCamera;
    public GameObject lookCamera;
    public GameObject screen;

    int p = 0;
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
        if (MicroUI.setTrue && Input.GetKeyDown(KeyCode.Q))
        {
            lineRenderer.enabled = !lineRenderer.enabled;
            Light.SetActive(!Light.activeSelf);
            Cam.SetActive(!Cam.activeSelf);
            screen.SetActive(!screen.activeSelf);
        }
        if (MicroUI.setTrue && Input.GetKeyDown(KeyCode.E))
        {
            if (Object==null)
            {
                if (player.transform.childCount>0)
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
                if (pointer==0)
                {
                    showCamera.SetActive(true);
                    showCamera.transform.position=player.transform.position;
                    showCamera.transform.rotation=player.transform.rotation;
                    pointer++;
                }
                else
                {
                    pointer++;
                }
            }
        }
        if (MicroUI.setTrue && Input.GetKeyDown(KeyCode.R))
        {
            if (Object != null)
            {
                Object.transform.SetParent(player.transform);
                Object = null; 
                show.SetActive(false);
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
                MicroUI.setTrue = false;
                p = 0;
            }
        }

        if(lookCamera.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                lookCamera.SetActive(false);
                pointer = 0;
                MicroUI.setTrue = true;
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

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MicroUI.setTrue = false;
        }
    }
}
