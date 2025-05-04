using UnityEngine;

namespace PvZ.Timer
{
    /// <summary>
    /// 为GameManagement添加Timer相关扩展方法
    /// </summary>
    public static class GameManagementExtension
    {
        /// <summary>
        /// 此方法供GameManagement.awakeAll()调用，用于启动计时器
        /// </summary>
        /// <param name="gameManagement">GameManagement实例</param>
        public static void StartTimer(this object gameManagement)
        {
            if (Timer.Instance != null)
            {
                Timer.Instance.StartTimer();
                Debug.Log("Timer started from GameManagement.awakeAll()");
            }
            else
            {
                Debug.LogWarning("尝试启动Timer，但Timer实例不存在！");
            }
        }
        
        /// <summary>
        /// 此方法供GameManagement使用，用于暂停计时器
        /// </summary>
        /// <param name="gameManagement">GameManagement实例</param>
        public static void PauseTimer(this object gameManagement)
        {
            if (Timer.Instance != null)
            {
                Timer.Instance.PauseTimer();
            }
        }
        
        /// <summary>
        /// 此方法供GameManagement使用，用于重置计时器
        /// </summary>
        /// <param name="gameManagement">GameManagement实例</param>
        public static void ResetTimer(this object gameManagement)
        {
            if (Timer.Instance != null)
            {
                Timer.Instance.ResetTimer();
            }
        }
    }
} 