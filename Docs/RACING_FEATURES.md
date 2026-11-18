# 赛车功能模块

## AI

`RacingPath` 是 AI、排名和小地图共用的闭环路径数据。选择路径对象后：

1. 在赛道中心线放置空物体。
2. 逐个选中空物体，在 Inspector 点击 **Add Selected**。
3. 在 Scene View 拖动青色节点即可微调。

`RaceAIDriver` 每帧从路径推算前方目标点，并通过三个值控制赛车：

- 根据前方两个弯的估计半径，用 `v = sqrt(a × r)` 自动生成安全目标速度；半径越小，刹车越早。
- 在前方 OverlapSphere 范围内发现其他 `KartController` 后，减小油门并向空侧偏移，避免追尾和强行并线。
- 大转向且需要降速时触发漂移输入。

演示场景已生成三台对手车。正式比赛中，给每台 AI 赋不同的 `cruiseSpeedKph`、避让距离和误差模型，即可形成不同驾驶风格。

## 回放

`RaceReplayRecorder` 以默认 30 Hz 记录：

- 位置、旋转、刚体速度；
- 油门、转向、漂移、氮气；
- 车速、氮气槽、当前圈、单圈与总时间。

完成比赛时自动保存至：

```text
%USERPROFILE%/AppData/LocalLow/<Company>/<Product>/Replays/
```

`RaceReplayPlayer` 从 JSON 读取并插值播放帧；它重放已记录的仪表数据，而不是重新运行物理，因此车身和仪表能和记录时一致。实现倒带只需把 `replayTime` 以负速度递减、用二分查找定位帧，再向相邻帧插值。

## 小地图

- `RuntimeMinimap` 是游戏内顶视相机，输出 RenderTexture，可直接接 RawImage。演示 HUD 会在左下角显示它。
- 菜单 **Neon Harbor > Minimap Capture** 打开航拍工具；设置中心、正交尺寸和分辨率后，输出 `Assets/Art/UI/Minimap_Aerial.png`。以这张图为底稿描画正式小地图即可。

## UI

演示 HUD 已展示：发车信号、单圈、总时间、最佳圈、圈数、位置、速度、氮气、课程地图和终点提示。正式版使用 `RaceHUD` 加 TextMeshPro 时，可把这些模块拆成独立 Canvas Prefab，自由组合到不同赛事模式。
