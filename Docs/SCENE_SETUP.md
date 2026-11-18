# 从空场景搭到可运行

## 1. 创建车辆

1. 新建空对象 `PlayerKart`，位置 `(0, 0.65, -6)`。
2. 添加 `Rigidbody`：
   - Mass `180`
   - Drag `0.05`
   - Angular Drag `3`
   - Use Gravity 开
   - Constraints 勾选 Freeze Rotation X、Z
3. 添加尺寸约 `(1.25, 0.65, 2.0)` 的 `BoxCollider`，底部略高于轮胎最低点。
4. 添加 `KartController` 与 `DriftNitroSystem`。Ground Mask 只选择 `Ground` 层。
5. 将原创车体和 Q 版角色模型作为子节点。角色应是独立原创轮廓与服装设计。
6. 后轮接两个窄 `TrailRenderer`，Time `1.8`、Min Vertex Distance `0.08`、Width `0.11 → 0`，使用半透明深灰材质。
7. 车尾建立两个氮气粒子与一个尾气粒子，再添加 `KartEffectsController` 并连接数组。

推荐初始手感参数已写入脚本默认值：最高约 122 km/h、加速 24、制动 38、常态横向抓地 11、漂移抓地 2.4、氮气倍率 1.38、持续 2.1 秒。调手感时每次只改一组参数，并在同一弯道记录入弯速度、出弯速度和漂移持续时间。

## 2. 创建赛道

1. 建立 `TrackRoot`，用 ProBuilder 或自制低模搭一条宽 `11 m`、中心线长度约 `650 m` 的闭环。
2. 起点直道铺原创蓝白方格材质。单格建议 `1.5 × 1.5 m`，避免使用任何既有游戏标志。
3. 两侧用原创围挡 Prefab，间隔约 `4 m`。围挡上只放项目原创 Logo，例如 `NEON HARBOR GRAND PRIX`。
4. 起点拱门跨度 `14 m`、净高 `6 m`，造型采用海港霓虹/流线金属语言，避免复刻现有拱门。
5. 用原创模块化建筑组成美式都市街区：道路后退 `3–6 m`，建筑高 `12–35 m`；每 `16–24 m` 放一盏路灯，每 `20–32 m` 放棕榈树。
6. 碰撞体尽量使用 BoxCollider 组合。路面放 `Ground` 层，墙体放 `Barrier` 层。
7. 弯前摆放白色原创箭头网格，材质开启 Emission，挂 `DirectionalArrow`。

## 3. 计圈触发器

1. 新建 `RaceSystems`，添加 `RaceClock`、`RaceManager`、`CheckpointSequence`。
2. 全赛道均匀放置 4 个跨越路宽的 BoxCollider，勾选 Is Trigger，依次命名 `CP_00` 至 `CP_03`。
3. 每个添加 `CheckpointTrigger`，Index 依次为 `0,1,2,3`，都指向同一个 `CheckpointSequence`。
4. 起终点线上再放一个 Is Trigger BoxCollider，添加 `LapLineTrigger`。
5. `CheckpointSequence.checkpointCount` 设为 4 并连接 RaceManager。
6. RaceManager 连接 PlayerKart 和 RaceClock，Total Laps 设为 2。

车辆必须按顺序通过全部检查点，终点线才会解锁一次。这既防止倒车刷圈，也避免出生点直接计为第一圈。

## 4. 相机

主相机添加 `ThirdPersonKartCamera`：

- Target：`PlayerKart`
- Target Body：车辆 Rigidbody
- Offset：`(0, 3.1, -6.4)`
- Look Offset：`(0, 1.1, 2.3)`
- FOV：`67`
- Near Clip：`0.1`

如果车体遮住角色，将 Offset Y 增至 `3.4` 或 Look Offset Y 境至 `1.35`。

## 5. HUD

按 [UI_LAYOUT.md](UI_LAYOUT.md) 创建 Canvas 树。Canvas 根节点挂 `RaceHUD`，连接：

- Race Manager / Race Clock / Player Kart / Nitro System
- 左上 3 个 TMP Text
- 右上 4 个 TMP Text
- Countdown、Speed Text、Speed Fill、Nitro Fill

另建 `FinishPanel` 控制器并连接遮罩面板与结果文字。所有 Inspector 引用连接完成后，Play Mode 中会依次出现 `3`、`2`、`1`、`GO`，GO 时才解锁车辆。

## 6. 建议 Hierarchy

```text
Race_City_01
├─ Environment
│  ├─ TrackRoot
│  ├─ Buildings
│  ├─ Props
│  └─ NavigationArrows
├─ RaceSystems
│  ├─ Checkpoints
│  └─ LapLine
├─ PlayerKart
├─ Main Camera
├─ Lighting
│  ├─ Sun
│  ├─ ReflectionProbe_City
│  └─ Global Volume
└─ UI
   ├─ HUD
   └─ FinishPanel
```
