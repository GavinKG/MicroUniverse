# The Micro Universe

A mobile minigame focusing on PCG, and fun.

malosgao · Gavin KG · gavin_kg@outlook.com

---

[TOC]



## 游戏流程

### 开场界面

上来肯定是 Made with Unity，然后接上魔方工作室的标志和“由malosgao为您献上”，然后切黑。

### 万花筒绘制

从黑切入一个手机UI界面，用来给用户进行万花筒图案的绘制，可以参考《万花筒涂鸦》。界面亮色，左侧可以选择万花筒扇形数目（可选），右边可以选择颜色 Pattern（可选），下方一开始是一个进度条，当用户画到一定程度时进度条走满，显示出“上色！”按钮。

绘制界面需要限制一下玩家能够绘制的区域，整体画板区域是一个空心圆环，留出内环区域用来做中心节点（当前设想是内环不一定是城墙，可以和外部区域共享，看实现难度可能会改变这个设定），外环区域用来做外城墙；用户可绘制的区域是这个空心圆的一个小扇形。小扇形必须从下到上完全绘制进度条才会走满。

这里的UI风格设计可以不用完全按照手机UI，可以夸张一些，画风可以和《Flat Heros》类似，走 Pop 风格。

![img](README.assets/FlatHeroes_02-1024x576.jpg)

为了给用户绘制的时候其实是在控制后面 Micro Universe 城市的建成，界面选择为亮色来模拟背后城市的太阳光，并且当前画在Canvas上的图案可以做一次毛玻璃模糊后贴在后面，但是一定要糊，一是因为不会露馅，二是因为一开始绘制的时候不想给玩家meta的感觉。

### Loading 前

当用户点击“上色！”按钮后，模拟系统短时间失去响应，继而闪一下 Glitch 特效，界面背景切几次灰，模拟给后面世界的太阳供电的灯憋了的感觉，继而永久切灰。镜头前推，营造一种推入手机屏幕的感觉。玩家画的万花筒图案变成一个个粒子，带着扰动同样深入到屏幕中。镜头这时已经完全推入云层，

### Loading

### Loading 后

### Gameplay

### 结束界面




## 游戏原型流程

>   在每天我们划过的手机屏幕的下面，其实是一个微观世界。

一开始，游戏将向玩家呈现出一个非常普通（甚至简陋，和之后的微观世界做反差，达到惊艳的效果）的涂色应用，例如下图所示。该应用的画图方法类似于万花筒，在用户画线条时，线条将被“复制”多份并围绕摆放，这样减少画画复杂度同时提升几何美感。

![image-20200702091501914](README.assets/image-20200702091501914.png)

（当然这里不一定让用户非得花万花筒，提供其他的几何图案绘制形式，例如Voronoi等，也可以达到效果，毕竟之后的流程不要求万花筒形式，只要有线条就行。具体可以参考城市规划相关的文章，例如 [https://www.urban-hub.com/zh-hant/urbanization/%E9%80%9A%E8%BF%87%E8%88%AA%E7%A9%BA%E6%91%84%E5%BD%B1%E8%BF%9B%E8%A1%8C%E5%9F%8E%E5%B8%82%E8%A7%84%E5%88%92/](https://www.urban-hub.com/zh-hant/urbanization/通过航空摄影进行城市规划/)）

当玩家绘制完线条时，一个“涂色”按钮将会出现，玩家会知道点击此按钮以后，线稿将会被“上色”。点击按钮之后，神奇的事情发生了：屏幕Glitch，图形逐渐放大，手机用户被渐渐吸入手机屏幕（当然，这里meta的成分会通过特效来完成，例如图片放大），穿越层层电路板，最终来到了一个微观世界中！

![Townscaper - Gameplay Trailer ▻ NEW GAMES 2020 - YouTube](README.assets/maxresdefault.jpg)

![image-20200702101633320](README.assets/image-20200702101633320.png)

![RE:【討論】科學之進擊！如果你是活在巨人威脅下的達文西，你會想出 ...](README.assets/9dc75df9bc660e08655b9681a5c15e06.JPG)

![image-20200704012121965](README.assets/image-20200704012121965.png)

![图像](README.assets/D8wI1ReVsAAvKNC.jpg)

![img](README.assets/0271e1ad9192fe09924e616925b35290.jpg)

![征選遊：進擊巨人小鎮Nordlingen｜即時新聞｜生活｜on.cc東網](README.assets/OSU-170519-10526-802-M.jpg)

这个世界是一个小小的城市（城市结构暂定参考游戏《Townscaper》，同样也是一款PCG游戏），城市的主要建筑物（或者墙，参考进击的巨人）正是画中的线条（使用 Image tracing / Polygon tracing 生成 Mesh），街道从主要建筑物旁延伸出来（L-system 或者 Subdivision），旁边是小型建筑。建筑、马路、绿植、引力核心的位置，模型的生成，以及它们的纹理等，为PCG场景/模型/纹理的核心，场景生成之后不再做改动。镜头逐渐拉近到玩家控制的主角——Dot身上。正如其名，Dot就是一个可爱的球体，它是城市的居民，同时它也是驱动整个城市的智慧单元。整个城市有很多很多这些球体（Dots），正像电子（或是人身体的细胞）一样，他们负责运营整个城市，当然也驱动起了我们的移动设备（建立城市->电路板的连接，强调meta的游戏属性）。

整个城市在一开始并不繁华，为一个纯粹的钢筋（可以导电！）水泥城市，并且没有任何的装饰，看起来甚至就像一个电路。这些dots依靠他们对这个世界的“引力”穿越整个城市（引力可以理解为电子和电子之间的引/斥力，这里dots即代表电子，这样引力的设定才会比较自恰），解锁并点亮城市的各个角落（此处属于核心玩法，见下一章。使用Top-down视角）。被解锁的城市区域恢复了颜色和生机（颜色盘这里随机一套，生机通过恢复建筑物纹理，摆放植被等方法，最终能够达到一个类似epic zen garden的画风，植被甚至可以靠近荒野乱斗）

![img](README.assets/Store_EpicZenGarden_01 (1)-1920x1080-82bc3c22e0437cd6a7acb7b9d7be92d7.png)

当它们点亮了全部地区，来到最中央的广场（万花筒图案中间肯定总是一个“广场”）时，整个城市的一次计算任务完成（指的就是点击“涂色”按钮以后的计算任务），同时城市也被点亮的五彩缤纷（此处同样指的是涂色，只不过这里的城市同样也被涂上了颜色，体现一语双关的meta感）。dot在城市的中央广场，此时镜头缓缓推远展现整个城市场景，直到和之前涂色App的视角类似，此时涂色App的UI出现，呈现给涂色App用户（其实就是玩家，这里还是一层meta）的就是刚刚玩家涂色完毕的整个城市场景，制造出一种极致的美感。至此游戏结束。整个游戏玩下来应该不会超过10分钟。



## 核心玩法

### 点亮城市



### 引力



### 线条 -> 电路板 -> 城市 -> 电子，由宏至微



## 艺术风格参考



## 设想的 PCG 技术要点

### 图案的绘制

### Ring2Stripe

将bitmap变换子区域到strip空间算路，算完之后把mesh变换回ring空间。

### Marching Square / Cube using Scalar Fields

http://thebigsmall.uk/uncategorized/part-1-contour-maps/

https://www.youtube.com/watch?v=yOgIncKp0BE

用来生成城墙mesh。

### Flood Fill

算中心，隔离出子区域范围。

### minimum spanning tree (*MST*)

算子区域联通路径，同时挖空bitmap。

### Urban Road using L-system

子区域算路，Agent-based solution。

### SDF Contour

子区域美化

### Wave Function Collapse (WFC)

https://www.youtube.com/watch?v=0bcZb-SsnrA

https://zhuanlan.zhihu.com/p/66416593

波函数坍缩用来做连续楼房的布局（可选，有时间做）

### Procedural Texture



### "Stub" Generation

用来提供gameplay机制。