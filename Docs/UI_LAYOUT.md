# HUD 布局规范

基准分辨率使用 `1920 × 1080`。Canvas 设置：

- Render Mode：`Screen Space - Overlay`
- Canvas Scaler / UI Scale Mode：`Scale With Screen Size`
- Reference Resolution：`1920 × 1080`
- Screen Match Mode：`Match Width Or Height`
- Match：`0.5`
- 所有文字使用 TextMeshPro，建议使用有合法商用授权的原创/开源粗体字体。

下表中的 Position 是 `RectTransform.anchoredPosition`，Size 是 `sizeDelta`。

| 节点 | Anchor / Pivot | Position | Size | 样式与文字格式 |
|---|---|---:|---:|---|
| `HUD/TopLeftPanel` | 左上 `(0,1)` / `(0,1)` | `(36,-34)` | `(360,150)` | 深蓝黑 72% 透明、右下斜切视觉 |
| `CompactLap` | 左上 | `(24,-18)` | `(160,42)` | 28 px，`LAP  1/2` |
| `Rank` | 左上 | `(22,-58)` | `(150,66)` | 52 px，`1 / 8`，数字加粗 |
| `PlayerId` | 左上 | `(164,-72)` | `(174,34)` | 23 px，`RACER_01` |
| `HUD/TopRightPanel` | 右上 `(1,1)` / `(1,1)` | `(-36,-34)` | `(410,184)` | 深蓝黑 72% 透明 |
| `LapProgress` | 右上 | `(-20,-12)` | `(370,52)` | 38 px 右对齐，`1/2 LAP` |
| `LapTime` | 右上 | `(-20,-65)` | `(370,30)` | 24 px，`LAP  00:12.345` |
| `TotalTime` | 右上 | `(-20,-101)` | `(370,30)` | 24 px，`TIME  00:35.902` |
| `BestTime` | 右上 | `(-20,-137)` | `(370,30)` | 24 px，`BEST  00:31.778` |
| `Countdown` | 中央 `(0.5,0.5)` | `(0,70)` | `(720,280)` | 210 px 白色，粗黑描边 0.22，居中 |
| `SpeedGauge` | 右下 `(1,0)` / `(1,0)` | `(-46,38)` | `(300,300)` | 圆环底图 + Filled/Radial 360 |
| `SpeedFill` | 中央拉伸 | `(0,0)` | `(0,0)` | 红橙渐变：`#FF342F → #FFB000` |
| `SpeedText` | 中央 | `(0,-8)` | `(245,90)` | 58 px，`087 km/h` |
| `NitroFill` | 右下 | `(-72,20)` | `(230,18)` | 青蓝色 Filled/Horizontal，0–100% |
| `FinishPanel` | 全屏拉伸 | `(0,0)` | `(0,0)` | 黑色 68% 遮罩，默认隐藏 |

`RaceHUD.FormatTime` 输出固定 `mm:ss.fff`，使用整数毫秒截断，避免 `59.9996` 被四舍五入显示成下一秒。赛事超过 99 分钟时分钟字段会自然扩展。

## 仪表盘制作

1. 准备原创的透明 PNG 圆环底图。
2. `SpeedFill` 的 Image Type 设为 `Filled`，Fill Method 设为 `Radial 360`，Origin 设为 `Bottom`，Clockwise 打开。
3. Inspector 中给 `speedGradient` 添加：
   - `0.00 #FF342F`
   - `0.55 #FF6A22`
   - `1.00 #FFB000`
4. `NitroFill` 使用青色到蓝白色的横向条；脚本只改变 fillAmount。

## 自适应与安全区

16:9 是视觉基准。移动端需增加 `SafeAreaPanel`，在运行时读取 `Screen.safeArea` 后将四个 HUD 角块放入该容器。不要用绝对屏幕坐标驱动 UI；上述坐标均建立在锚点之上。
