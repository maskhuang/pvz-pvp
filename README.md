# PvZ-PVP 游戏项目

这是一个基于Unity开发的植物大战僵尸多人对战版本游戏，实现了玩家之间的PVP对战模式。

## 项目说明

本项目是植物大战僵尸游戏的创新版本，在保留原版游戏核心玩法的基础上，增加了玩家对战机制。玩家可以控制植物方或僵尸方，与其他玩家进行实时对战。

### 游戏特色

- **双方对战**：玩家可以选择扮演植物方或僵尸方
- **多种角色**：包含多种植物和僵尸角色，各具特色
- **实时对战**：基于Photon实现的网络对战功能
- **关卡设计**：多种不同风格的对战地图

## 技术栈

- Unity 2021
- C#
- Photon Unity Networking (PUN)
- 自定义动画系统

## 项目结构

项目主要包含以下部分：

- `/PvZ-Unity/Assets/`：游戏核心资源
  - `/Scripts`：游戏脚本
  - `/Prefabs`：预制体
  - `/Scenes`：场景文件
  - `/Resources`：游戏资源

## 安装和运行

1. 克隆仓库：`git clone https://github.com/yourusername/pvp-pvz2.git`
2. 使用Unity Hub打开项目文件夹
3. 在Unity编辑器中打开主场景
4. 点击运行按钮进行测试

## 游戏玩法说明

- 植物方：种植不同类型的植物防御僵尸进攻，保护基地
- 僵尸方：控制不同类型的僵尸攻击对方基地
- 游戏目标：摧毁对方基地或在时间结束时拥有更高分数

## 贡献指南

欢迎提交问题报告或功能建议。如果您想为项目做出贡献，请遵循以下步骤：

1. Fork 本仓库
2. 创建功能分支：`git checkout -b feature/YourFeature`
3. 提交更改：`git commit -m 'Add some feature'`
4. 推送到分支：`git push origin feature/YourFeature`
5. 提交Pull Request

## 许可证

本项目采用 MIT 许可证。详情请见 [LICENSE](LICENSE) 文件。

## 联系方式

如有问题或建议，请通过以下方式联系我们：

- Email: yourname@example.com
- GitHub Issues: [提交问题](https://github.com/yourusername/pvp-pvz2/issues)
