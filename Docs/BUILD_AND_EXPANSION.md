# 打包、联机与道具赛扩展

## Windows 打包

1. `File > Build Settings`，平台选择 `PC, Mac & Linux Standalone`，Target Platform 选 Windows。
2. 将 `Race_City_01` 加入 Scenes In Build。
3. `Player Settings`：
   - Company / Product：使用自己的工作室与原创项目名
   - Color Space：Linear
   - Scripting Backend：IL2CPP
   - Api Compatibility：`.NET Standard 2.1`
   - Target Architectures：x86_64
   - Active Input Handling：Input Manager (Old) 或 Both
4. Resolution 默认 `1920 × 1080`、允许全屏切换。
5. `Build` 到项目目录之外的 `Builds/Windows`。
6. 在目标机器测试键盘输入、存档、首次运行防火墙弹窗和 60/120 Hz 帧率。

Android/iOS 打包前，应把 HUD 包进 Safe Area、降低阴影距离至 45–60、Render Scale 调到 0.8–0.9，并使用 ASTC 纹理压缩。

## 多人联机扩展

建议选用 Unity Netcode for GameObjects、Photon Fusion 或专用服务器框架，但不要把现有 `KartController` 直接做成“每帧同步 Transform”。

推荐拆分：

1. `KartInputFrame`：只保存 tick、油门、转向、漂移、氮气按键。
2. `KartMotor`：由输入和确定性参数推进车辆；尽量减少依赖不确定的 PhysX 碰撞结果。
3. 本地玩家：客户端预测并立即反馈操控。
4. 服务器：权威验证速度、氮气余额、检查点顺序和完赛时间。
5. 客户端：保存 100–200 ms 输入历史，收到服务器快照后回滚重演。
6. 远端车辆：快照插值，不参与本地预测。
7. 发车：服务器广播统一服务器时间，客户端用时钟偏移显示倒计时。
8. 排名：根据已完成圈数、最后检查点索引、沿赛道样条的距离计算，不能只比世界坐标。

将 `RaceManager` 改造成状态机：

```text
Lobby → Loading → Countdown → Racing → Finishing → Results
```

只有服务器能推进状态和确认过线。PlayerPrefs 仅适合单机；线上成绩应由后端签名保存，并记录赛道版本、车辆配置和反作弊摘要。

## 道具赛扩展

新增模块：

- `ItemBoxTrigger`：服务器确认拾取，进入冷却并播放重生表现。
- `ItemInventory`：持有 1–2 个道具，只公开必要状态。
- `ItemDefinition`：ScriptableObject，配置权重、目标类型、冷却与表现 Prefab。
- `ItemDirector`：根据名次和比赛阶段做加权抽取，避免第一名持续获得强进攻道具。
- `ProjectileMotor`：服务器权威命中，客户端只预测视觉轨迹。
- `StatusEffectController`：统一管理护盾、减速、眩晕、加速，按优先级和持续时间叠加。

原创道具示例：潮汐泡泡（短时减速）、电磁路标（临时改变局部路线提示）、港湾护盾（抵消一次攻击）、纸飞机无人机（追踪前一名但可被急弯甩脱）。道具名称、轮廓、颜色、音效和规则都应形成自己的体系。

## 后续工程化

- 用 ScriptableObject 保存车辆调校参数，而不是把数值散落在 Prefab。
- 为计圈顺序、时间格式、氮气消耗编写 EditMode 测试。
- 增加对象池管理粒子、道具和路边动态物体。
- 记录匿名遥测：赛道完成率、弯道失败率、平均漂移时长和氮气使用点。
- 对美术资源启用 Addressables，按赛道分组下载。
