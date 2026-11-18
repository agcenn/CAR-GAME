# Ushi Arcade Car Physics 接入说明

本工程不包含 Ushi Arcade Car Physics 的源码、模型、特效或资源。`UshiCarPhysicsBridge` 是一个不依赖特定命名空间的接入层：它让本工程的 AI、回放、赛道和 UI 可以把统一的 `RaceVehicleInput` 交给你已合法导入的 Ushi 控制器。

## 接入步骤

1. 将你自己的 Ushi Arcade Car Physics 包导入 Unity 工程。
2. 在玩家或 AI 赛车根节点上保留 `UshiCarPhysicsBridge`。
3. 把 Ushi 的车辆控制组件拖到 **Target Controller**。
4. 勾选 **Prefer Ushi Controller**。
5. 根据你当前 Ushi 版本的公开方法填写桥接方法名称：
   - 输入方法必须接收 `RaceVehicleInput` 一个参数，或 `(float throttle, float steering, bool drift, bool boost)` 四个参数。
   - 路面方法接收 `RoadSurfaceProfile`；若 Ushi 使用自己的路面数据类型，可在 Ushi 一侧添加一个很薄的公开包装方法做转换。
   - 灯光方法接收一个 `bool`。
6. 在 Ushi 控制器的输入来源中关闭直接读取键盘的逻辑，改为只读取桥接方法传入的值，避免与 `KartController` 重复输入。
7. 成功桥接后，`UshiCarPhysicsBridge.UsingUshi` 为 `true`，内置 `KartController` 会被禁用；AI、回放、名次和 HUD 不需要改动。

`VehicleLightController` 默认按 `L` 开关大灯，也会通过桥接器调用 Ushi 配置的灯光方法。

## 路面与特效

为 Asphalt、Grass、Sand、Ice 等每种地面建立 `RoadSurfaceProfile`：

- `lateralGripMultiplier`：横向抓地系数。
- `accelerationMultiplier`：动力输出系数。
- `skidColor`、`surfaceParticles`、`rollingLoop`：轮胎印、粒子和滚动声的美术接口。

把 `RoadSurfaceVolume` 放在对应道路碰撞体上；车根节点挂 `RoadSurfaceDetector` 并设定 fallback Profile。内置卡丁车会立即获得不同抓地和加速表现；启用 Ushi 时桥接器会转交当前 Profile。

## 边界

桥接层不反编译、复制或模拟 Ushi 的专有实现。因为本工程内没有 Ushi 包，方法名称需要由你按实际 API 配置；导入后若你把组件截图或 API 签名发给我，我可以将桥接器改成强类型适配器。
