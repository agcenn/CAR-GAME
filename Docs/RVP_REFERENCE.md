# Randomation Vehicle Physics 参考改造

参考仓库：[JustInvoke/Randomation-Vehicle-Physics](https://github.com/JustInvoke/Randomation-Vehicle-Physics)，2026-09-05 查阅 master 分支。仓库 README 声明最近测试的版本为 Unity 2019.2.9；本工程目标为 Unity 2022.3，采用参考模块结构后重新实现的轻量版本，没有整包导入旧项目设置。

## 已改动的源码

- `RaycastWheel.cs`：四轮独立射线接触、剔除本车碰撞体、接触点相对速度、弹簧阻尼、接触点施力及移动地面反作用力。参考 [Suspension.cs](https://github.com/JustInvoke/Randomation-Vehicle-Physics/blob/master/Assets/Scripts/Suspension/Suspension.cs) 与 [Wheel.cs](https://github.com/JustInvoke/Randomation-Vehicle-Physics/blob/master/Assets/Scripts/Drivetrain/Wheel.cs) 的职责划分。
- `KartController.cs`：组合四轮受力，前轮内外转角有区别，随速度减小转向范围；加入防倾杆和有限稳定辅助。取消强制偏航旋转，转弯来自轮胎力矩。后轮抓地下降形成持续漂移。
- `ArcadeDrivetrain.cs`：扭矩曲线、车轮转速反馈、五个前进挡、倒挡、自动换挡和短暂动力中断。参考 [GasMotor.cs](https://github.com/JustInvoke/Randomation-Vehicle-Physics/blob/master/Assets/Scripts/Drivetrain/GasMotor.cs) 的发动机与传动分离设计。
- `KartVisualMotion.cs`：轮胎随悬挂行程移动，滚动使用轮胎 RPM。
- HUD/音效/回放：接入发动机 RPM 和实际挡位，保存并插值玩家四轮视觉状态。
- AI：目标路径转为前轮所需转角，继续使用曲率减速和前车避让。

这是本项目自行实现的参考改造，不是 RVP 官方发行版，也没有导入其音频、字体、模型、贴图或损坏/脱轮系统。引用仓库的 MIT 声明保存在 `ThirdPartyNotices/RVP-MIT.txt`。

## 在 Unity 中使用

打开 `Assets/Scenes/Race_Coast_02.unity`。新控制器兼容原场景，运行时自动建立缺失的四轮布局，并调整旧底盘碰撞体以留出悬挂空间。也可通过 `Neon Harbor > Build Coast 02 Scene` 重建场景后保存。

自定义车模：选中车辆根节点，点击 `Neon Harbor > Setup Selected Vehicle Physics`。在控制器 Wheels 中设置四个安装点，顺序为前左、前右、后左、后右；默认轴距 1.7m、轮距 1.82m、轮胎半径 0.315m、弹簧自由长度 0.22m。默认每轮弹簧 6500N/m、阻尼 720N·s/m，车重 180kg。默认碰撞体适配仅针对演示车，自定义模型应关闭 `Fit Demo Chassis Collider` 并自行设置底盘。

## 验证状态

2026-09-05：34 个运行时脚本与 5 个编辑器脚本已通过 Unity 配套 C# 编译器检查。结果为 `SOURCE_COMPILE_OK`。这仅验证源码与程序集接口，不验证 PhysX 行驶表现、场景生成或最终打包。

启动游戏窗口的自动审批服务因额度限制拒绝了操作，因此本次参考改造尚未完成实机测试。此前的 `Logs/CoastBuild2.log` 成功记录属于改造前版本，不能混用。

源码静态编译：运行 `Tools/Validate-Source.ps1`，使用已安装 Unity 的编译器和项目程序集引用，输出独立临时程序集。

完整验证仍需 Unity 重新生成并打包，然后用 `--race-smoke-test` 跑两圈比赛、存档和回放。此前的 `Builds/Windows/NeonHarbor.exe` 不包含这次 RVP 参考改造，不能当作本次修改的验证结果。
