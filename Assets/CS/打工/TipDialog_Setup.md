## TipDialog 弹窗组件配置说明

### 配置步骤

1. 在 Unity 编辑器中打开任意场景（推荐打开 SampleScene.unity）

2. 在 Hierarchy 面板中右键点击，选择 `UI -> Panel` 创建一个新面板

3. 将面板命名为 `TipDialogPanel`

4. 在该面板下创建以下子对象：
   - **Text (TMP)**: 命名为 `MessageText`，用于显示提示信息
   - **Button**: 命名为 `CloseButton`，用于关闭弹窗
   - 可选：添加一个背景图片作为弹窗框架

5. 调整面板位置和大小，使其居中显示在屏幕上

6. 给 `TipDialogPanel` 添加 `TipDialog` 脚本组件

7. 在 `TipDialog` 组件的 Inspector 面板中配置：
   - **Panel**: 拖入 `TipDialogPanel` 游戏对象
   - **Message Text**: 拖入 `MessageText` 文本组件
   - **Close Button**: 拖入 `CloseButton` 按钮组件

8. 确保弹窗面板在初始状态下是隐藏的（取消勾选面板的 Active 复选框）

### 组件参数说明

| 参数 | 类型 | 说明 |
|------|------|------|
| Panel | GameObject | 弹窗的主面板对象 |
| Message Text | TextMeshProUGUI | 显示提示信息的文本组件 |
| Close Button | Button | 关闭弹窗的按钮 |

### 使用方式

代码中通过 `TipDialog.Instance.Show(message)` 显示弹窗，例如：
```csharp
TipDialog.Instance.Show("AP不足，无法添加该打工");
```

### 注意事项

- `TipDialog` 使用单例模式，确保场景中只有一个实例
- 使用 `DontDestroyOnLoad`，弹窗会在场景切换时保留
- 关闭按钮会自动绑定 `Hide()` 方法