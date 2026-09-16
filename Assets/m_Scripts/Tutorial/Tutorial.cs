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
    [Header("UI References")]
    public GameObject firstLevelUI;          
    public GameObject secondLevelUITemplate; 
    public TextMeshProUGUI titleText;                   // 标题文本
    public TextMeshProUGUI contentText;                 // 内容文本              
    public RawImage videoDisplay;            
    [Header("Video Player")]
    public VideoPlayer videoPlayer; // 直接引用挂在RawImage上的VideoPlayer组件

    [Header("Tutorial Data")]
    public List<TutorialData> tutorialDatas; 

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
                Debug.LogError("TutorialButton not found. Ensure it exists in the scene.");
            }
        }

        // 初始化VideoPlayer引用（如果未手动赋值，自动从RawImage上查找）
        if (videoPlayer == null && videoDisplay != null)
        {
            videoPlayer = videoDisplay.GetComponent<VideoPlayer>();
            if (videoPlayer == null)
            {
                Debug.LogError("Add a VideoPlayer component to the RawImage first.");
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

    private void Start()
    {
        Interactor.Instance.currentTutorial = this;
        Interactor.Instance.tutorialButtonInput = GetComponent<TutorialButtonInput>();
    }

    private void OnEnable()
    {
        Interactor.Instance.currentTutorial = this;
        Interactor.Instance.tutorialButtonInput = GetComponent<TutorialButtonInput>();
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
                Debug.LogError("Tutorial data could not load; resetting: " + e.Message);
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
    /// [新增] 仅检查是否已完成该教程（不写入存档），用于强制性教程的条件判断
    /// </summary>
    public bool IsTutorialCompleted(string key)
    {
        if (progressData?.triggeredTutorialKeys == null) LoadProgress();
        return progressData.triggeredTutorialKeys.Contains(key);
    }

    /// <summary>
    /// [新增] 标记该教程为已完成并保存存档，在玩家完成强制任务后调用
    /// </summary>
    public void CompleteTutorialProgress(string key)
    {
        if (progressData?.triggeredTutorialKeys == null) LoadProgress();
        if (!progressData.triggeredTutorialKeys.Contains(key))
        {
            progressData.triggeredTutorialKeys.Add(key);
            SaveProgress();
            Debug.Log($"Mandatory tutorial [{key}] completed and saved.");
        }
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
        
        Debug.Log("All tutorial progress reset (JSON file deleted).");
    }

    public void ShowTutorial(int tutorialIndex)
    {
        if (tutorialIndex < 0 || tutorialIndex >= tutorialDatas.Count)
        {
            Debug.LogWarning("Tutorial index out of range.");
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
            Debug.LogError("VideoPlayer is not initialized; video cannot play.");
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
        Debug.Log($"Video playback started: {vp.clip.name}");
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
