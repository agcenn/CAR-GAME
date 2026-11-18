# 原创美术、材质与灯光

## 视觉方向

目标是“明亮、易读、略带玩具质感的都市竞速”，不是对特定作品逐像素复刻。建立独有识别点：海港霓虹、珊瑚橙与青绿色、圆角模块化建筑、波浪形箭头、原创赛事品牌。

## URP

在 `Assets/Settings` 创建 Universal Render Pipeline Asset：

- HDR：开
- MSAA：4x
- Render Scale：1.0（移动端可降到 0.85）
- Main Light Shadows：2048，4 Cascades，Max Distance 90
- Additional Lights：Per Pixel，单物体上限 4
- SRP Batcher：开

Quality 设为：

- Shadow Distance `90`
- Shadow Resolution `High`
- Soft Shadows 开
- VSync `Every V Blank`；竞速手感测试时也可关闭并把目标帧率设为 120

## 太阳与环境

- Directional Light Rotation：`(48, -32, 0)`
- Color：`#FFF1D3`
- Intensity：`1.25`
- Indirect Multiplier：`1.0`
- Environment Lighting Source：Gradient
- Sky / Equator / Ground：`#78CFFF / #DCEEFF / #799083`
- Reflection Intensity：`0.9`
- Fog：Linear，Start `90`，End `340`，Color `#B8DBE9`

建筑使用 Baked GI，车辆和动态物体使用 Light Probes。街区中心放一个 `Box Projection` Reflection Probe，Resolution `256`，使用 Baked。

## 材质基线

| 材质 | Shader | Metallic | Smoothness | 说明 |
|---|---|---:|---:|---|
| Asphalt | URP/Lit | 0.00 | 0.28 | 法线强度 0.45，平铺 4–6 m |
| StartTiles | URP/Lit | 0.00 | 0.48 | 蓝 `#208DCE`、暖白 `#F2F5EE` |
| KartPaint | URP/Lit | 0.55 | 0.72 | Clear coat 0.35，颜色使用原创方案 |
| Barrier | URP/Lit | 0.05 | 0.40 | 围挡 Logo 使用原创贴花 |
| Arrow | URP/Lit | 0.10 | 0.55 | Emission 白色，强度 2.2 |
| Window | URP/Lit | 0.35 | 0.75 | 不透明假反射优先，节省透明开销 |

## Global Volume

- Tonemapping：ACES
- Bloom：Threshold `1.05`，Intensity `0.23`，Scatter `0.65`
- Color Adjustments：Post Exposure `0.08`，Contrast `8`，Saturation `9`
- White Balance：Temperature `3`，Tint `-2`
- Lift/Gamma/Gain：仅轻微抬高 Gain，避免黑位发灰
- Vignette：Intensity `0.12`，Smoothness `0.45`
- Motion Blur：默认关；高速拖影优先用车尾 Trail/粒子表达，保证 HUD 清晰
- Chromatic Aberration：关

## 粒子

- 尾气：灰白软粒子，Start Lifetime `0.35–0.7`，Speed `0.4–1.2`，Rate `12`。
- 漂移烟：白灰粒子，Lifetime `0.6–1.1`，Rate `28`，Size `0.25–0.6`。
- 氮气：锥形橙黄核心 + 青蓝外焰两层，Lifetime `0.08–0.18`，使用 Local Simulation。
- 尾迹：额外使用两个透明 Ribbon/Trail，Lifetime `0.25`，从橙黄渐隐到透明。

所有纹理、Logo、字体、模型与音频都应保存来源和授权记录。即使资产“看起来通用”，也不要从目标游戏客户端提取。
