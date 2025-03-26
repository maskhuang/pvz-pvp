using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace PvZ.Timer
{
    /// <summary>
    /// 计时器组件：实现正计时功能 (单例模式)
    /// </summary>
    public class Timer : MonoBehaviour
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        public static Timer Instance { get; private set; }
        
        [Header("计时器设置")]
        [Tooltip("是否在Start时自动开始计时")]
        public bool autoStart = false;
        
        [Tooltip("显示时间的TextMeshPro组件，如果有的话")]
        public TMP_Text timerDisplay;
        
        [Tooltip("时间显示格式 (HH:MM:SS 或 MM:SS)")]
        public TimeFormat timeFormat = TimeFormat.MMSS;
        
        // 计时器当前是否正在运行
        private bool isRunning = false;
        
        // 已经运行的时间（秒）
        private float elapsedTime = 0f;
        
        /// <summary>
        /// 时间显示格式枚举
        /// </summary>
        public enum TimeFormat
        {
            MMSS,   // 分:秒
            HHMMSS  // 时:分:秒
        }
        
        /// <summary>
        /// 初始化单例
        /// </summary>
        void Awake()
        {
            // 保证场景中只有一个Timer单例
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 可选：场景切换时保留
            }
            else if (Instance != this)
            {
                // 如果已经存在实例，销毁当前对象
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            if (autoStart)
            {
                StartTimer();
            }
            else
            {
                UpdateTimerDisplay();
            }
        }
        
        void Update()
        {
            if (isRunning)
            {
                elapsedTime += Time.deltaTime;
                UpdateTimerDisplay();
            }
        }
        
        /// <summary>
        /// 开始计时
        /// </summary>
        public void StartTimer()
        {
            isRunning = true;
        }
        
        /// <summary>
        /// 暂停计时
        /// </summary>
        public void PauseTimer()
        {
            isRunning = false;
        }
        
        /// <summary>
        /// 重置计时器
        /// </summary>
        public void ResetTimer()
        {
            elapsedTime = 0f;
            UpdateTimerDisplay();
        }
        
        /// <summary>
        /// 重置并开始计时
        /// </summary>
        public void RestartTimer()
        {
            elapsedTime = 0f;
            isRunning = true;
        }
        
        /// <summary>
        /// 获取当前经过的时间（秒）
        /// </summary>
        public float GetElapsedTime()
        {
            return elapsedTime;
        }
        
        /// <summary>
        /// 更新计时器显示
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (timerDisplay != null)
            {
                timerDisplay.text = FormatTime(elapsedTime);
            }
        }
        
        /// <summary>
        /// 格式化时间为字符串
        /// </summary>
        private string FormatTime(float timeInSeconds)
        {
            int hours = (int)(timeInSeconds / 3600);
            int minutes = (int)((timeInSeconds % 3600) / 60);
            int seconds = (int)(timeInSeconds % 60);
            
            if (timeFormat == TimeFormat.HHMMSS || hours > 0)
            {
                return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
            }
            else
            {
                return string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
    }
} 