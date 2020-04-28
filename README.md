# 水银涩图机~
## 程序使用的环境
基于[Jie2GG](https://github.com/Jie2GG)巨好用的酷Q C# SDK [Native.FarmeWork](https://github.com/Jie2GG/Native.Framework) v4.2.0.0424版本开发，依托于[酷Q](https://cqp.cc/)运行
## 接口介绍
使用了大佬的[Lolicon API](https://api.lolicon.app/#/setu)涩图接口
这个接口提供的这些图~~很戳我的xp~~我很喜欢，就写了个机器人
## 功能介绍
- [x] 接入[Lolicon Api](https://api.lolicon.app/#/setu)拉取图片
- [x] 分群控制
- [x] 限制调用次数
- [x] 每日自动重置次数
- [x] 透过本地代理(socks5代理测试可通)
- [x] 使用Material Design绘制UI，挺好看
- [ ] 自定义指令与回答
- [x] 完善的异常~~处理~~报告机制（由于接口位于墙外，高峰期拉取图片失败率过高，当然也有图太涩被tx吞掉的原因）
- [ ] 接入[Pixiv Api](https://api.imjad.cn/pixiv_v2.md)用于实现搜图与获取日榜周榜
- [ ] 接入[SauceNAO](https://saucenao.com)实现以图搜图
- [ ] 接入[trace moe](https://trace.moe/)实现以图搜番
- [ ] 通过语义分析来自行填充搜图参数<sup>[1]</sup>

<font color=#ff0000>[1]</font>例如:收到指令:水银来一份德丽莎的R18涩图
自行解析为:keyword=德丽莎&r18=true

## 其他的话
在我瞎逛论坛的时候发现了和这个项目使用同一接口的[应用](https://cqp.cc/t/48770)，瞬间就失去了将插件发布到社区的欲望，或许以后功能写完了会发布吧……权当自己瞎写着玩了
最近差不多热情消磨的差不多了，真就龟速开发了，如果有大佬想赞助支持我的话，在崩坏三抽卡机项目的关于里有赞助码，拜谢
## 项目代码如何使用
酷Q开启开发模式，参考[酷Q文库](https://docs.cqp.im/dev/v9/devmode/)的方法开启<br>
Visual Studio的最低版本为2019<br>
clone下载之后，设置Native.Core项目的生成目录为 ...\CQP-xiaoi\酷Q Pro\dev<br>
### 具体方法为:
>1.右击Native.Core项目，点击属性<br>
![说明图片3](https://i.loli.net/2020/03/21/PlNBCAHV1JWmLsO.png)<br>
2.左侧点击生成一栏，设置输出路径<br>
![说明图片4](https://i.loli.net/2020/03/21/mtCeRTWDHAh2Irg.png)<br>
点击生成-重新生成解决方案，之后酷Q重载应用即可