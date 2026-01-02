using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    // 在编辑器面板中填入视频文件名，例如 "DemoVideo.mp4"
    public string videoFileName = "DemoVideo.mp4";
    
    // 引用同一物体上的组件
    private VideoPlayer videoPlayer;
    private RawImage rawImage;

    void Awake()
    {
        // 获取组件
        videoPlayer = GetComponent<VideoPlayer>();
        rawImage = GetComponent<RawImage>();
    }

    void Start()
    {
        // 1. 构建路径
        string videoPath = Path.Combine(Application.streamingAssetsPath, videoFileName);

        // Debug: 在安卓真机上可以通过 Logcat 查看这个路径是否正确
        Debug.Log("Unity视频路径: " + videoPath);

        // 2. 设置 VideoPlayer
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoPath;

        // 3. 准备并播放
        // 准备完成后，将纹理赋值给 RawImage，防止一开始显示白色方块
        videoPlayer.prepareCompleted += (vp) => {
            rawImage.texture = vp.texture;
            vp.Play();
        };

        videoPlayer.Prepare();
    }
}