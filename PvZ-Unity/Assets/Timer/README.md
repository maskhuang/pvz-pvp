# 计时器组件使用指南

## 简介

本计时器组件为Unity项目提供了简单易用的正计时功能。可以用于游戏内各种计时需求，如关卡计时、技能冷却等。计时器采用单例模式设计，可以在项目的任何地方轻松访问。

## 组件类型

计时器组件提供了三个不同的类：

1. **Timer**: 基础计时器，提供简单的正计时功能（单例模式实现）
2. **TimerWithEvents**: 增强版计时器，提供事件回调功能
3. **TimerExample**: 使用示例脚本

## 使用方法

### 基础计时器 (Timer)

#### 添加到游戏对象

1. 选择要添加计时器的游戏对象
2. 在Inspector面板中点击"Add Component"
3. 搜索"Timer"并选择PvZ.Timer.Timer脚本

#### 配置选项

- **Auto Start**: 是否在Start时自动开始计时
- **Timer Display**: 用于显示时间的TextMeshPro文本组件（可选）
- **Time Format**: 时间显示格式（MM:SS或HH:MM:SS）

#### 主要方法

- `StartTimer()`: 开始计时
- `PauseTimer()`: 暂停计时
- `ResetTimer()`: 重置计时器
- `RestartTimer()`: 重置并开始计时
- `GetElapsedTime()`: 获取当前经过的时间（秒）

### 单例模式访问

由于Timer采用单例模式设计，您可以在代码中的任何位置访问计时器：

```csharp
// 在任何脚本中访问Timer
if (Timer.Instance != null)
{
    // 开始计时
    Timer.Instance.StartTimer();
    
    // 获取经过的时间
    float time = Timer.Instance.GetElapsedTime();
}
```

### 与GameManagement集成

计时器提供了与GameManagement类集成的扩展方法：

```csharp
// 在GameManagement类中
public void SomeMethod()
{
    // 使用扩展方法控制计时器
    this.StartTimer();  // 开始计时
    this.PauseTimer();  // 暂停计时
    this.ResetTimer();  // 重置计时器
}
```

### 增强版计时器 (TimerWithEvents)

增强版计时器拥有基础计时器的所有功能，并增加了以下特性：

#### 额外配置选项

- **Has Target Time**: 是否有目标时间
- **Target Time**: 目标时间（秒）

#### 事件回调

- **On Timer Start**: 计时开始时触发
- **On Timer Pause**: 计时暂停时触发
- **On Timer Reset**: 计时重置时触发
- **On Target Time Reached**: 达到目标时间时触发
- **On Second Elapsed**: 每秒触发一次

## 代码示例

```csharp
// 获取计时器组件
Timer timer = GetComponent<Timer>();

// 开始计时
timer.StartTimer();

// 暂停计时
timer.PauseTimer();

// 获取经过的时间
float elapsedTime = timer.GetElapsedTime();
Debug.Log("已经过时间: " + elapsedTime + "秒");

// 重置计时器
timer.ResetTimer();
```

## UI集成示例

1. 创建Canvas和TextMeshPro - Text (UI)组件用于显示时间
2. 创建Button组件用于控制计时器（开始、暂停、重置）
3. 将TimerExample脚本添加到一个游戏对象
4. 将TMP_Text和Button组件拖拽到TimerExample的相应字段中

## 注意事项

- 计时器使用`Time.deltaTime`进行计时，因此会受到时间缩放(Time.timeScale)的影响
- 如果不需要UI显示，可以将Timer Display设为null
- 对于需要精确计时的场景，请考虑使用TimerWithEvents并监听onSecondElapsed事件
- 本计时器使用TextMesh Pro组件显示文本，请确保项目中已导入TextMesh Pro包
- 由于Timer使用单例模式，在一个场景中只应该存在一个Timer实例 