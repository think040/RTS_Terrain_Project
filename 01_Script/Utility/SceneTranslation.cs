using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTranslation : MonoBehaviour
{
    public int targetFPS = 60;
    public int vSyncCount = 1;

    public Button[] btToScene;
    List<IdxInfo> idx;

   
    void Awake()
    {
        QualitySettings.vSyncCount = vSyncCount;
        Application.targetFrameRate = targetFPS;

        fpsTimer = new Stopwatch();
        fpsTimer.Start();

    }

    // Start is called before the first frame update
    void Start()
    {
        {
            idx = new List<IdxInfo>();

            if (btToScene != null)
            {
                for (int i = 0; i < btToScene.Length; i++)
                {
                    if (btToScene[i] != null)
                    {
                        IdxInfo info = new IdxInfo();
                        info.idx = i;
                        idx.Add(info);

                        btToScene[i].onClick.AddListener(
                            () =>
                            {
                                int idx = info.idx;
                                SceneManager.LoadScene(idx);
                            });
                    }
                }
            }
        }
    }

    class IdxInfo
    {
        public int idx;
    }


    // Update is called once per frame
    void Update()
    {
        ShowFPS();
    }

    static Stopwatch fpsTimer;
    static int fpsCounter;
    public Text fpsInfo;

    public void ShowFPS()
    {
        fpsCounter++;
        if (fpsTimer.ElapsedMilliseconds > 1000)
        {
            //float fps = (float)fpsCounter;
            float fps = (float)1000.0 * fpsCounter / fpsTimer.ElapsedMilliseconds;
            float timePerFrame = (float)fpsTimer.ElapsedMilliseconds / fpsCounter;
            fpsInfo.text = string.Format("(FPS : {0:F2}), (msPF : {1:F2} (ms))", fps, timePerFrame);

            fpsTimer.Stop();
            fpsTimer.Reset();
            fpsTimer.Start();
            fpsCounter = 0;
        }
    }
}