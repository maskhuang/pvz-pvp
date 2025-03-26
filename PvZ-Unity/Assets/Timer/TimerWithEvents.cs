using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace PvZ.Timer
{
    /// <summary>
    /// 增强版计时器：带有事件回调的正计时功能
    /// </summary>
    public class TimerWithEvents : MonoBehaviour
    {
        [Header("计时器设置")]
        [Tooltip("是否在Start时自动开始计时")]
        public bool autoStart = false;
        
        [Tooltip("显示时间的TextMeshPro组件，如果有的话")]
        public TMP_Text timerDisplay;
        
        [Tooltip("是否有目标时间")]
        public bool hasTargetTime = false;
        
        [Tooltip("目标时间（秒）")]
        public float targetTime = 60f;
        
        [Header("事件")]
        [Tooltip("计时开始时触发")]
        public UnityEvent onTimerStart;
        
        [Tooltip("计时暂停时触发")]
        public UnityEvent onTimerPause;
        
        [Tooltip("计时重置时触发")]
        public UnityEvent onTimerReset;
        
        [Tooltip("达到目标时间时触发")]
        public UnityEvent onTargetTimeReached;
        
        [Tooltip("每秒触发一次")]
        public UnityEvent onSecondElapsed;
        
        // 计时器当前是否正在运行
        private bool isRunning = false;
        
        // 已经运行的时间（秒）
        private float elapsedTime = 0f;
        
        // 上一次整秒时间，用于触发整秒事件
        private int lastSecond = -1;
        
        // 是否已经到达目标时间
        private bool targetReached = false;
        
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
                
                // 检查是否有新的整秒
                int currentSecond = Mathf.FloorToInt(elapsedTime);
                if (currentSecond > lastSecond)
                {
                    lastSecond = currentSecond;
                    onSecondElapsed?.Invoke();
                }
                
                // 检查是否达到目标时间
                if (hasTargetTime && !targetReached && elapsedTime >= targetTime)
                {
                    targetReached = true;
                    onTargetTimeReached?.Invoke();
                    PauseTimer();
                }
                
                UpdateTimerDisplay();
            }
        }
        
        /// <summary>
        /// 开始计时
        /// </summary>
        public void StartTimer()
        {
            isRunning = true;
            onTimerStart?.Invoke();
        }
        
        /// <summary>
        /// 暂停计时
        /// </summary>
        public void PauseTimer()
        {
            isRunning = false;
            onTimerPause?.Invoke();
        }
        
        /// <summary>
        /// 重置计时器
        /// </summary>
        public void ResetTimer()
        {
            isRunning = false;
            elapsedTime = 0f;
            lastSecond = -1;
            targetReached = false;
            UpdateTimerDisplay();
            onTimerReset?.Invoke();
        }
        
        /// <summary>
        /// 重置并开始计时
        /// </summary>
        public void RestartTimer()
        {
            ResetTimer();
            StartTimer();
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
            
            if (hours > 0)
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