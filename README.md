# The Micro Universe

A mobile minigame focusing on PCG, and fun.

malosgao · Gavin KG · gavin_kg@outlook.com

---

[TOC]



## 游戏简述

### 游戏剧情

在我们日常使用的手机屏幕的背后，其实是一个持续不断为用户提供服务的微观世界。

这个游戏首先会提供给玩家一个涂色程序，当玩家画好并且点击上色时，由于微观世界的小故障，玩家在不经意间被卷入到了背后的世界中。世界的结构和玩家绘制的万花筒图案组成一致，但由于并没有成功上色的缘故，场景依然是一片黑暗并失去了色彩。在这个微观世界中，居住着的是一些被称为“待定”（可以理解为集成电路中的电子）的成员，他们穿梭于不同区域之间，像电路一般，移动、吸引并点亮区域中的“能量柱”，维持着世界持续运转。

玩家成为了一名“待定”，来到了这个暂时故障的都市。玩家需要操作着自己控制的“待定”在这个世界中，重新开启区域中所有的“能量柱”，继而解锁并点亮一片又一片的区域。当所有区域都被点亮时，整个城市重新恢复了原来的色彩，而巧合的是，整个城市的色彩正好是涂色程序所渲染出的色彩。玩家控制的“待定”完成了目标，欣赏着由于自己的努力而恢复了功能和色彩的城市，回到了原本的身体中。屏幕上显示的，则是一个完整涂好色的图案。

### 游戏内核

整个游戏其实是一个打破第四面墙的 Meta 游戏，并且具有双关性。玩家执行手机程序，屏幕后的都市（类似于电路）负责运行程序的逻辑；玩家程序出现故障，“待定”通过自己的努力恢复都市的正常运转；玩家执行涂色，屏幕后的都市从一开始的黑暗到玩家解锁全部区域后的生机勃勃，并且整个都市的样式**直接为涂色的结果**。玩家同时具有两种身份，第一身份是涂色软件的使用者，第二身份是都市“待定”的操作者，服务于涂色软件的使用者（即第一身份）。

游戏使用 top-down 视角，通过摇杆或屏幕上虚拟摇杆来操控“待定”的运动。



## 游戏流程

### 开场界面

上来肯定是 Made with Unity，然后接上魔方工作室的标志和“由malosgao为您献上”，然后切黑。

### 万花筒绘制

// 20200705：milo 说要基于一个随机数种子，所以打算看看能不能用一个随机数滑条来代替用手绘制生成万花筒图案。如果能够允许除了随机数种子以外的更复杂的操作，可以同时保留画图和生成万花筒的方案。

从黑切入一个手机UI界面，用来给用户进行万花筒图案的绘制，可以参考《万花筒涂鸦》。界面亮色，左侧可以选择万花筒扇形数目（可选），右边可以选择颜色 Pattern（可选），下方一开始是一个进度条，当用户画到一定程度时进度条走满，显示出“上色！”按钮。

绘制界面需要限制一下玩家能够绘制的区域，整体画板区域是一个空心圆环，留出内环区域用来做中心节点（当前设想是内环不一定是城墙，可以和外部区域共享，看实现难度可能会改变这个设定），外环区域用来做外城墙；用户可绘制的区域是这个空心圆的一个小扇形。小扇形必须从下到上**完全绘制**进度条才会走满。

这里的UI风格设计可以不用完全按照手机UI，可以夸张一些，画风可以和《Flat Heros》类似，走 Pop 风格。

为了给用户绘制的时候其实是在控制后面 Micro Universe 城市的建成，界面选择为亮色来模拟背后城市的太阳光，并且当前画在Canvas上的图案可以做一次毛玻璃模糊后贴在后面，但是一定要糊，一是因为不会露馅，二是因为一开始绘制的时候不想给玩家meta的感觉。

### Loading 前

当用户点击“上色！”按钮后，模拟系统短时间失去响应，继而闪一下 Glitch 特效，界面背景切几次灰，模拟给后面世界的太阳供电的灯憋了的感觉，继而永久切灰。镜头前推，营造一种推入手机屏幕的感觉。玩家画的万花筒图案变成一个个粒子，带着扰动同样深入到屏幕中（此效果选作）。镜头这时已经完全推入“云层”，成为一个纯灰色的背景，此时可以开始Loading并且不会产生卡顿感觉了。

### Loading

Loading 期间屏幕不产生任何变动，摄像机直接 Culling = 0，按照下面列表开始生成资源：

* 对手绘的万花筒图（原图 2048x2048）模糊 + 二值化。下方根据需求 Blit 到相应的分辨率上，不再赘述；
* 降采样到 128x128，用 Flood Fill 拆出内部区域（除了圆心和圆形四周的区域），算出中心点，保留所有区域的 Texture 用来做灯光 Mask，剔除比较小的区域标记为不可游玩；
* 用最小生成树算出一条连通路径，算出路径和各个区域墙壁的交汇点并记录;
* 使用 Marching Square 生成城墙顶和四周的墙壁，并且结合轮廓生成塔楼和UV（UV1存放局部贴图UV，UV2存世界级别的UV）；
* 将每个内部区域变换到 Flatten 空间上；
* 对于每个变换后的内部区域，从交汇点开始（可能有多个交汇点）用 WFC 算出路网/建筑布局；
* 路网生成水平 Mesh （用于上纹理）和垂直 Mesh（用于变换回去以后给碰撞器）；
* 将建筑和路网 Mesh 变换回笛卡尔坐标系（顶点数量大的话可以用 Compute Shader）；
* 将墙壁和建筑生成 Mask，高斯模糊，用来做 AO；
* Top-down 渲染整个场景（或者只用整个场景的 Albedo 做 Replacement Material 渲染），高斯模糊做 GI 采样图；
* 手动配置 Light Probe 阵列，对于每个 Probe 用上面的 GI 采样图配置 SH 参数；
* 将变换后的路网 Mesh 赋予 Mesh Collider；
* 生成墙壁/建筑物 SDF（可以考虑用高斯模糊替代真正的距离场，即可以直接用AO图）；
* 使用 SDF 渲染绘制路网/建筑物轮廓；
* 用 Compute Shader 收束粒子到 SDF 生成附近的装饰物摆放位置。
* Quasirandom + SDF 撒 Stub，然后迭代位置；
* 初始化剩余控制器。

### Loading 后

场景配置好以后，视野从“云层”里推出来。之前提到，由于未知原因导致这里停止了运行，因此此处在空中划过几道闪电，照亮场景（冷光源直接闪几下），展现出整个程序化生成的世界。世界几乎黑暗，由主光源产生月光的效果，但地上供小球挂着的 Stub 闪着微微的光（可以是 Emission，也可以用金属材质，用Bloom出光晕）。小球出生点在一个区域的入口处（上述提到的交汇点）。镜头缓缓推向小球。之后根据 FTUE 参数弹出静态教程。

### Gameplay（待完善）

场景除了主摄像机拍摄以外，还有一个辅助正交摄像机拍摄光源的 Mask 到一张灰阶纹理（黑色代表场景的夜晚，白色代表白天）。纹理初始为黑色，每激活一个 Stub，会有一些白色半透明烟雾状粒子在Stub生成，并渲染到辅助摄像机中，产生此处被暂时照亮的效果。同时，摄像机会拍摄到一张永久的Mask图，当解锁一整个区域时，这个永久Mask图会逐渐叠加（噪声叠加）整个区域的Mask图（之前Flood Fill时生成过），产生整个区域被永久照亮的效果。场景中所有物体的Shader用世界坐标/区域长宽采样这张图做Mask来实现这种效果（和AO一样的做法）。

小球按照下面提到的核心玩法在不同区域中使用Stub穿梭，激活Stub继而解锁一整个区域。当一个完整的区域被解锁，除了叠加Mask将该区域从“夜晚”转换成“白天”之后，将根据之前提到的SDF的方法摆放场景的装饰物。摆放过程动态进行，效果是装饰物从地图上到下一个一个pop出来。完整照亮之后，也可以将那片区域的纹理从中心扩散“上”到模型上，即模型的纹理采样强度也通过这张Mask图做，当黑暗时模型完全没有纹理，暂时被照亮/白天的情况纹理全部显示。

当所有区域都解锁后，中心区域（玩家进入不到中心区域）的主Stub被点亮，整个场景完全变亮（Mask图逐渐全白），游玩结束。

### 结束界面

游玩结束后，镜头逐渐拉远并垂直朝向地面（可以同时调小 FOV，但是会有种希区柯克式变焦的感觉，不太喜欢），一段时间后上一个风格化后处理，继续上推，再过一段时间，之前开始界面的UI飞回场景中，按钮显示“已上色”。

一段时间过后，场景整体模糊，显示游戏的Logo“The Micro Universe”，背景淡出，流程结束。

有时间可以做一下stuff界面。



## 核心玩法

玩家控制的小球需要在场景中不断穿梭，解锁区域中的能量柱从而解锁整个区域，打开通往其它区域的大门。当玩家解锁全部区域时，游戏结束。

### 交互模型

玩家操控小球靠近能量柱并使用连接按钮连接能力，即可连接上一个能量柱（如果存在多个能够连接的能量柱，连接最近并且中间没有遮挡物的那个）。当连接上时，小球到能量柱的半径范围便锁死，即小球只能被能量柱吸引围绕这能量柱做圆周运动，类似于中间架起了一根弹簧，用来模拟电子之间的排斥和吸引。玩家通过向沿着圆周切线的方向继续移动左摇杆（用处见下），可以使得小球沿着圆周运动起来并加快速度。当玩家松开连接按钮时，小球和能量柱之间的连接断开，小球沿着当前运动方向飞出。

小球拥有能量条和驱动力。当小球并未连接能量柱时，可以通过推动左摇杆使得小球沿着该方向缓慢移动，此举消耗能量。玩家需要在能量耗尽时成功连接到一个能量柱，否则判定游戏结束，该区域能量柱变为未激活（类似于读取了存盘点）。连接能量柱时，能量条快速回满，此时操纵左摇杆做圆周运动时不消耗能量。因此，玩家需要谨慎选择小球射出的方向，以保证射出后小球能在不消耗完自身的能量，甚至直接借助惯性不消耗能量达到下一个能量柱。

当玩家在连接状态时围绕着能量柱圆周运动一周后，判定该能量柱被成功激活。

由于当不适应操作时，预判好小球射出的方向并准确的松开连接键比较困难。为了避免这种情况破坏玩家的游玩乐趣，这里将右摇杆设计为在连接的情况下射出方向的控制：连接时，如果推动了右摇杆到某个方向并释放连接按钮，小球并不会立刻沿该方向释放，而是加速继续做圆周运动直到速度方向与右摇杆所指方向相同再释放（类似于辅助瞄准），这段时间小球不受输入所控制，被称为“冲刺”。“冲刺”期间会消耗能量，此举在于鼓励玩家适应手动的操作模式，而并不去经常依赖辅助操作。“冲刺”方向会在屏幕上做出提示。

### 按键规则

#### 触摸屏

双摇杆设计：左摇杆操控小球驱动力的方向；右摇杆在没有推动时显示为一个按钮，按下触发连接，抬起终止连接。当按下按钮并移动时，按钮展开成为一个摇杆，控制“冲刺”方向，此时再抬起则触发冲刺（而并非立刻释放）。

#### 控制器

LS/L 控制驱动力方向；RS/R 控制冲刺方向；RB/R1 控制连接，按下触发连接，抬起终止连接。在连接时若不推动 RS/R，松开 RB/R1 判定为直接释放；否则判定为“冲刺”。

#### 键盘与鼠标

（当前不支持键盘鼠标操作）



## 技术要点

- [ ] 万花筒图案绘制器

- [x] Rect <-> Ring <->Flatten Map

- [x] Gaussian Blur

- [x] 核心玩法 Player Controller

- [x] Top-down capturer

- [x] Marching Square

  http://thebigsmall.uk/uncategorized/part-1-contour-maps/

  https://www.youtube.com/watch?v=yOgIncKp0BE

  用来生成城墙mesh。

  Why Marching Square instead of Dual Contouring?

  https://www.boristhebrave.com/2018/04/15/dual-contouring-tutorial/

  https://wordsandbuttons.online/interactive_explanation_of_marching_cubes_and_dual_contouring.html

- [x] Flood Fill

  算中心，隔离出子区域范围，提供玩法保障。

- [x] Minimum Spanning Tree (*MST*)

  算子区域连通路径。

- [ ] SDF Contour

  SDF Generator: https://catlikecoding.com/sdf-toolkit/docs/texture-generator/

  子区域美化，绘制墙壁/建筑物边界，路网边界

- [ ] WFC

  https://www.youtube.com/watch?v=0bcZb-SsnrA

  https://zhuanlan.zhihu.com/p/66416593

  https://github.com/mxgmn/WaveFunctionCollapse

  https://github.com/marian42/wavefunctioncollapse

  波函数坍缩用来做区域中路网/建筑的生成。

  > Based on: https://gridbugs.org/wave-function-collapse/
  >
  > * Image Preprocessing (can be done offline)
  >   * NxN scan with rotation and reflection
  >   * Generating Adjacency rules based on overlapping model
  >   * Generating Frequency Hints
  > * Core
  >   * Supports pre-collapsed tile / not-grid-like map
  >   * Entropy calculation with Caching
  >   * Collapse chooser
  >   * Collapsing and Contradictions
  >   * Propagating with enablers (supporters) and cascade removal
  > * Image Postprocessing
  >   * Convert tile index to final color

- [ ] Procedural Texture

  DDX/DDY wall tile

  程序化闪电。

- [ ] "Stub" 放置

  First run a Monte-Carlo method to find a point close to a global optimum and then run a gradient descent from that point for greater accuracy.

- [ ] SDF 装饰物摆放

![image-20200715171932818](README.assets/image-20200715171932818.png)

- [ ] 挖洞材质（Greater Equals）

这些技术要点满足之后，可以完全达到程序化生成关卡/模型/纹理/游玩机制的要求。



## 艺术风格参考

### 中心城市的总体结构


![RE:【討論】科學之進擊！如果你是活在巨人威脅下的達文西，你會想出 ...](README.assets/9dc75df9bc660e08655b9681a5c15e06.JPG)


![图像](README.assets/D8wI1ReVsAAvKNC.jpg)

![img](README.assets/0271e1ad9192fe09924e616925b35290.jpg)

![征選遊：進擊巨人小鎮Nordlingen｜即時新聞｜生活｜on.cc東網](README.assets/OSU-170519-10526-802-M.jpg)

### 城市配色及风格



![image-20200714221657585](README.assets/image-20200714221657585.png)

![Townscaper - Gameplay Trailer ▻ NEW GAMES 2020 - YouTube](README.assets/maxresdefault.jpg)

![image-20200704012121965](README.assets/image-20200704012121965.png)

![img](README.assets/Store_EpicZenGarden_01 (1)-1920x1080-82bc3c22e0437cd6a7acb7b9d7be92d7.png)

![image-20200716110518209](README.assets/image-20200716110518209.png)

![image-20200718101143625](README.assets/image-20200718101143625.png)

（移轴摄影，同样有一种微缩模型的感觉，符合主题）

### UI 界面配色


![img](README.assets/FlatHeroes_02-1024x576.jpg)



### 万花筒

![image-20200702091501914](README.assets/image-20200702091501914.png)


