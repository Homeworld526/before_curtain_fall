# 存档系统 (SaveSystem)

## 概述
这是一个功能完善的Unity存档系统，支持多槽位存档、自动截图、JSON格式数据存储，具有良好的扩展性。

## 核心功能

### 1. 保存游戏（自动截图）
```csharp
// 异步保存游戏，会自动截取当前画面
SaveSystem.Instance.SaveGame(0, (success) => {
    if (success) {
        Debug.Log("保存成功");
    } else {
        Debug.Log("保存失败");
    }
});
```

### 2. 读取游戏
```csharp
// 从指定槽位加载存档
bool success = SaveSystem.Instance.LoadGame(0);
if (success) {
    // 加载成功，数据已自动应用到游戏
}
```

### 3. 检查存档是否存在
```csharp
if (SaveSystem.Instance.IsSaveExists(0)) {
    // 存档存在
}
```

### 4. 删除存档（同时删除截图）
```csharp
SaveSystem.Instance.DeleteSave(0);
```

### 5. 获取所有存档信息
```csharp
List<SaveInfo> saveInfos = SaveSystem.Instance.GetAllSaveInfos();
foreach (var info in saveInfos) {
    Debug.Log($"槽位: {info.SlotIndex}, 时间: {info.SaveTime}, 截图: {info.Pic}");
}
```

### 6. 创建新游戏
```csharp
SaveSystem.Instance.CreateNewSaveData();
```

## 自动截图功能

### 特性
- **自动触发**：每次保存游戏时自动截取当前游戏画面
- **低分辨率**：默认320x180，节省存储空间
- **PNG格式**：高质量压缩，文件体积小
- **路径存储**：Pic字段存储相对路径，便于跨平台使用
- **Layer过滤**：可配置忽略指定Layer，避免UI等元素出现在截图中

### 截图配置
在Unity Inspector中可调整以下参数：
- `screenshotWidth`: 截图宽度（默认320）
- `screenshotHeight`: 截图高度（默认180）
- `screenshotIgnoreLayers`: 截图时忽略的Layer掩码（LayerMask）

#### 配置忽略Layer示例
如果你想让截图不包含UI元素：
1. 在Inspector中找到 `Screenshot Ignore Layers` 字段
2. 点击右侧的下拉菜单
3. 勾选 `UI` Layer
4. 这样截图时所有UI元素都会被忽略

你也可以通过代码设置：
```csharp
// 忽略UI层（Layer 5）
SaveSystem.Instance.SetIgnoreLayer(LayerMask.GetMask("UI"));

// 忽略多个层
int mask = LayerMask.GetMask("UI", "Water", "Effects");
SaveSystem.Instance.SetIgnoreLayer(mask);
```

### 截图存储位置
```
<persistentDataPath>/Screenshots/
├── Screenshot_Slot0_20260411_143022.png
├── Screenshot_Slot1_20260411_150015.png
└── Screenshot_Slot2_20260411_160530.png
```

### 使用截图
```csharp
// 获取存档信息中的截图路径
SaveInfo saveInfo = SaveSystem.Instance.GetAllSaveInfos()[0];
string screenshotPath = Path.Combine(Application.persistentDataPath, saveInfo.Pic);

// 加载截图到UI
if (File.Exists(screenshotPath)) {
    byte[] fileData = File.ReadAllBytes(screenshotPath);
    Texture2D tex = new Texture2D(2, 2);
    tex.LoadImage(fileData);
    
    // 应用到UI Image
    Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    yourImage.sprite = sprite;
}
```

## 如何添加新的保存数据

### 步骤1：创建新的数据类
在 `SaveSystem.cs` 或其他位置创建数据类，继承 `SaveData`：

```csharp
[System.Serializable]
public class InventorySaveData : SaveData
{
    public List<string> ItemIds = new List<string>();
    public List<int> ItemCounts = new List<int>(); // 避免使用Dictionary
}
```

### 步骤2：在 CompleteSaveData 中添加字段
```csharp
[System.Serializable]
public class CompleteSaveData
{
    public string SaveTime;              // 存档时间
    public int SaveSlotIndex;            // 存档槽位
    public PlayerSaveData PlayerData;    // 玩家数据
    public string Pic;                   // 截图路径（自动生成）

    // 添加你的新数据
    public InventorySaveData InventoryData;  // <-- 新增

    public CompleteSaveData()
    {
        SaveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        PlayerData = new PlayerSaveData();
        InventoryData = new InventorySaveData();  // <-- 初始化
    }
}
```

### 步骤3：实现数据收集方法
在 `CollectSaveData()` 方法中添加收集逻辑：

```csharp
private void CollectSaveData()
{
    CollectPlayerData();
    CollectInventoryData();  // <-- 新增
}

private void CollectInventoryData()  // <-- 新增方法
{
    // 从你的库存管理器收集数据
    // if (InventoryManager.Instance != null)
    // {
    //     currentSaveData.InventoryData.ItemIds = InventoryManager.Instance.GetItemIds();
    //     currentSaveData.InventoryData.ItemCounts = InventoryManager.Instance.GetItemCounts();
    // }
}
```

### 步骤4：实现数据应用方法
在 `ApplyLoadData()` 方法中添加应用逻辑：

```csharp
private void ApplyLoadData()
{
    ApplyPlayerData();
    ApplyInventoryData();  // <-- 新增
}

private void ApplyInventoryData()  // <-- 新增方法
{
    // 将数据应用到你的库存管理器
    // if (InventoryManager.Instance != null && currentSaveData.InventoryData != null)
    // {
    //     InventoryManager.Instance.LoadItems(currentSaveData.InventoryData);
    // }
}
```

## 数据存储位置

### 存档文件
- **Windows**: `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Saves\`
- **Mac**: `~/Library/Application Support/<CompanyName>/<ProductName>/Saves/`
- **Linux**: `~/.config/<CompanyName>/<ProductName>/Saves/`

### 截图文件
- **Windows**: `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Screenshots\`
- **Mac**: `~/Library/Application Support/<CompanyName>/<ProductName>/Screenshots/`
- **Linux**: `~/.config/<CompanyName>/<ProductName>/Screenshots/`

存档文件以JSON格式保存，可以直接用文本编辑器查看和修改。

## 数据结构

### CompleteSaveData
完整存档数据，包含所有模块信息：
- `SaveTime`: 存档时间（字符串）
- `SaveSlotIndex`: 存档槽位（整数）
- `PlayerData`: 玩家数据
- `Pic`: 截图相对路径（自动生成）

### PlayerSaveData
玩家游戏数据示例：
- `dialogIndex`: 对话索引
- `dialogLine`: 对话行号

### SaveInfo
存档摘要信息（用于存档列表显示）：
- `SlotIndex`: 槽位索引
- `SaveTime`: 存档时间
- `Pic`: 截图路径

## 注意事项

1. **异步保存**：`SaveGame` 是异步方法，必须使用回调函数获取保存结果
2. **序列化特性**：所有数据类必须添加 `[System.Serializable]` 特性
3. **Dictionary限制**：`JsonUtility` 不支持Dictionary的嵌套，请使用两个List替代
4. **存档槽位**：从0开始，默认3个槽位，可在Inspector中修改 `maxSaveSlots`
5. **截图性能**：截图会在保存时自动进行，建议在合适的时机调用保存（如菜单中）
6. **路径格式**：Pic字段存储的是相对于 `persistentDataPath` 的相对路径
7. **错误处理**：建议在关键操作前后添加错误处理和日志输出

## 扩展建议

- 可添加截图质量调节功能
- 可添加存档加密功能
- 可添加云存档支持
- 可添加存档预览功能
