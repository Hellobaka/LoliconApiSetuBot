# 水银涩图机~
## 功能介绍
- [x] 接入[Lolicon Api](https://api.lolicon.app/#/setu)拉取图片
- [x] 分群控制
- [x] 限制调用次数
- [x] 每日自动重置次数
- [x] 透过本地代理(socks5代理测试可通)
- [x] 使用Material Design绘制UI，挺好看
- [x] 自定义指令与回答
- [x] R18图自定义时长撤回
- [x] 接入[SauceNAO](https://saucenao.com)实现以图搜图
- [x] 接入[trace moe](https://trace.moe/)实现以图搜番

## 指令
- Lolicon拉取：#setu[关键字|r18] 示例：#setu、#setu初音、#setu 初音、#setu r18、#setu初音r18
- 清除限制：#clear 仅限配置内的管理员
- pid拉取：#pid pid 示例：#pid 1234678、#pid12345678
- SauceNao搜图：#nao
- TraceMoe搜番：#trace

## 配置说明
- 路径：数据目录\me.cqp.luohuaming.Setu\Config.json
- 配置并非修改后立即生效 重载插件后生效
- 删除此配置后重载插件可重新生成一份

|键|默认值|含义|
| :----| :---- | :---- |
|MaxGroupQuota|50|群每日最多使用次数|
|MaxPersonQuota|10|群每日单人最多使用次数|
|R18|False|限制R18图片发送|
|R18_PicRevoke|False|若开启R18图 是否自动撤回|
|R18_RevokeTime|60000|自动撤回延时 单位ms|
|ProxyEnabled|False|是否使用HTTP代理|
|ProxyURL||HTTP代理地址|
|ProxyUserName||代理认证的用户名|
|ProxyPassword||代理认证的密码|
|PassSNI|True|Pixiv免代理直连|
|SNI_IPAddress||手动设置SNI地址|
|PixivCookie||默认使用的Pixiv的Cookie 非必要|
|PixivUA|Mozilla/5.0 (Windows NT 10.0; Win64; x64)|UA|
|AdminList||管理员列表 使用`|`分割|
|GroupList||开启的群列表 使用`|`分割|
|StartResponse|拉取图片中~至少需要15s……\n你今日剩余调用次数为\<count\>次(￣▽￣)|指令成功触发时发送的消息|
|MaxMemberResoponse|你当日所能调用的次数已达上限(￣▽￣)|个人单日调用达到上限|
|MaxGroupResoponse|本群当日所能调用的次数已达上限(￣▽￣)|群单日调用次数达到上限|
|PicNotFoundResoponse|哦淦 老兄你的xp好机八小众啊 找不到啊|使用关键字拉取图片时 找不到时触发的回复|
|SuccessResponse|title: \<title\>\nauthor: \<author\>\np: \<p\>\npid: \<pid\>|lolicon pid发送的图片简介|
|LoliconPicOrder|#setu|Lolicon触发指令|
|ClearLimitOrder|#clear|清除群限制触发指令|
|PIDSearchOrder|#pid|PID搜图触发指令|
|SauceNaoSearchOrder|#nao|SauceNao搜图触发指令|
|TraceMoeSearchOrder|#trace|TraceMoe搜番触发指令|