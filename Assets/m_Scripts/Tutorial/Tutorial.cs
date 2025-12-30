using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using UnityEngine.XR.Management; // 引入 IO 命名空间用于文件操作

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
    public VideoPlayer videoPlayer;     
    public RenderTexture renderTexture; 
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

    [Header("教程数据")]
    public List<TutorialData> tutorialDatas; 

    [Header("首次启动设置")]
    public int firstLaunchTutorialIndex = 0; 

    public Microscope microscope;  //显微镜脚本

    private int currentTutorialIndex = -1;   
    private int currentNodeIndex = 0;        
    private VideoPlayer currentVideoPlayer;  
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
        // 逻辑保持不变，但 CheckFirstLaunch 内部实现已变
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

    // ----------------------------------------------------------------
    // [修改] JSON 持久化核心逻辑区域
    // ----------------------------------------------------------------

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
        string json = JsonUtility.ToJson(progressData, true); // true 表示格式化输出，方便调试查看
        File.WriteAllText(saveFilePath, json);
    }

    /// <summary>
    /// [修改] 检查是否为首次启动 (替换了 PlayerPrefs)
    /// </summary>
    public bool CheckFirstLaunch(string key)
    {
        if (progressData?.triggeredTutorialKeys==null)
        {
            LoadProgress();
        }
        // 如果列表中不包含这个Key，说明是第一次
        if (!progressData.triggeredTutorialKeys.Contains(key))
        {
            // 标记为已触发
            progressData.triggeredTutorialKeys.Add(key);
            // 保存到 JSON 文件
            SaveProgress();
            return true;
        }
        return false;
    }

    /// <summary>
    /// [修改] 重置所有教程进度 (替换了 PlayerPrefs)
    /// </summary>
    public void ResetAllTutorials()
    {
        // 清空列表
        progressData.triggeredTutorialKeys.Clear();
        
        // 删除文件 或 保存空列表 (这里选择删除文件更彻底)
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
        
        // 重新初始化内存中的数据
        progressData = new TutorialProgressData();
        
        Debug.Log("所有教程进度已重置（JSON文件已删除）！");
    }

    // ----------------------------------------------------------------
    // 下方逻辑未改动，保持原样
    // ----------------------------------------------------------------

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
    }

    private void UpdateVideoPlayer(TutorialNode node)
    {
        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
            currentVideoPlayer.targetTexture = null; 
        }

        if (node.videoPlayer == null || node.renderTexture == null)
        {
            if (videoDisplay != null)
            {
                videoDisplay.texture = null;
            }
            currentVideoPlayer = null;
            return;
        }

        currentVideoPlayer = node.videoPlayer;
        currentVideoPlayer.targetTexture = node.renderTexture;

        if (videoDisplay != null)
        {
            videoDisplay.texture = node.renderTexture;
        }

        currentVideoPlayer.Prepare();
        StartCoroutine(PlayVideoAfterPrepare(currentVideoPlayer));
    }
    
    private IEnumerator PlayVideoAfterPrepare(VideoPlayer vp)
    {
        while (!vp.isPrepared)
        {
            yield return null;
        }
        vp.Play();
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
        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
            currentVideoPlayer.targetTexture = null;
        }
        currentVideoPlayer = null;

        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(false);
        if (firstLevelUI != null) firstLevelUI.SetActive(true);

        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
        }
        level=1;
        ChangeLevel();
        currentTutorialIndex = -1;
        currentNodeIndex = 0;
    }

    public void Back()
    {
        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
            currentVideoPlayer.targetTexture = null;
        }
        currentVideoPlayer = null;

        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(false);
        if (firstLevelUI != null) firstLevelUI.SetActive(false);
        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
        }

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