using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using UnityEngine.XR.Management;

// [新增] 用于JSON序列化的数据包装类
[System.Serializable]
public class TutorialProgressData
{
    public List<string> triggeredTutorialKeys = new List<string>();
}

[System.Serializable]
public class TutorialNode
{
    public string title;                
    public string content;              
    public VideoClip videoClip;
}

[System.Serializable]
public class TutorialData
{
    public string tutorialName;   
    public List<TutorialNode> nodes; 
}

public class Tutorial : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject firstLevelUI;          
    public GameObject secondLevelUITemplate; 
    public TextMeshProUGUI titleText;                   // 标题文本
    public TextMeshProUGUI contentText;                 // 内容文本              
    public RawImage videoDisplay;            
    [Header("视频播放器")]
    public VideoPlayer videoPlayer; // 直接引用挂在RawImage上的VideoPlayer组件

    [Header("教程数据")]
    public List<TutorialData> tutorialDatas; 

    [Header("首次启动设置")]
    public int firstLaunchTutorialIndex = 0; 

    public Microscope microscope;  //显微镜脚本

    private int currentTutorialIndex = -1;   
    private int currentNodeIndex = 0;         
    public bool player;

    public int level; // 当前级别
    public List<TextMeshProUGUI> page; // 页码

    // [新增] 存档数据与路径
    private TutorialProgressData progressData;
    private string saveFilePath;

    public TutorialButton tutorialButton;

    private void Awake()
    {
        // 【新增】优先查找TutorialButton组件
        if (tutorialButton == null)
        {
            tutorialButton = FindObjectOfType<TutorialButton>();
            if (tutorialButton == null)
            {
                Debug.LogError("未找到TutorialButton组件，请确保场景中有该脚本！");
            }
        }

        // 初始化VideoPlayer引用（如果未手动赋值，自动从RawImage上查找）
        if (videoPlayer == null && videoDisplay != null)
        {
            videoPlayer = videoDisplay.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError("RawImage上未找到VideoPlayer组件，请先挂载！");
            }
            else
            {
                // 初始化VideoPlayer基础设置
                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = true; // 视频循环播放，可根据需求修改
                videoPlayer.source = VideoSource.VideoClip;
                // 关键：确保VideoPlayer的渲染模式正确（自动关联RawImage）
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            }
        }

        // [新增] 初始化路径并加载数据
        saveFilePath = Path.Combine(Application.persistentDataPath, "TutorialProgress.json");
        LoadProgress();

        // 初始化UI状态
        if (firstLevelUI != null) firstLevelUI.SetActive(false);
        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(false);
        
        Interactor.Instance.currentTutorial = this;
        Interactor.Instance.tutorialButtonInput = GetComponent<TutorialButtonInput>();
    }

    private IEnumerator Start()
    {
        Interactor.Instance.currentTutorial = this;
        Interactor.Instance.tutorialButtonInput = GetComponent<TutorialButtonInput>();
        // 检查首次启动
        if (!(CheckFirstLaunch("Tutorial_FirstLaunch") && player))
        {
            yield break;
        }

        yield return new WaitForSeconds(2f);
        ShowTutorial(firstLaunchTutorialIndex);
        microscope.SetBlink();
        
        Interactor.Instance.ChangeState(Interactor.GameState.Tutorial);
    }

    private void OnEnable()
    {
        Interactor.Instance.currentTutorial = this;
        Interactor.Instance.tutorialButtonInput = GetComponent<TutorialButtonInput>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.O))
        {
            ReturnToFirstLevel();
        }
    }

    /// <summary>
    /// [新增] 加载进度数据
    /// </summary>
    private void LoadProgress()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                progressData = JsonUtility.FromJson<TutorialProgressData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError("教程数据加载失败，重置为新数据: " + e.Message);
                progressData = new TutorialProgressData();
            }
        }
        else
        {
            progressData = new TutorialProgressData();
        }
    }

    /// <summary>
    /// [新增] 保存进度数据
    /// </summary>
    private void SaveProgress()
    {
        string json = JsonUtility.ToJson(progressData, true);
        File.WriteAllText(saveFilePath, json);
    }

    /// <summary>
    /// [修改] 检查是否为首次启动
    /// </summary>
    public bool CheckFirstLaunch(string key)
    {
        if (progressData?.triggeredTutorialKeys==null)
        {
            LoadProgress();
        }
        if (!progressData.triggeredTutorialKeys.Contains(key))
        {
            progressData.triggeredTutorialKeys.Add(key);
            SaveProgress();
            return true;
        }
        return false;
    }

    /// <summary>
    /// [修改] 重置所有教程进度
    /// </summary>
    public void ResetAllTutorials()
    {
        progressData.triggeredTutorialKeys.Clear();
        
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
        
        progressData = new TutorialProgressData();
        
        Debug.Log("所有教程进度已重置（JSON文件已删除）！");
    }

    public void ShowTutorial(int tutorialIndex)
    {
        if (tutorialIndex < 0 || tutorialIndex >= tutorialDatas.Count)
        {
            Debug.LogWarning("教程索引超出范围！");
            return;
        }

        currentTutorialIndex = tutorialIndex;
        currentNodeIndex = 0;
        level=2;
        ChangeLevel();
        if (firstLevelUI != null) firstLevelUI.SetActive(false);
        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(true);

        LoadCurrentNodeContent();
    
        Interactor.Instance.ChangeState(Interactor.GameState.Tutorial);
    }

    private void LoadCurrentNodeContent()
    {
        if (currentTutorialIndex == -1) return;

        TutorialData currentTutorial = tutorialDatas[currentTutorialIndex];
        TutorialNode currentNode = currentTutorial.nodes[currentNodeIndex];

        if (titleText != null) titleText.text = currentNode.title;
        if (contentText != null) contentText.text = currentNode.content;

        UpdateVideoPlayer(currentNode);
        // 更新页码显示
        SetPage(currentNodeIndex);
    }

    /// <summary>
    /// 最终版视频播放逻辑（完全依赖VideoPlayer自动关联RawImage）
    /// </summary>
    /// <param name="node">当前教程节点</param>
    private void UpdateVideoPlayer(TutorialNode node)
    {
        // 检查VideoPlayer是否有效
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer组件未初始化，无法播放视频！");
            return;
        }

        // 1. 停止当前播放的视频
        videoPlayer.Stop();

        // 2. 如果当前节点没有视频，仅停止播放即可（无需修改texture）
        if (node.videoClip == null)
        {
            return;
        }

        // 3. 设置新的视频剪辑
        videoPlayer.clip = node.videoClip;

        // 4. 准备并播放视频
        StartCoroutine(PlayVideoAfterPrepare(videoPlayer));
    }
    
    private IEnumerator PlayVideoAfterPrepare(VideoPlayer vp)
    {
        if (vp == null || vp.clip == null) yield break;
        
        // 准备视频
        vp.Prepare();
        
        // 等待视频准备完成
        while (!vp.isPrepared)
        {
            yield return null;
        }
        
        // 准备完成后播放
        vp.Play();
        Debug.Log($"视频开始播放: {vp.clip.name}");
    }

    public void GoToNextNode(int level)
    {
        if(level==2)
        {
            if (currentTutorialIndex == -1) return;

            TutorialData currentTutorial = tutorialDatas[currentTutorialIndex];

            if (currentNodeIndex < currentTutorial.nodes.Count - 1)
            {
                currentNodeIndex++;
                LoadCurrentNodeContent();
            }
            else
            {
                Back();
            }
        }
    }

    public void GoToPreviousNode(int level)
    {
        if(level==2)
        {
            if (currentTutorialIndex == -1) return;

            if (currentNodeIndex > 0)
            {
                currentNodeIndex--;
                LoadCurrentNodeContent();
            }
            SetPage(currentNodeIndex);
        }
    }

    public void ReturnToFirstLevel()
    {
        if(!player)return;

        // 停止视频播放
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(false);
        if (firstLevelUI != null) firstLevelUI.SetActive(true);
        
        level=1;
        ChangeLevel();
        currentTutorialIndex = -1;
        currentNodeIndex = 0;
    }

    public void Back()
    {
        // 停止视频播放
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(false);
        if (firstLevelUI != null) firstLevelUI.SetActive(false);

        currentTutorialIndex = -1;
        currentNodeIndex = 0;
        level=-1;
        ChangeLevel();
        Interactor.Instance.ChangeState(null);
    }

    public void OnTutorialButtonClick(int tutorialIndex)
    {
        ShowTutorial(tutorialIndex);
    }

    /// <summary>
    /// 设置页码显示
    /// </summary>
    public void SetPage(int pageIndex)
    {
        if(level<0 || level>=page.Count)
        {
            return;
        }
        page[level].text="Page "+(pageIndex+1).ToString();
    }

    /// <summary>
    /// 通知TutorialButton切换级别
    /// </summary>
    public void ChangeLevel()
    {
        tutorialButton.ChangeLevel(level);
    }
}