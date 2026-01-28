# NeXTMake Studio UI 框架开发文档

本文档详细介绍了 NeXTMake Studio 项目的 UI 框架结构、核心组件使用方法、以及常见功能的扩展指南。本框架采用 **全代码生成 (Code-Driven UI)** 模式，基于 Unity UGUI 系统构建。

---

## 1. 架构概览 (Architecture Overview)

UI 系统采用了 **模块化 (Modular)** 设计，通过一个总入口脚本协调各个独立的功能模块。

### 1.1 核心流程图

```mermaid
graph TD
    Entry[NeXTMakeStudioUIAutoSetup.cs] -->|初始化| Factory[UIFactory (资源/样式配置)]
    Entry -->|创建| Canvas[Canvas & Root Container]
    Entry -->|挂载| Managers[UIManager & PrintModeManager]
    
    Entry -->|调用| ModHome[HomeModule]
    Entry -->|调用| Mod3D[Print3DModule]
    
    ModHome -->|构建| LayoutUV[UVPrintStudioLayout]
    ModHome -->|构建| ViewProj[ProjectsView]
    ModHome -->|构建| ViewDetail[DetailViewModule]
    
    ModHome -.->|动态创建| ModCanvas[CanvasModule (编辑器)]
    
    subgraph "UI 生成核心 (UIFactory)"
    CreateObj[CreateObject]
    CreateTxt[CreateText]
    CreateBtn[CreateButton]
    Layout[Auto Layout System]
    end
    
    ModHome -.-> CreateObj
    Mod3D -.-> CreateObj
    ModCanvas -.-> CreateObj
    ViewDetail -.-> CreateObj
```

### 1.2 目录结构

*   **Entry (入口)**: `NeXTMakeStudioUIAutoSetup.cs` - 负责初始化 Canvas，调用各模块构建 UI。
*   **Core (核心)**:
    *   `UIFactory.cs`: **UI 工厂类**，封装了所有创建 UI 元素的方法（按钮、文本、面板等）。
    *   `ProjectData.cs`: 数据模型定义。
*   **Modules (模块)**:
    *   `HomeModule.cs`: 主页、项目列表、创意实验室。
    *   `DetailViewModule.cs`: 项目详情弹窗。
    *   `CanvasModule.cs`: 画布编辑器、左侧工具栏、图层面板。
    *   `Print3DModule.cs`: 3D 打印模式界面。
*   **Components (组件)**:
    *   `ProjectDetailViewUpdater.cs`: 负责详情页的数据更新逻辑。
    *   `DragRotator.cs`: 简单的 3D 旋转交互逻辑。

---

## 2. 核心组件使用手册 (UIFactory Guide)

所有 UI 元素的创建都应通过 `NeXTMake.UI.Core.UIFactory` 类进行，以确保风格统一。

### 2.1 基础容器与布局

| 功能 | 方法/组件 | 示例代码 | 说明 |
| :--- | :--- | :--- | :--- |
| **创建空节点** | `CreateObject` | `var obj = UIFactory.CreateObject("Name", parent);` | 基础构建块，自带 RectTransform。 |
| **铺满全屏** | `Stretch` | `UIFactory.Stretch(rectTransform);` | 将锚点设为(0,0)-(1,1)，位置归零。用于背景板或全屏遮罩。 |
| **垂直布局** | `VerticalLayoutGroup` | `obj.AddComponent<VerticalLayoutGroup>();` | 自动纵向排列子物体。常用属性：`spacing`(间距), `padding`(边距)。 |
| **水平布局** | `HorizontalLayoutGroup` | `obj.AddComponent<HorizontalLayoutGroup>();` | 自动横向排列子物体。 |
| **网格布局** | `GridLayoutGroup` | `obj.AddComponent<GridLayoutGroup>();` | 用于素材库、卡片列表。需设置 `cellSize` (单元格大小)。 |
| **布局权重** | `LayoutElement` | `obj.AddComponent<LayoutElement>();` | 控制子物体大小。`minHeight`(固定高度), `flexibleWidth=1`(自动填满剩余宽度)。 |

### 2.2 常用控件创建

#### 1. 文本 (Text)
```csharp
// 参数: 内容, 父物体, 字号, 颜色, 位置(相对于锚点), 尺寸
UIFactory.CreateText("Hello", parent, 14, Color.black, Vector2.zero, new Vector2(100, 30));
```

#### 2. 标准按钮 (Button)
```csharp
// 参数: 按钮文字, 父物体, 位置, 尺寸, 背景色, 文字色
GameObject btn = UIFactory.CreateButton("Click Me", parent, Vector2.zero, new Vector2(120, 40), Color.blue, Color.white);
// 添加点击事件
btn.GetComponent<Button>().onClick.AddListener(() => Debug.Log("Clicked!"));
```

#### 3. 纯文字按钮 (Text Button)
```csharp
// 只有文字，背景透明，鼠标悬停无变化（可扩展）
UIFactory.CreateTextButton("Menu Item", parent, 12, Color.gray);
```

#### 4. 带图标的选择卡片 (Selection Card)
```csharp
// 用于启动页的模式选择
UIFactory.CreateSelectionCard("UV Print Studio", parent);
```

---

## 3. 功能扩展指南 (Extension Guide)

### 场景一：在 Canvas 编辑器左侧添加新工具

**目标**: 添加一个名为 "Sticker" 的工具栏，点击后显示贴纸列表。

1.  **打开文件**: `Assets/Scripts/UI/Modules/CanvasModule.cs`
2.  **修改工具列表**:
    在 `CreateCanvasEditor` 方法中找到 `tools` 数组：
    ```csharp
    // 添加 "Sticker" 到数组
    string[] tools = { "Upload", "Image AI", "Sticker", "Templates", ... };
    ```
3.  **实现面板逻辑**:
    在 `ShowSidePanel` 方法的 `switch` 语句中添加 case：
    ```csharp
    case "Sticker":
        // 使用 SetupGrid 辅助方法快速创建网格
        SetupGrid(container, 10, (i) => {
            // 定义点击贴纸后的行为：在画布(paper)上生成物体
            GameObject sticker = UIFactory.CreateObject("StickerObj", paper);
            RectTransform r = sticker.GetComponent<RectTransform>();
            r.sizeDelta = new Vector2(100, 100);
            
            // 添加图片和交互组件
            sticker.AddComponent<Image>().color = Color.green; // 示例：绿色方块
            AddManipulationComponents(sticker); // 添加拖拽脚本
        }, "StickerIcon");
        break;
    ```

### 场景二：修改详情页 (Detail View) 的布局

**目标**: 将详情页的 "Like" 按钮移到标题下方。

1.  **打开文件**: `Assets/Scripts/UI/Modules/DetailViewModule.cs`
2.  **定位代码**: 找到 `CreateProjectDetailView` 方法。
3.  **调整层级**:
    *   找到创建 `likeBtn` 的代码段。
    *   目前它被添加到了 `buttonContainer` (水平布局) 中。
    *   如果要移到标题下方，需要找到 `info` (垂直布局容器) 对象。
    *   将 `likeBtn` 的创建代码移动到 `buttonContainer` 创建代码之前，并将其父物体参数从 `buttonContainer` 改为 `info`。
    ```csharp
    // 原代码: GameObject likeBtn = UIFactory.CreateButton(..., buttonContainer, ...);
    // 修改为:
    GameObject likeBtn = UIFactory.CreateButton("🟢Like", info, ...); 
    ```

### 场景三：修改全局字体或颜色

**目标**: 将应用的主题色从绿色改为蓝色。

1.  **打开文件**: `Assets/Scripts/UI/Core/UIFactory.cs`
2.  **修改常量**:
    ```csharp
    // 修改 COLOR_ACCENT_GREEN 的值
    public static readonly Color COLOR_ACCENT_GREEN = new Color(0.0f, 0.5f, 1.0f); // 改为蓝色
    ```
3.  **重新运行**: 由于是全代码生成，修改常量后运行游戏，所有引用该颜色的按钮都会自动变色。

---

## 4. 模块功能对照表 (Module Reference)

| 模块 (Module) | 包含功能 (Sub-features) | 修改建议 |
| :--- | :--- | :--- |
| **HomeModule** | 1. 顶部导航栏 (NavBar)<br>2. 左侧分类栏 (LeftSidebar)<br>3. 项目网格 (MainGrid)<br>4. 过滤器 (Filters) | 修改首页布局、增加新的分类、调整项目卡片样式。 |
| **DetailViewModule** | 1. 左侧缩略图列表<br>2. 中间大图预览<br>3. 右侧信息/操作按钮<br>4. 底部评论区 | 修改详情弹窗的交互逻辑、增删操作按钮、修改评论区样式。 |
| **CanvasModule** | 1. 左侧工具栏 (LeftToolBar)<br>2. 动态侧边面板 (Drawer)<br>3. 画布区域 (Workspace/Paper)<br>4. 右侧属性面板 (RightPanel)<br>5. 3D 预览层 (PreviewLayer) | **核心编辑器逻辑**。添加新工具、修改属性面板参数、调整画布交互。 |
| **Print3DModule** | 1. 顶部菜单<br>2. 功能Tab (Home/Model/Slice)<br>3. 左侧侧边栏 | 3D 打印模式的特有界面。 |

## 5. 调试与常见问题

*   **布局错乱**: 检查是否错误地混合使用了 `ContentSizeFitter` 和 `LayoutElement` 的 `flexible` 属性。
*   **点击无效**: 检查遮挡关系。UI 是按创建顺序绘制的，后创建的在最上层。`Text` 组件默认 `raycastTarget=false` (在 UIFactory 中设置)，如果需要点击文字，需手动开启。
*   **重构注意**: `NeXTMakeStudioUIAutoSetup.cs` 是入口，如果您添加了新的 Module，记得在这里实例化并调用它。


