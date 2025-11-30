using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[System.Serializable]
public class TutorialNode
{
    public string title;                // 节点标题
    public string content;              // 节点描述
    public VideoPlayer videoPlayer;     // 该节点使用的 VideoPlayer 组件
    public RenderTexture renderTexture; // 该 VideoPlayer 输出的 RenderTexture
}

[System.Serializable]
public class TutorialData
{
    public string tutorialName;   // 教程名称
    public List<TutorialNode> nodes; // 该教程的所有节点
}

public class Tutorial : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject firstLevelUI;          // 一级UI
    public GameObject secondLevelUITemplate; // 二级UI模板
    public Text titleText;                   // 标题文本
    public Text contentText;                 // 内容文本
    public RawImage videoDisplay;            // 用于显示视频的 RawImage

    [Header("教程数据")]
    public List<TutorialData> tutorialDatas; // 所有教程数据

    [Header("首次启动设置")]
    public int firstLaunchTutorialIndex = 0; // 首次启动显示的教程索引

    private int currentTutorialIndex = -1;   // 当前教程索引
    private int currentNodeIndex = 0;        // 当前节点索引
    private VideoPlayer currentVideoPlayer;  // 当前正在播放的 VideoPlayer
    public bool player;

    private void Awake()
    {
        // 初始化UI状态
        if (firstLevelUI != null) firstLevelUI.SetActive(false);
        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(false);

        // 检查首次启动
        if (CheckFirstLaunch("Tutorial_FirstLaunch"))
        {
            ShowTutorial(firstLaunchTutorialIndex);
        }
    }

    void Update()
    {
        if (player)
        {
            if(Input.GetKeyDown(KeyCode.O))
            {
                ReturnToFirstLevel();
            }
        }
    }
    /// <summary>
    /// 检查是否为首次启动
    /// </summary>
    public bool CheckFirstLaunch(string key)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    /// <summary>
    /// 显示指定索引的教程
    /// </summary>
    public void ShowTutorial(int tutorialIndex)
    {
        if (tutorialIndex < 0 || tutorialIndex >= tutorialDatas.Count)
        {
            Debug.LogWarning("教程索引超出范围！");
            return;
        }

        // 更新当前教程索引并重置节点索引
        currentTutorialIndex = tutorialIndex;
        currentNodeIndex = 0;

        // 隐藏一级UI，显示二级UI模板
        if (firstLevelUI != null) firstLevelUI.SetActive(false);
        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(true);

        // 加载并显示当前节点内容
        LoadCurrentNodeContent();
    }

    /// <summary>
    /// 加载当前节点的文本和视频内容
    /// </summary>
    private void LoadCurrentNodeContent()
    {
        if (currentTutorialIndex == -1) return;

        TutorialData currentTutorial = tutorialDatas[currentTutorialIndex];
        TutorialNode currentNode = currentTutorial.nodes[currentNodeIndex];

        // 更新文本
        if (titleText != null) titleText.text = currentNode.title;
        if (contentText != null) contentText.text = currentNode.content;

        // 更新视频
        UpdateVideoPlayer(currentNode);
    }

    /// <summary>
    /// 切换并配置当前节点的 VideoPlayer
    /// </summary>
    private void UpdateVideoPlayer(TutorialNode node)
    {
        // 停止上一个视频
        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
            currentVideoPlayer.targetTexture = null; // 解除上一个 RenderTexture 的绑定
        }

        // 如果当前节点没有指定 VideoPlayer，则清空显示
        if (node.videoPlayer == null || node.renderTexture == null)
        {
            if (videoDisplay != null)
            {
                videoDisplay.texture = null;
            }
            currentVideoPlayer = null;
            return;
        }

        // 配置新的 VideoPlayer
        currentVideoPlayer = node.videoPlayer;
        currentVideoPlayer.targetTexture = node.renderTexture;

        // 将 RawImage 的显示目标设置为当前节点的 RenderTexture
        if (videoDisplay != null)
        {
            videoDisplay.texture = node.renderTexture;
        }

        // 准备并播放视频
        currentVideoPlayer.Prepare();
        // 等待一帧确保 Prepare 完成，然后播放
        StartCoroutine(PlayVideoAfterPrepare(currentVideoPlayer));
    }
    
    /// <summary>
    /// 协程：等待 VideoPlayer.Prepare() 完成后再播放
    /// </summary>
    private IEnumerator PlayVideoAfterPrepare(VideoPlayer vp)
    {
        while (!vp.isPrepared)
        {
            yield return null;
        }
        vp.Play();
    }

    /// <summary>
    /// 前往下一个节点
    /// </summary>
    public void GoToNextNode()
    {
        if (currentTutorialIndex == -1) return;

        TutorialData currentTutorial = tutorialDatas[currentTutorialIndex];

        // 检查是否有下一个节点
        if (currentNodeIndex < currentTutorial.nodes.Count - 1)
        {
            currentNodeIndex++;
            LoadCurrentNodeContent();
        }
        else
        {
            // 没有下一个节点时返回一级UI
            Back();
        }
    }

    /// <summary>
    /// 前往上一个节点
    /// </summary>
    public void GoToPreviousNode()
    {
        if (currentTutorialIndex == -1) return;

        // 检查是否有上一个节点
        if (currentNodeIndex > 0)
        {
            currentNodeIndex--;
            LoadCurrentNodeContent();
        }
    }

    /// <summary>
    /// 返回一级UI
    /// </summary>
    public void ReturnToFirstLevel()
    {
        // 停止当前视频
        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
            currentVideoPlayer.targetTexture = null;
        }
        currentVideoPlayer = null;

        // 隐藏二级UI模板，显示一级UI
        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(false);
        if (firstLevelUI != null) firstLevelUI.SetActive(true);

        // 清空 RawImage
        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
        }

        // 重置当前教程索引
        currentTutorialIndex = -1;
        currentNodeIndex = 0;
    }

    /// <summary>
    /// 返回
    /// </summary>
    public void Back()
    {
        // 停止当前视频
        if (currentVideoPlayer != null)
        {
            currentVideoPlayer.Stop();
            currentVideoPlayer.targetTexture = null;
        }
        currentVideoPlayer = null;

        // 隐藏二级UI模板
        if (secondLevelUITemplate != null) secondLevelUITemplate.SetActive(false);
        // 隐藏一级UI
        if (firstLevelUI != null) firstLevelUI.SetActive(false);
        // 清空 RawImage
        if (videoDisplay != null)
        {
            videoDisplay.texture = null;
        }

        // 重置当前教程索引
        currentTutorialIndex = -1;
        currentNodeIndex = 0;
    }

    /// <summary>
    /// 从一级UI按钮调用，显示指定教程
    /// </summary>
    public void OnTutorialButtonClick(int tutorialIndex)
    {
        ShowTutorial(tutorialIndex);
    }

    /// <summary>
    /// 重置所有教程进度
    /// </summary>
    public void ResetAllTutorials()
    {
        PlayerPrefs.DeleteKey("Tutorial_FirstLaunch");
        PlayerPrefs.Save();
        Debug.Log("所有教程进度已重置！");
    }
}