using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    private float updateInterval = 0.5F;
    private Text FPSText;
    private float accum = 0;
    private int frames = 0;
    private float timeleft;

    void Start()
    {
        FPSText = GetComponent<Text>();
        FPSText.material = Instantiate(FPSText.material);
        timeleft = updateInterval;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        if (timeleft <= 0.0)
        {
            float fps = accum / frames;
            string format = System.String.Format("{1:F2} ms {0:F2} FPS", fps, 1f / fps * 1000f);
            FPSText.text = format;

            if (fps < 30)
                FPSText.material.color = Color.yellow;
            else
            if (fps < 10)
                FPSText.material.color = Color.red;
            else
                FPSText.material.color = Color.green;

            timeleft = updateInterval;
            accum = 0.0F;
            frames = 0;
        }
    }
}
