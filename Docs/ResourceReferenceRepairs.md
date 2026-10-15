# 资源引用处理记录

## 处理规则与结果

按用户更正后的规则：Git 确认在目录迁移提交中删除的资源，将其引用置空；其他具有明确目标和类型依据的引用重接到当前资源。

- 共修改 **13 个场景、预制体或字体资源文件**，处理 **55 处引用**。
- **置空 27 处**：六个被迁移提交删除的图片资源，统一写为 `{fileID: 0}`。这些图片未恢复，置空位置不再计入缺失 GUID。
- **重接 28 处**：10 处悬停音效、16 处选项预制体、1 处夏伦配置、1 处字体 Shader。
- 剩余项目缺失引用 **155 → 100 处**，缺失 GUID **81 → 71 个**；构建场景静态依赖可达 **13 → 6 处**。
- 当前剩余清单：[MissingResourceReferencesAudit.md](MissingResourceReferencesAudit.md)；本次逐项明细：[RepairedResourceReferences.csv](RepairedResourceReferences.csv)。

## 迁移删除资源：置空

已使用 Git `--no-renames --name-status` 确认提交 `becba6ee09854bc84f67cfbf5748d2c2b8b6bc0a`（2026-10-13，资源大迁移）同时删除以下原资源及 `.meta`；GUID 与 `.meta` 历史一致。

| 原资源路径 | 原 GUID | 置空数 |
| --- | --- | --- |
| `Assets/美术素材/立绘/陈玉澍-默认.png` | `cd1189b0d6646b94fb9cc8de0a6fdf69` | 4 |
| `Assets/美术素材/立绘/夏伦-默认.png` | `4577b6d984551544aa5bb708292a3f7e` | 4 |
| `Assets/美术素材/立绘/姜韶平-默认.PNG` | `9c1527316ce785840a4b23c586a57e1d` | 1 |
| `Assets/美术素材/立绘/姜韶平-坐-全身.png` | `1fde07785bb895e4a812d51dfae213a6` | 9 |
| `Assets/美术素材/背景/场景-走廊.JPG` | `b18454ebdc31ce84eade019fd3eb977e` | 4 |
| `Assets/美术素材/场景/场景-化妆间.JPG` | `5033667fd3c77784ba6635de972afc1d` | 5 |

## 现有资源：重接

| 字段 | 数量 | 旧 GUID | 新 GUID / 目标 | 依据 |
| --- | --- | --- | --- | --- |
| `_customHoverClip` | 10 | `3ccf02978e0714ca79f0a1b8010d103f` | `cb4e3a71b727cbe488eccbb558b59915` / `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` | Start.unity:3035 的 SoundsManager._hoverSfxClip 已使用此音效；保留 UISoundPlayer 的显式悬停播放条件。 |
| `choicePrefab` | 16 | `78bf832dcaaf5d7429424b2e31c91db0` | `1e086c748b1f8cf458c6c5cf9d728f1a` / `Assets/Systems/Dialog/Prefabs/Button.prefab` | 根 GameObject fileID 3063205307973740173 一致，包含 Button 和子 TMP 文本；DIalogSystem.prefab:5907 已使用此预制体。预制体自身仍有 1 处缺图，保留在剩余清单。 |
| `m_Shader` | 1 | `f07e8aed33e642142b0df8b7be6e095d` | `68e6db2ebdc24f95958faec2be5558d6` / `Assets/TextMesh Pro/Shaders/TMP_SDF.shader` | W3 的 SDF 图集材质具有 TMP Distance Field 对应属性；目标为项目现有 TextMeshPro/Distance Field Shader，fileID 4800000。 |
| `affectionConfig` | 1 | `5ea4f8091d2a65a49a817d8cbc6729eb` | `9b7829b855bd9264a9bcaabf01914ca9` / `Assets/Data/NPCdata/夏伦.asset` | 目标是现有唯一 characterName 为夏伦且脚本为 AffectionConfig 的配置，主资源 fileID 11400000。 |

夏伦配置的 `levels` 和 `specialEvents` 当前为空，本次仅重接引用，未补充业务配置。

## 剩余项目的评估

| 类别 | 剩余数 | 暂未继续处理的原因 |
| --- | --- | --- |
| 脚本 | 33 | 本地 Git 历史没有 GUID 定义；仅凭对象名替换可能挂错组件，无法确认原逻辑和序列化字段。 |
| Tilemap 资源 | 28 | 缺少原始图块资产和历史 GUID 定义，无法确认每个单元对应的 Tile。 |
| 对话文本 | 17 | 原 CSV 对应哪段剧情缺少证据，同场景内存在多段不同剧情引用。 |
| 图片（含预制体覆盖） | 18 | 没有可靠的图像或历史路径对应关系；其中 5 处覆盖的目标组件 fileID 本身也不存在。 |
| 动画控制器 | 2 | 无法确认原动画状态机及其动作对应关系。 |
| 后备字体 | 1 | 无法确定预期字体及其字形覆盖范围。 |
| 材质纹理 | 1 | 缺少原纹理用途及目标资源依据。 |

包内另有 7 处未解析的实际序列化引用，详见 [PackageMissingResourceReferences.csv](PackageMissingResourceReferences.csv)。

## 静态验证

- 全量重扫 Assets、Packages、ProjectSettings 和当前锁定包，55 条原丢失记录均已按规则处理，10 个旧 GUID 不再被项目引用。
- 对 13 个修改文件与修复前快照逐行比对，仅发现计划内 55 行替换；28 处重接均解析到存在的资源，主对象 fileID 或导入器类型匹配。
- 用户已有修改的四个文件 SHA-256 均保持一致；未发现重复 GUID、孤立 `.meta`、无效 `.meta`。
- 未启动 Unity，未做导入、运行时或视觉验证。动态 Resources 路径及所有有效 GUID 下的子资源 fileID 不属于本次全量扫描范围。

## Unity 中待验证

- 打开 SampleScene、Game、Move、old、SampleScene 副本及参考场景，检查本报告置空位置的 Sprite 字段为 None；原删除图片不会再由这些字段提供。SampleScene 的 Player 也在置空清单内。
- 从正式入口进入地图及打工列表，鼠标悬停应播放当前统一的悬停音效。
- 检查 Dialog 场景中的 choicePrefab 指向现有 Button 预制体；在有效对话数据上验证选项生成和点击。Dialog 场景本身仍有其他缺失脚本及剧情文本，详见剩余清单。
- 检查 W3 SDF 的 Atlas Material 使用 TextMeshPro/Distance Field，字体不因 Shader 缺失而变粉；Game 场景中夏伦的 affectionConfig 应指向夏伦配置。

## 本次全部处理位置

| 位置 | 字段 | 方式 | 原 GUID | 结果 |
| --- | --- | --- | --- | --- |
| [Assets/预制件/地图.prefab:1497](../Assets/%E9%A2%84%E5%88%B6%E4%BB%B6/%E5%9C%B0%E5%9B%BE.prefab#L1497) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/预制件/地图.prefab:3052](../Assets/%E9%A2%84%E5%88%B6%E4%BB%B6/%E5%9C%B0%E5%9B%BE.prefab#L3052) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/预制件/地图.prefab:3612](../Assets/%E9%A2%84%E5%88%B6%E4%BB%B6/%E5%9C%B0%E5%9B%BE.prefab#L3612) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/预制件/地图.prefab:4181](../Assets/%E9%A2%84%E5%88%B6%E4%BB%B6/%E5%9C%B0%E5%9B%BE.prefab#L4181) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/预制件/BackgroundRoot.prefab:445](../Assets/%E9%A2%84%E5%88%B6%E4%BB%B6/BackgroundRoot.prefab#L445) | `m_Sprite` | 置空 | `5033667fd3c77784ba6635de972afc1d` | `{fileID: 0}` |
| [Assets/prefab/地图.prefab:1451](../Assets/prefab/%E5%9C%B0%E5%9B%BE.prefab#L1451) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/prefab/地图.prefab:2987](../Assets/prefab/%E5%9C%B0%E5%9B%BE.prefab#L2987) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/prefab/地图.prefab:4031](../Assets/prefab/%E5%9C%B0%E5%9B%BE.prefab#L4031) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/prefab/地图.prefab:4411](../Assets/prefab/%E5%9C%B0%E5%9B%BE.prefab#L4411) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/prefab/CharJobListItem.prefab:558](../Assets/prefab/CharJobListItem.prefab#L558) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/prefab/JobListItem.prefab:311](../Assets/prefab/JobListItem.prefab#L311) | `_customHoverClip` | 重接 | `3ccf02978e0714ca79f0a1b8010d103f` | `Assets/Resources/Audio/SFX/鼠标放置按键音效.mp3` |
| [Assets/Scenes/参考.unity:622](../Assets/Scenes/%E5%8F%82%E8%80%83.unity#L622) | `m_Sprite` | 置空 | `cd1189b0d6646b94fb9cc8de0a6fdf69` | `{fileID: 0}` |
| [Assets/Scenes/参考.unity:4322](../Assets/Scenes/%E5%8F%82%E8%80%83.unity#L4322) | `m_Sprite` | 置空 | `5033667fd3c77784ba6635de972afc1d` | `{fileID: 0}` |
| [Assets/Scenes/参考.unity:14531](../Assets/Scenes/%E5%8F%82%E8%80%83.unity#L14531) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Scenes/参考.unity:35417](../Assets/Scenes/%E5%8F%82%E8%80%83.unity#L35417) | `m_Sprite` | 置空 | `b18454ebdc31ce84eade019fd3eb977e` | `{fileID: 0}` |
| [Assets/Scenes/参考.unity:42136](../Assets/Scenes/%E5%8F%82%E8%80%83.unity#L42136) | `m_Sprite` | 置空 | `4577b6d984551544aa5bb708292a3f7e` | `{fileID: 0}` |
| [Assets/Scenes/参考.unity:47618](../Assets/Scenes/%E5%8F%82%E8%80%83.unity#L47618) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Scenes/Game.unity:516](../Assets/Scenes/Game.unity#L516) | `m_Sprite` | 置空 | `cd1189b0d6646b94fb9cc8de0a6fdf69` | `{fileID: 0}` |
| [Assets/Scenes/Game.unity:6630](../Assets/Scenes/Game.unity#L6630) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Scenes/Game.unity:16317](../Assets/Scenes/Game.unity#L16317) | `m_Sprite` | 置空 | `b18454ebdc31ce84eade019fd3eb977e` | `{fileID: 0}` |
| [Assets/Scenes/Game.unity:17603](../Assets/Scenes/Game.unity#L17603) | `m_Sprite` | 置空 | `5033667fd3c77784ba6635de972afc1d` | `{fileID: 0}` |
| [Assets/Scenes/Game.unity:19884](../Assets/Scenes/Game.unity#L19884) | `affectionConfig` | 重接 | `5ea4f8091d2a65a49a817d8cbc6729eb` | `Assets/Data/NPCdata/夏伦.asset` |
| [Assets/Scenes/Game.unity:19929](../Assets/Scenes/Game.unity#L19929) | `m_Sprite` | 置空 | `4577b6d984551544aa5bb708292a3f7e` | `{fileID: 0}` |
| [Assets/Scenes/Game.unity:21779](../Assets/Scenes/Game.unity#L21779) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Scenes/Move.unity:733](../Assets/Scenes/Move.unity#L733) | `m_Sprite` | 置空 | `9c1527316ce785840a4b23c586a57e1d` | `{fileID: 0}` |
| [Assets/Scenes/Move.unity:1549](../Assets/Scenes/Move.unity#L1549) | `m_Sprite` | 置空 | `b18454ebdc31ce84eade019fd3eb977e` | `{fileID: 0}` |
| [Assets/Scenes/old.unity:639](../Assets/Scenes/old.unity#L639) | `m_Sprite` | 置空 | `cd1189b0d6646b94fb9cc8de0a6fdf69` | `{fileID: 0}` |
| [Assets/Scenes/old.unity:4054](../Assets/Scenes/old.unity#L4054) | `m_Sprite` | 置空 | `5033667fd3c77784ba6635de972afc1d` | `{fileID: 0}` |
| [Assets/Scenes/old.unity:13780](../Assets/Scenes/old.unity#L13780) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Scenes/old.unity:34226](../Assets/Scenes/old.unity#L34226) | `m_Sprite` | 置空 | `b18454ebdc31ce84eade019fd3eb977e` | `{fileID: 0}` |
| [Assets/Scenes/old.unity:41227](../Assets/Scenes/old.unity#L41227) | `m_Sprite` | 置空 | `4577b6d984551544aa5bb708292a3f7e` | `{fileID: 0}` |
| [Assets/Scenes/old.unity:46964](../Assets/Scenes/old.unity#L46964) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Scenes/SampleScene - 副本.unity:619](../Assets/Scenes/SampleScene%20-%20%E5%89%AF%E6%9C%AC.unity#L619) | `m_Sprite` | 置空 | `cd1189b0d6646b94fb9cc8de0a6fdf69` | `{fileID: 0}` |
| [Assets/Scenes/SampleScene - 副本.unity:11671](../Assets/Scenes/SampleScene%20-%20%E5%89%AF%E6%9C%AC.unity#L11671) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Scenes/SampleScene - 副本.unity:23647](../Assets/Scenes/SampleScene%20-%20%E5%89%AF%E6%9C%AC.unity#L23647) | `m_Sprite` | 置空 | `5033667fd3c77784ba6635de972afc1d` | `{fileID: 0}` |
| [Assets/Scenes/SampleScene - 副本.unity:33902](../Assets/Scenes/SampleScene%20-%20%E5%89%AF%E6%9C%AC.unity#L33902) | `m_Sprite` | 置空 | `4577b6d984551544aa5bb708292a3f7e` | `{fileID: 0}` |
| [Assets/Scenes/SampleScene - 副本.unity:37114](../Assets/Scenes/SampleScene%20-%20%E5%89%AF%E6%9C%AC.unity#L37114) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Scenes/SampleScene.unity:8459](../Assets/Scenes/SampleScene.unity#L8459) | `m_Sprite` | 置空 | `1fde07785bb895e4a812d51dfae213a6` | `{fileID: 0}` |
| [Assets/Systems/Dialog/Dialog.unity:1809](../Assets/Systems/Dialog/Dialog.unity#L1809) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:1832](../Assets/Systems/Dialog/Dialog.unity#L1832) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:1855](../Assets/Systems/Dialog/Dialog.unity#L1855) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:1878](../Assets/Systems/Dialog/Dialog.unity#L1878) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:1901](../Assets/Systems/Dialog/Dialog.unity#L1901) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:1924](../Assets/Systems/Dialog/Dialog.unity#L1924) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:1947](../Assets/Systems/Dialog/Dialog.unity#L1947) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:1970](../Assets/Systems/Dialog/Dialog.unity#L1970) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:1993](../Assets/Systems/Dialog/Dialog.unity#L1993) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:2016](../Assets/Systems/Dialog/Dialog.unity#L2016) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:2039](../Assets/Systems/Dialog/Dialog.unity#L2039) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:2062](../Assets/Systems/Dialog/Dialog.unity#L2062) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:2085](../Assets/Systems/Dialog/Dialog.unity#L2085) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:2108](../Assets/Systems/Dialog/Dialog.unity#L2108) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:2131](../Assets/Systems/Dialog/Dialog.unity#L2131) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/Dialog.unity:2154](../Assets/Systems/Dialog/Dialog.unity#L2154) | `choicePrefab` | 重接 | `78bf832dcaaf5d7429424b2e31c91db0` | `Assets/Systems/Dialog/Prefabs/Button.prefab` |
| [Assets/Systems/Dialog/W3 SDF.asset:211](../Assets/Systems/Dialog/W3%20SDF.asset#L211) | `m_Shader` | 重接 | `f07e8aed33e642142b0df8b7be6e095d` | `Assets/TextMesh Pro/Shaders/TMP_SDF.shader` |
