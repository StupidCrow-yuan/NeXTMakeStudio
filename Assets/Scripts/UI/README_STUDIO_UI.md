# Studio UI 界面说明

本目录包含了类似 NeXTMake Studio 的专业界面系统。

## 组件说明

### 1. StudioUIManager.cs
主要的UI管理器，负责整体布局和各个面板的协调。

**主要功能：**
- 管理顶部菜单栏、工具栏
- 管理左侧工具面板、右侧属性面板
- 管理中间主视图区域
- 管理底部状态栏
- 响应式布局调整

### 2. MenuBar.cs
顶部菜单栏组件，提供文件、编辑、视图、工具、帮助等菜单。

**主要功能：**
- 下拉菜单显示
- 菜单项点击处理
- 自动关闭菜单

### 3. ToolBar.cs
顶部工具栏组件，提供常用工具的快速访问。

**主要功能：**
- 新建、打开、保存项目
- 撤销、重做操作
- 设置按钮

### 4. LeftPanel.cs
左侧工具面板，提供各种绘图和编辑工具。

**主要功能：**
- 工具选择（选择、画笔、橡皮擦、形状、文本等）
- 工具状态管理
- 面板显示/隐藏切换

### 5. RightPanel.cs
右侧属性面板，显示和编辑当前选中对象的属性。

**主要功能：**
- 不透明度调整
- 画笔大小调整
- 旋转和缩放控制
- 颜色选择器

### 6. StatusBar.cs
底部状态栏，显示当前状态信息。

**主要功能：**
- 状态消息显示
- 缩放比例显示
- 位置信息显示
- 图像尺寸显示
- 进度条显示

## 使用说明

### 在Unity中设置Studio界面

1. **创建Canvas**
   - 在场景中创建一个Canvas（Screen Space - Overlay）
   - 设置Canvas Scaler为Scale With Screen Size，参考分辨率1920x1080

2. **创建主容器**
   - 在Canvas下创建一个GameObject作为主容器
   - 添加RectTransform，设置为全屏（Anchor: Stretch-Stretch）

3. **添加StudioUIManager**
   - 在主容器上添加StudioUIManager组件
   - 按照Inspector中的提示分配各个UI元素

4. **创建各个面板**
   - **菜单栏**：创建水平布局的容器，添加MenuBar组件
   - **工具栏**：创建水平布局的容器，添加ToolBar组件
   - **左侧面板**：创建垂直布局的容器，添加LeftPanel组件
   - **右侧面板**：创建垂直布局的容器，添加RightPanel组件
   - **主视图**：创建ScrollRect容器，用于显示图像
   - **状态栏**：创建水平布局的容器，添加StatusBar组件

5. **连接MainUIManager**
   - 在MainUIManager组件中，分配StudioUIManager和StatusBar引用
   - 这样图像加载和处理功能会自动更新Studio界面

## 界面布局结构

```
Canvas
└── MainContainer (StudioUIManager)
    ├── MenuBar (30px高，顶部)
    ├── ToolBar (40px高，菜单栏下方)
    ├── LeftPanel (250px宽，左侧，可折叠)
    ├── MainView (中间区域，自适应)
    ├── RightPanel (300px宽，右侧，可折叠)
    └── StatusBar (25px高，底部)
```

## 功能扩展

所有组件都预留了TODO标记，方便后续添加具体功能：

- 菜单项功能实现
- 工具功能实现
- 属性编辑功能实现
- 颜色选择器实现
- 撤销/重做系统
- 项目保存/加载系统

## 注意事项

1. 所有UI组件都支持TextMeshPro和普通Text两种模式
2. 面板可以通过按钮切换显示/隐藏
3. 布局会自动响应面板的显示/隐藏状态
4. 底层功能（图像加载、处理等）保持不变，通过MainUIManager集成

## 下一步工作

1. 在Unity编辑器中创建实际的UI GameObject结构
2. 分配各个组件的引用
3. 实现具体的工具功能
4. 添加颜色选择器对话框
5. 实现撤销/重做系统
6. 添加项目保存/加载功能
