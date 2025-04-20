using UnityEngine;

namespace PvZ.UI
{
    /// <summary>
    /// 提供基于分辨率缩放的工具类
    /// 确保UI元素在不同分辨率下保持相对尺寸和位置
    /// </summary>
    public static class ResolutionScaler
    {
        // 参考分辨率（4K）
        public const float REFERENCE_WIDTH = 3840f;
        public const float REFERENCE_HEIGHT = 2160f;
        
        // 卡片相关的参考尺寸
        public const float CARD_WIDTH = 43f;
        public const float CARD_HEIGHT = 61f;
        
        /// <summary>
        /// 获取当前分辨率的缩放因子
        /// </summary>
        /// <returns>缩放因子</returns>
        public static float GetScaleFactor()
        {
            float widthScale = Screen.width / REFERENCE_WIDTH;
            float heightScale = Screen.height / REFERENCE_HEIGHT;
            
            // 使用较小的缩放因子，避免UI元素溢出屏幕
            return Mathf.Min(widthScale, heightScale);
        }
        
        /// <summary>
        /// 根据参考尺寸和当前分辨率计算实际尺寸
        /// </summary>
        /// <param name="referenceSize">参考分辨率下的尺寸</param>
        /// <returns>适应当前分辨率的实际尺寸</returns>
        public static Vector2 ScaleSize(Vector2 referenceSize)
        {
            float scale = GetScaleFactor();
            return new Vector2(referenceSize.x * scale, referenceSize.y * scale);
        }
        
        /// <summary>
        /// 缩放单个数值
        /// </summary>
        /// <param name="referenceValue">参考分辨率下的数值</param>
        /// <returns>适应当前分辨率的实际数值</returns>
        public static float ScaleValue(float referenceValue)
        {
            return referenceValue * GetScaleFactor();
        }
        
        /// <summary>
        /// 获取卡片在当前分辨率下的标准尺寸
        /// </summary>
        /// <returns>卡片尺寸</returns>
        public static Vector2 GetCardSize()
        {
            float scale = GetScaleFactor();
            return new Vector2(CARD_WIDTH * scale, CARD_HEIGHT * scale);
        }
        
        /// <summary>
        /// 为RectTransform设置适应当前分辨率的大小
        /// </summary>
        /// <param name="rectTransform">要设置的RectTransform</param>
        /// <param name="referenceSize">参考分辨率下的尺寸</param>
        public static void SetScaledSize(RectTransform rectTransform, Vector2 referenceSize)
        {
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = ScaleSize(referenceSize);
            }
        }
        
        /// <summary>
        /// 打印当前分辨率和缩放信息（调试用）
        /// </summary>
        public static void LogScalingInfo()
        {
            float scale = GetScaleFactor();
            Debug.Log($"[ResolutionScaler] 当前分辨率: {Screen.width}x{Screen.height}");
            Debug.Log($"[ResolutionScaler] 参考分辨率: {REFERENCE_WIDTH}x{REFERENCE_HEIGHT}");
            Debug.Log($"[ResolutionScaler] 缩放因子: {scale}");
            Debug.Log($"[ResolutionScaler] 卡片尺寸: {GetCardSize()}");
        }
    }
} 