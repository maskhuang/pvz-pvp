using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PvZ.Timer
{
    /// <summary>
    /// 计时器使用示例
    /// </summary>
    public class TimerExample : MonoBehaviour
    {
        [Header("UI 元素")]
        public TMP_Text timerText;
        public Button startButton;
        public Button pauseButton;
        public Button resetButton;
        
        private Timer timer;
        
        void Start()
        {
            // 获取或添加计时器组件
            timer = GetComponent<Timer>();
            if (timer == null)
            {
                timer = gameObject.AddComponent<Timer>();
            }
            
            // 设置计时器的显示Text组件
            timer.timerDisplay = timerText;
            
            // 设置按钮事件
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClick);
            }
            
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseButtonClick);
            }
            
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetButtonClick);
            }
        }
        
        void OnStartButtonClick()
        {
            timer.StartTimer();
        }
        
        void OnPauseButtonClick()
        {
            timer.PauseTimer();
        }
        
        void OnResetButtonClick()
        {
            timer.ResetTimer();
        }
    }
} 