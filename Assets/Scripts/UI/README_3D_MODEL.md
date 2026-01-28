# 3D模型加载与渲染系统说明

本系统为NeXTMakeStudio添加了3D模型加载、渲染和编辑功能，支持UV打印和3D打印两种模式。

## 功能特性

### 1. 3D模型加载
- 支持STL格式（ASCII和Binary）
- 支持OBJ格式
- 异步加载，不阻塞UI
- 自动计算法线和边界

### 2. 3D模型渲染
- 使用RenderTexture在UI上显示3D模型
- 支持实时渲染和交互
- 可配置的渲染分辨率和质量

### 3. 模式切换
- **UV打印模式**：2D图片编辑（原有功能）
- **3D打印模式**：3D模型编辑（新增功能）
- 一键切换，界面自动适配

### 4. 3D模型操作
- **旋转**：鼠标左键拖拽旋转模型
- **缩放**：鼠标滚轮缩放
- **平移**：鼠标中键拖拽平移
- **切片**：支持模型切片预览（需要进一步实现切片着色器）

## 文件结构

```
Assets/Scripts/
├── Core/
│   ├── ModelLoader.cs          # 3D模型加载器
│   └── PrintMode.cs            # 打印模式枚举
├── UI/
│   ├── Model3DViewer.cs        # 3D模型查看器
│   ├── Model3DController.cs    # 3D模型控制器
│   ├── PrintModeManager.cs     # 模式切换管理器
│   └── StudioUIManager.cs      # 主UI管理器（已更新）
└── Utils/
    └── ModelDownloader.cs       # 模型下载器（示例）
```

## 使用方法

### 在Unity编辑器中设置

1. **创建3D视图容器**
   - 在MainView下创建两个子对象：
     - `UVPrintView`：用于2D图片显示（已有）
     - `Print3DView`：用于3D模型显示（新建）

2. **设置Model3DViewer**
   - 在Print3DView上添加`Model3DViewer`组件
   - 设置`targetImage`为RawImage组件（用于显示渲染结果）
   - 可选：设置`renderCamera`（不设置会自动创建）

3. **设置Model3DController**
   - 在Print3DView上添加`Model3DController`组件
   - 设置`modelViewer`引用
   - 配置旋转、缩放、平移速度

4. **设置PrintModeManager**
   - 在StudioUIManager对象上添加`PrintModeManager`组件
   - 设置模式切换按钮（`uvPrintModeButton`、`print3DModeButton`）
   - 设置视图容器引用

5. **更新StudioUIManager**
   - 在Inspector中设置：
     - `printModeManager`：PrintModeManager组件
     - `uvPrintViewContainer`：UV打印视图容器
     - `print3DViewContainer`：3D打印视图容器
     - `model3DViewer`：Model3DViewer组件
     - `model3DController`：Model3DController组件

### 加载3D模型

#### 方法1：通过文件对话框（编辑器模式）
1. 切换到3D打印模式
2. 点击"打开项目"按钮
3. 选择STL或OBJ文件

#### 方法2：通过代码加载
```csharp
ModelLoader loader = GetComponent<ModelLoader>();
GameObject model = await loader.LoadModelTaskAsync("path/to/model.stl");
model3DViewer.SetModel(model);
```

#### 方法3：从makerworld下载（需要实现API）
```csharp
ModelDownloader downloader = GetComponent<ModelDownloader>();
string modelPath = await downloader.DownloadFromMakerWorldAsync(
    "https://makerworld.com.cn/zh/3d-models/...",
    Application.persistentDataPath + "/3DModels/model.stl"
);
```

## 从makerworld下载模型

### 当前实现状态
`ModelDownloader.cs`提供了基础的下载框架，但需要根据makerworld的实际API或网页结构来实现：

1. **如果makerworld有公开API**：
   - 实现`GetDownloadUrlFromAPI`方法
   - 使用API获取模型下载链接

2. **如果需要解析HTML**：
   - 使用HTML解析库（如HtmlAgilityPack）
   - 解析页面获取下载链接

3. **如果需要在浏览器中打开**：
   - 使用`Application.OpenURL`打开页面
   - 提示用户手动下载并保存到指定位置

### 示例：手动下载流程
1. 访问 https://makerworld.com.cn/zh/3d-models
2. 选择并下载模型文件（STL或OBJ格式）
3. 将文件保存到 `Application.persistentDataPath/3DModels/` 目录
4. 在Unity中使用文件对话框加载

## 扩展功能

### 切片功能
当前切片功能提供了基础框架，需要进一步实现：
1. 创建切片着色器（Shader）
2. 实现模型裁剪逻辑
3. 支持多层切片预览

### 模型编辑
可以扩展以下功能：
- 模型合并
- 模型分割
- 支撑生成
- 打印参数设置（层高、填充率等）

### 参考软件功能
- **NeXTMakeStudio**：UV打印功能参考
- **NeXTStudio**：3D打印切片和编辑功能参考

## 注意事项

1. **性能优化**：
   - 大型模型可能需要优化网格
   - 考虑使用LOD（细节层次）系统
   - RenderTexture分辨率影响性能

2. **内存管理**：
   - 及时销毁不需要的模型
   - 释放RenderTexture资源

3. **文件格式**：
   - 当前支持STL和OBJ
   - 可以扩展支持3MF、PLY等格式

4. **平台兼容性**：
   - WebGL平台可能需要特殊处理
   - 移动平台性能考虑

## 下一步工作

1. ✅ 基础3D模型加载和渲染
2. ✅ 模式切换系统
3. ✅ 基础模型操作（旋转、缩放、平移）
4. ⏳ 切片功能完整实现
5. ⏳ makerworld API集成
6. ⏳ 模型编辑工具（合并、分割等）
7. ⏳ 打印参数设置界面
8. ⏳ 支撑生成算法

## 问题反馈

如遇到问题，请检查：
1. 模型文件格式是否正确
2. 文件路径是否有效
3. RenderTexture是否正确设置
4. 相机和灯光配置是否正确

