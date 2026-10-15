# 资源引用丢失检查报告

检查时间：2026-10-15 23:52:57（Asia/Shanghai）。Git HEAD：`ac832dcd4cec9c8a716ce7a28b35b14be132dce7`。

## 1. 结果

- 项目自身：**100 处引用**无法解析目标 GUID，涉及 **71 个 GUID、14 个文件**。
- 已安装包另有 **7 处序列化引用**无法解析，单独列在第 6 节，不计入项目自身的统计。
- 当前正式构建场景为 `Assets/Scenes/Start.unity`、`Assets/Scenes/SampleScene.unity`；从它们沿序列化 GUID 依赖追踪，可达 **6 处**项目失效引用。
- 修复前为 155 处、81 个 GUID；本次按用户规则置空 27 处迁移删除资源引用，并重接 28 处现有资源引用。详见 [修复报告](ResourceReferenceRepairs.md)。
- 未发现重复 GUID、孤立 `.meta`、格式无效的 `.meta` 或无法扫描的 Unity 二进制序列化文件；65 个锁定依赖包均在本地找到。
- 当前清单只包含处理后仍待定位的引用；主动置空的字段不代表原资源已经恢复。

### 扫描依据与范围

未找到已有的完整可查询 GUID 清单。`Library/SourceAssetDB`、`Library/ArtifactDB` 是 Unity 的二进制数据库，本次没有用它们充当资源存在的依据。已临时生成 [guid-index.csv](../Library/ReferenceAudit/guid-index.csv)，包含 **8284 个 GUID**（项目 `.meta` 1766 个，当前已安装包 `.meta` 6518 个），每条都有资源路径、`.meta` 路径和资源是否实际存在。

扫描 `Assets`、`Packages`、`ProjectSettings` 及当前锁定依赖对应的 `Library/PackageCache`。项目文本文件 2302 个，包文本文件 11382 个；检查项目 GUID 引用 20340 处。`.meta` 自身 GUID 定义不作为引用；包中 Unity 不导入的 `*~` 目录不参与可解析资源索引。排除全零 GUID、Unity 内置资源 GUID，以及 `productGUID` / `clonedFromGUID` 这两个项目标识。

**判定含义：**“丢失”表示引用目标的 GUID 在当前项目和已安装包中没有可解析的资源。即便磁盘上有同名图片，只要 GUID 不同，旧引用仍然失效。当前项目清单中的 100 处均属于“没有对应 GUID 定义”。

**检查边界：**本次核查持久化的 GUID 引用，不覆盖 `Resources.Load` 字符串、CSV 中的资源名称、运行时拼接路径，以及目标 GUID 有效但子资源 `fileID` 失效的全部情形。仅对缺失引用涉及的预制体覆盖项额外记录目标组件是否存在。未启动 Unity、未做运行时或视觉验证；静态依赖可达并不等同于运行中一定会访问该字段。

### 交付文件

- [MissingResourceReferences.csv](MissingResourceReferences.csv)：100 处剩余项目引用，逐项记录路径、行列、字段、对象、GUID、fileID、源行及构建场景可达情况。
- [MissingResourceGuids.csv](MissingResourceGuids.csv)：按 71 个剩余缺失 GUID 汇总所有引用位置。
- [PackageMissingResourceReferences.csv](PackageMissingResourceReferences.csv)：7 处包内序列化引用。
- [RepairedResourceReferences.csv](RepairedResourceReferences.csv)：本次全部 55 处处理位置及依据。
- `Library/ReferenceAudit/`：临时索引、扫描脚本、完整机器可读结果和历史追踪数据；该目录受现有 `.gitignore` 排除，清理 Library 后会消失。

首次扫描的 155 处结果已使用 PowerShell 和 `rg` 交叉核对；本次另按修复前快照逐项验证处理字段，并重新扫描剩余引用。

## 2. 构建场景及其静态依赖中的引用

各构建场景沿序列化 GUID 依赖追踪所得的剩余引用如下，共 6 处（修复前 13 处）。此统计包含预制体、材质及遗留覆盖记录。具体来源场景保留在 CSV 的 buildScenes 列。

| 位置 | 字段 / 类型 | 对象或目标 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| [Assets/Scenes/SampleScene.unity:13118](../Assets/Scenes/SampleScene.unity#L13118) | `objectReference -> m_Sprite` / 图片（预制体覆盖） | PrefabInstance &959929500; missing target &7132004763855946177 | `db76f0e32cdb5b542876fca54976ca9b` | 覆盖目标组件也不存在，属遗留覆盖项 |
| [Assets/Scenes/SampleScene.unity:29904](../Assets/Scenes/SampleScene.unity#L29904) | `objectReference -> m_Sprite` / 图片（预制体覆盖） | reference / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs | `1dddc16e979bf35439fe91c51a5f631f` |  |
| [Assets/Systems/Dialog/Prefabs/Button.prefab:221](../Assets/Systems/Dialog/Prefabs/Button.prefab#L221) | `m_Sprite` / 图片 | Button / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs | `b6139c90e288ee0478c68e472158ad4d` |  |
| [Assets/Systems/Dialog/Prefabs/DIalogSystem.prefab:1190](../Assets/Systems/Dialog/Prefabs/DIalogSystem.prefab#L1190) | `m_Sprite` / 图片 | PicR / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs | `2903e230de96bee44ac271c7dc65dea4` |  |
| [Assets/Systems/Dialog/Prefabs/DIalogSystem.prefab:5890](../Assets/Systems/Dialog/Prefabs/DIalogSystem.prefab#L5890) | `dialogFile` / 对话文本 | Dialog / Assets/Systems/Dialog/DialogVisual.cs | `2c7b43740c75d4c4dab4e64b284f72d0` |  |
| [Assets/Systems/Dialog/Shaders/ShaderHolder.mat:60](../Assets/Systems/Dialog/Shaders/ShaderHolder.mat#L60) | `m_Texture` / 纹理 | ShaderHolder | `adc9925db40c9a043bc9758bb2555181` |  |

其中 Player 的迁移删除图片已按规则置空；遗留预制体覆盖仍保留在待定位清单中。

## 3. 资源迁移相关的已确认记录

迁移提交 `becba6e` 删除的六个资源涉及 27 处引用，已按用户规则全部置为 `{fileID: 0}`，具体旧路径和位置见 [修复报告](ResourceReferenceRepairs.md)。其余仍有历史记录的剩余 GUID 如下：

| 旧 GUID | 引用数 | 历史资源路径 | 当前同名候选（新 GUID） |
| --- | --- | --- | --- |

剩余 71 个 GUID 在当前本地所有 Git refs 的 `.meta` 变更历史中未找到定义，无法据此还原原始资源名。

## 4. 按文件汇总

| 文件 | 失效引用数 | 不同 GUID 数 | 从构建场景静态可达 |
| --- | --- | --- | --- |
| `Assets/Fonts/FZHTJW.TTF.meta` | 1 | 1 | 未由该依赖图到达 |
| `Assets/prefab/map.prefab` | 28 | 28 | 未由该依赖图到达 |
| `Assets/Scenes/Game.unity` | 6 | 5 | 未由该依赖图到达 |
| `Assets/Scenes/old.unity` | 4 | 4 | 未由该依赖图到达 |
| `Assets/Scenes/SampleScene - 副本.unity` | 7 | 7 | 未由该依赖图到达 |
| `Assets/Scenes/SampleScene.unity` | 2 | 2 | 是 |
| `Assets/Scenes/workscene.unity` | 8 | 6 | 未由该依赖图到达 |
| `Assets/Scenes/参考.unity` | 4 | 4 | 未由该依赖图到达 |
| `Assets/Systems/Dialog/Dialog.unity` | 34 | 25 | 未由该依赖图到达 |
| `Assets/Systems/Dialog/DialogDemo.unity` | 1 | 1 | 未由该依赖图到达 |
| `Assets/Systems/Dialog/Prefabs/Button.prefab` | 1 | 1 | 是 |
| `Assets/Systems/Dialog/Prefabs/DIalogSystem.prefab` | 2 | 2 | 是 |
| `Assets/Systems/Dialog/Shaders/ShaderHolder.mat` | 1 | 1 | 是 |
| `Assets/预制件/BackgroundRoot.prefab` | 1 | 1 | 未由该依赖图到达 |

类型汇总：后备字体 1 处、Tilemap 资源 28 处、脚本 33 处、图片（预制体覆盖） 6 处、图片 12 处、动画控制器 2 处、对话文本 17 处、纹理 1 处。

## 5. 全部项目引用明细

每行对应一个实际序列化引用位置。`&数字` 为 Unity YAML 文档对象 ID；预制体覆盖的真实属性见 `objectReference -> 属性名`。完整源行及覆盖目标信息保留在 CSV 中。

### `Assets/Fonts/FZHTJW.TTF.meta`（1 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 14 | `fallbackFontReferences` | 字体导入器 | `3c647a71c6bb0ea45a9493d1aca2f1f9` |  |

### `Assets/prefab/map.prefab`（28 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 114 | `m_Data` | collision [&6951297841984934933] | `360ea6a981309b14787ecfff37ddbe1d` |  |
| 116 | `m_Data` | collision [&6951297841984934933] | `970a65937d3658b4bb263857778d9174` |  |
| 119 | `m_Data` | collision [&6951297841984934933] | `f099dfa081f5e7b4590ba79b7ccf4b17` |  |
| 121 | `m_Data` | collision [&6951297841984934933] | `c662f6a446604e248ab83050ee518fcc` |  |
| 1362 | `m_Data` | ground [&3252424339869132301] | `d784575ffcf080c4ea522cb88ea28b5e` |  |
| 1364 | `m_Data` | ground [&3252424339869132301] | `82c310b156722d843b040b26907dc80c` |  |
| 1366 | `m_Data` | ground [&3252424339869132301] | `b6daf192f2f618e41aced53eb32aa1de` |  |
| 1368 | `m_Data` | ground [&3252424339869132301] | `d019aee4e4dfc3649b453e07bc4c9788` |  |
| 1370 | `m_Data` | ground [&3252424339869132301] | `e88523a61dffa654989fcd56b4af9ae1` |  |
| 1372 | `m_Data` | ground [&3252424339869132301] | `05cd4f87380532d489a17ed4be8cc3a9` |  |
| 1374 | `m_Data` | ground [&3252424339869132301] | `a14a025bc4e149c43b1f3f19bb02b227` |  |
| 1376 | `m_Data` | ground [&3252424339869132301] | `841b7c3592eb15045a2d9fd7e796144a` |  |
| 1378 | `m_Data` | ground [&3252424339869132301] | `980ff9d0ec1eaf547a5170bec0d376ed` |  |
| 1380 | `m_Data` | ground [&3252424339869132301] | `efac4283533e9004d8e25f8391acde2a` |  |
| 1382 | `m_Data` | ground [&3252424339869132301] | `426e8251ba8f13643b5cf28abdd24bdb` |  |
| 1384 | `m_Data` | ground [&3252424339869132301] | `b3dd8ac3b9c496b42b610a72dfa5fd9e` |  |
| 1387 | `m_Data` | ground [&3252424339869132301] | `f7f361d92fa1c484e8bbb49a0a572aee` |  |
| 1389 | `m_Data` | ground [&3252424339869132301] | `afc090c4b3c5ed549b644d657cd30d6f` |  |
| 1391 | `m_Data` | ground [&3252424339869132301] | `61051da239e6ff24dbb3d3f481dbf981` |  |
| 1393 | `m_Data` | ground [&3252424339869132301] | `578a2b3f64b684a4888f49d5c631d1fb` |  |
| 1395 | `m_Data` | ground [&3252424339869132301] | `3a96958c6b58e2143b5dbc0c938f9463` |  |
| 1397 | `m_Data` | ground [&3252424339869132301] | `9aef0184e75b5d34da0fa45c6d8103fd` |  |
| 1399 | `m_Data` | ground [&3252424339869132301] | `9ddef38cc5cb60945923f8d5b86f1b31` |  |
| 1401 | `m_Data` | ground [&3252424339869132301] | `c565104fe94b3944493e7ced82d6afb6` |  |
| 1403 | `m_Data` | ground [&3252424339869132301] | `3bd9211c6547de14dacb46bac825da3c` |  |
| 1405 | `m_Data` | ground [&3252424339869132301] | `92fc6ffcb97bff74caa2f3a8ddc3069c` |  |
| 1407 | `m_Data` | ground [&3252424339869132301] | `55e7fd89729fed842a784c04d7e22c13` |  |
| 1409 | `m_Data` | ground [&3252424339869132301] | `001d07bb309b3924a8f94427301afe57` |  |

### `Assets/Scenes/Game.unity`（6 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 5834 | `m_Script` | WorkManager [&524418655] | `a7dab163589df8c4fa4aedb16b237c6c` |  |
| 7800 | `m_Script` | CharJobListContainer [&782296853] | `90da3b8aaa282644babc502e0a65b498` |  |
| 20084 | `m_Script` | 送礼按钮 [&1833163932] | `c1afe5a57ea2f714da534c2becfd1546` |  |
| 21266 | `m_Script` | ConfigManger [&1931732911] | `4ef9925c9f21e6141b7c90684ffca1cc` |  |
| 21310 | `m_Script` | ConfingManager [&1932012069] | `4ef9925c9f21e6141b7c90684ffca1cc` |  |
| 22224 | `m_Script` | UIManger [&2039454362] | `a0971dd2302303d4cae7a09e28591762` |  |

### `Assets/Scenes/old.unity`（4 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 6622 | `m_Script` | ConfigManger [&285230365] | `4ef9925c9f21e6141b7c90684ffca1cc` |  |
| 8151 | `m_Script` | 送礼按钮 [&395170675] | `c1afe5a57ea2f714da534c2becfd1546` |  |
| 22048 | `objectReference -> m_Sprite` | PrefabInstance &959929500; missing target &7132004763855946177 [&959929500] | `db76f0e32cdb5b542876fca54976ca9b` | 覆盖目标组件 fileID 也已不存在 |
| 49286 | `m_Script` | WorkManager [&2119034645] | `a7dab163589df8c4fa4aedb16b237c6c` |  |

### `Assets/Scenes/SampleScene - 副本.unity`（7 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 6176 | `m_Script` | ConfigManger [&285230365] | `4ef9925c9f21e6141b7c90684ffca1cc` |  |
| 7542 | `m_Script` | 送礼按钮 [&395170675] | `c1afe5a57ea2f714da534c2becfd1546` |  |
| 17477 | `objectReference -> m_Sprite` | PrefabInstance &959929500; missing target &7132004763855946177 [&959929500] | `db76f0e32cdb5b542876fca54976ca9b` | 覆盖目标组件 fileID 也已不存在 |
| 28558 | `m_Sprite` | Background [&1499779883] | `8980951e478e15a42933630e6daec026` |  |
| 28982 | `m_Script` | UIManger [&1518374156] | `a0971dd2302303d4cae7a09e28591762` |  |
| 30148 | `m_Sprite` | home [&1582605960] | `6e7e6f3358b3b7b45a986fe3d64d0427` |  |
| 39002 | `m_Script` | WorkManager [&2119034645] | `a7dab163589df8c4fa4aedb16b237c6c` |  |

### `Assets/Scenes/SampleScene.unity`（2 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 13118 | `objectReference -> m_Sprite` | PrefabInstance &959929500; missing target &7132004763855946177 [&959929500] | `db76f0e32cdb5b542876fca54976ca9b` | 构建场景静态依赖可达；覆盖目标组件 fileID 也已不存在 |
| 29904 | `objectReference -> m_Sprite` | reference / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs [&7481837990213141880] | `1dddc16e979bf35439fe91c51a5f631f` | 构建场景静态依赖可达 |

### `Assets/Scenes/workscene.unity`（8 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 1526 | `m_Script` | NormalJobListContainer [&383052297] | `90da3b8aaa282644babc502e0a65b498` |  |
| 7928 | `m_Sprite` | player 1 [&1556114841] | `43d51993e3521b541b83925686ec35c8` |  |
| 7963 | `m_Script` | player 1 [&1556114843] | `ecb8fba454f142ffa422ed4c72634c15` |  |
| 7976 | `m_Controller` | player 1 [&1556114844] | `3c318cdc9b6595942973b36c4f4d9d85` |  |
| 9497 | `m_Script` | ConfingManager [&1932012069] | `4ef9925c9f21e6141b7c90684ffca1cc` |  |
| 9906 | `m_Sprite` | player 2 [&2039723058] | `9c19258aaf1406141ae7e5db162ac265` |  |
| 9941 | `m_Script` | player 2 [&2039723060] | `ecb8fba454f142ffa422ed4c72634c15` |  |
| 9954 | `m_Controller` | player 2 [&2039723061] | `3c318cdc9b6595942973b36c4f4d9d85` |  |

### `Assets/Scenes/参考.unity`（4 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 7174 | `m_Script` | ConfigManger [&285230365] | `4ef9925c9f21e6141b7c90684ffca1cc` |  |
| 8804 | `m_Script` | 送礼按钮 [&395170675] | `c1afe5a57ea2f714da534c2becfd1546` |  |
| 23013 | `objectReference -> m_Sprite` | PrefabInstance &959929500; missing target &7132004763855946177 [&959929500] | `db76f0e32cdb5b542876fca54976ca9b` | 覆盖目标组件 fileID 也已不存在 |
| 49928 | `m_Script` | WorkManager [&2119034645] | `a7dab163589df8c4fa4aedb16b237c6c` |  |

### `Assets/Systems/Dialog/Dialog.unity`（34 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 327 | `m_Script` | 1 (5) [&282948910] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 388 | `m_Sprite` | BG (2) / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs [&383584380] | `22d9f33499327744a9bb3d11987438d0` |  |
| 447 | `m_Script` | 1 (12) [&435449668] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 491 | `m_Script` | 1 [&573562921] | `624e3ec482f162a4bb59d75476b6a9bd` |  |
| 655 | `m_Sprite` | BG (1) / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs [&592094746] | `8c1d235efd4be9e41a53c484e1c14ead` |  |
| 714 | `m_Script` | 1 (4) [&755728043] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 758 | `m_Script` | 1 (2) [&791168738] | `1013542204968ec4e8856a0842571774` |  |
| 849 | `m_Script` | 1 (6) [&820567460] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 1073 | `m_Script` | 1 (9) [&1129081658] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 1193 | `m_Script` | 1 (11) [&1183717117] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 1372 | `m_Script` | 1 (7) [&1341618808] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 1416 | `m_Script` | 1 (1) [&1411854910] | `c694fdde3c77a064ea28c062ecfdc371` |  |
| 1479 | `m_Sprite` | BG (4) / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs [&1421465415] | `a8e19ddca73366146b64abd7e7575ddb` |  |
| 1538 | `m_Script` | 1 (3) [&1507558838] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 1650 | `m_Script` | 1 (10) [&1687383654] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |
| 1712 | `m_Sprite` | BG (3) / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs [&1806961091] | `9d5cc87c0cdb6fa47bf19f996a664ff1` |  |
| 1802 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800386] | `0c6b52f7b303b0d4fb2919cbd858881f` |  |
| 1825 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800387] | `8011712a6a0892d428ff95e4cd442ea9` |  |
| 1848 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800388] | `dd9b2dd1f7bdcb14d8408ea10d536e83` |  |
| 1871 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800389] | `74c27a9eccbe6e0419bfe29a97a1090e` |  |
| 1894 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800390] | `83f3d5272b423d94b9be634ee35fbda5` |  |
| 1917 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800391] | `915ec00fe84fcc64682f6636c9c633ea` |  |
| 1940 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800392] | `e02fbea2407783945a05aa7ca5fe18cc` |  |
| 1963 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800393] | `5b495689e70f0444c8ebe8131f1a43b4` |  |
| 1986 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800394] | `6d398892e45551048a16054d80ee5566` |  |
| 2009 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800395] | `70bc5045390b3c5479d52d3379add76d` |  |
| 2032 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800396] | `96b0623ac9fad09499e20fb958e60437` |  |
| 2055 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800397] | `183b7d6a679b952479a3b9772afd840a` |  |
| 2078 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800398] | `77dd2fcff5c85d84bb497c53fc82903a` |  |
| 2101 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800399] | `eb0f4b477809b0747bce8b1ffa47b4c8` |  |
| 2124 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800400] | `f675f3e8a8b00914aa045a65b9473198` |  |
| 2147 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&2010800401] | `100696cfa01439643b0cdc9145ea1191` |  |
| 2263 | `m_Sprite` | BG / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs [&2021087751] | `3ab5dff5452c39346bbdd795a9a5002e` |  |
| 2322 | `m_Script` | 1 (8) [&2037944942] | `12ae07c314031d04eb68d4e95c0fcb4d` |  |

### `Assets/Systems/Dialog/DialogDemo.unity`（1 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 360 | `objectReference -> m_Sprite` | PrefabInstance &229313184; missing target &7132004763855946177 [&229313184] | `db76f0e32cdb5b542876fca54976ca9b` | 覆盖目标组件 fileID 也已不存在 |

### `Assets/Systems/Dialog/Prefabs/Button.prefab`（1 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 221 | `m_Sprite` | Button / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs [&3204088774893373850] | `b6139c90e288ee0478c68e472158ad4d` | 构建场景静态依赖可达 |

### `Assets/Systems/Dialog/Prefabs/DIalogSystem.prefab`（2 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 1190 | `m_Sprite` | PicR / Library/PackageCache/com.unity.ugui@1.0.0/Runtime/UI/Core/Image.cs [&8391305653561378684] | `2903e230de96bee44ac271c7dc65dea4` | 构建场景静态依赖可达 |
| 5890 | `dialogFile` | Dialog / Assets/Systems/Dialog/DialogVisual.cs [&7132004763581632258] | `2c7b43740c75d4c4dab4e64b284f72d0` | 构建场景静态依赖可达 |

### `Assets/Systems/Dialog/Shaders/ShaderHolder.mat`（1 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 60 | `m_Texture` | ShaderHolder [&2100000] | `adc9925db40c9a043bc9758bb2555181` | 构建场景静态依赖可达 |

### `Assets/预制件/BackgroundRoot.prefab`（1 处）

| 行号 | 字段 | 对象 / 组件 | GUID | 备注 |
| --- | --- | --- | --- | --- |
| 334 | `m_Sprite` | home [&8590086543296801331] | `6e7e6f3358b3b7b45a986fe3d64d0427` |  |

## 6. 已安装包内的未解析序列化引用

以下 7 处不计入项目自身统计。TextMeshPro 的引用位于脚本导入元数据，属于编辑器图标、皮肤或样式默认引用；SpriteShape 的引用位于默认预制体。不能由此推断游戏运行中必然出错。

| 位置 | 字段 | GUID | 原始引用 |
| --- | --- | --- | --- |
| [Library/PackageCache/com.unity.2d.spriteshape@7.0.7/Editor/ObjectMenuCreation/DefaultAssets/Sprite Shapes/Open Sprite Shape.prefab:78](../Library/PackageCache/com.unity.2d.spriteshape@7.0.7/Editor/ObjectMenuCreation/DefaultAssets/Sprite%20Shapes/Open%20Sprite%20Shape.prefab#L78) | `m_ShapeTexture` | `cb35359504814441a895fe5e3ce2f9fe` | `m_ShapeTexture: {fileID: 2800000, guid: cb35359504814441a895fe5e3ce2f9fe, type: 3}` |
| [Library/PackageCache/com.unity.2d.spriteshape@7.0.7/Editor/ObjectMenuCreation/DefaultAssets/Sprite Shapes/Open Sprite Shape.prefab:80](../Library/PackageCache/com.unity.2d.spriteshape@7.0.7/Editor/ObjectMenuCreation/DefaultAssets/Sprite%20Shapes/Open%20Sprite%20Shape.prefab#L80) | `m_Sprites` | `2f3056e08c07b4cfabadcfee59be0309` | `- {fileID: 21300000, guid: 2f3056e08c07b4cfabadcfee59be0309, type: 3}` |
| [Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMPro_Private.cs.meta:7](../Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMPro_Private.cs.meta#L7) | `icon` | `8e098d8d28c5182419f7a1c8b91ca722` | `icon: {fileID: 2800000, guid: 8e098d8d28c5182419f7a1c8b91ca722, type: 3}` |
| [Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMPro_UGUI_Private.cs.meta:7](../Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMPro_UGUI_Private.cs.meta#L7) | `icon` | `8e098d8d28c5182419f7a1c8b91ca722` | `icon: {fileID: 2800000, guid: 8e098d8d28c5182419f7a1c8b91ca722, type: 3}` |
| [Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMP_PackageResourceImporter.cs.meta:8](../Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMP_PackageResourceImporter.cs.meta#L8) | `LightSkin` | `0c156a7b2f4d450da1716b1625b5441d` | `- LightSkin: {fileID: 11400000, guid: 0c156a7b2f4d450da1716b1625b5441d, type: 2}` |
| [Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMP_PackageResourceImporter.cs.meta:9](../Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMP_PackageResourceImporter.cs.meta#L9) | `DarkSkin` | `9d345c3252c147c89e8b61a249a46a9d` | `- DarkSkin: {fileID: 11400000, guid: 9d345c3252c147c89e8b61a249a46a9d, type: 2}` |
| [Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMP_StyleSheet.cs.meta:8](../Library/PackageCache/com.unity.textmeshpro@3.0.6/Scripts/Runtime/TMP_StyleSheet.cs.meta#L8) | `StyleSheet` | `cab1ac28c8e6be24e995befe0c36d7c1` | `- StyleSheet: {fileID: 11400000, guid: cab1ac28c8e6be24e995befe0c36d7c1, type: 2}` |

另有 266 条包内 GUID 文本候选未归为资源丢失：258 条来自 TextMeshPro 的两个历史 GUID 转换表，8 条来自 Visual Scripting 的旧版本 DLL 识别常量。完整原始候选保留在 `Library/ReferenceAudit/missing-package-references.csv` 中。

## 7. 复查方式

临时脚本仍在时，可在项目根目录运行：

```powershell
node Library/ReferenceAudit/audit-guid-references.cjs
node Library/ReferenceAudit/write-report.cjs
```

第一条重新扫描磁盘并只读查询 Git 历史，第二条更新本报告和三份剩余引用 CSV。报告行号对应本次工作区快照，后续修改场景或预制体后需要重跑。本次通过 PowerShell 导出同一 HEAD 的 Git 历史快照后，使用 `--git-snapshot=Library/ReferenceAudit/git-snapshot.json` 参数完成扫描，以适配当前环境的 Node 子进程限制；复用该快照前须确认 HEAD 和所查 GUID 集合未变化。
