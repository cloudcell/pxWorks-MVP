using System;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    const float fpsMeasurePeriod = 0.5f;
    private int m_FpsAccumulator = 0;
    private float m_FpsNextPeriod = 0;
    private int m_CurrentFps;
    const string display = "{0} FPS";
    const string displayAdvanced = "{0} FPS,  {1} FPS (fixed)";

    public bool Advanced = false;
    public bool ShowButtom = false;
    public int FontSize = 100;

    private int m_FpsAccumulatorFixed = 0;
    private int m_CurrentFpsFixed = 0;


    private void Start()
    {
        m_FpsNextPeriod = Time.unscaledTime + fpsMeasurePeriod;
    }

    private void Update()
    {
        // measure average frames per second
        m_FpsAccumulator++;
        if (Time.unscaledTime > m_FpsNextPeriod)
        {
            m_CurrentFps = (int)(m_FpsAccumulator / fpsMeasurePeriod);
            m_CurrentFpsFixed = (int)(m_FpsAccumulatorFixed / fpsMeasurePeriod);
            m_FpsAccumulator = 0;
            m_FpsAccumulatorFixed = 0;
            m_FpsNextPeriod += fpsMeasurePeriod;
        }
    }

    private void FixedUpdate()
    {
        m_FpsAccumulatorFixed++;
    }

    private void OnGUI()
    {
        var x = Screen.width / 2 - 60;
        GUI.contentColor = Color.white;
        GUI.skin.label.fontSize =  FontSize * Mathf.Max(Screen.height, Screen.width) / 50 / 100;
        var y = ShowButtom ? Screen.height - GUI.skin.label.fontSize - 10 : 0;
        GUI.Label(new Rect(x, y, 1000, GUI.skin.label.fontSize + 10), string.Format(Advanced ? displayAdvanced : display, m_CurrentFps, m_CurrentFpsFixed));
    }
}