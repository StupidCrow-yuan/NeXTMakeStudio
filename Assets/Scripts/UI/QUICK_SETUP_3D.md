# 3D模型功能快速设置指南

## 步骤1：创建3D视图容器

1. 在Unity Hierarchy中找到`MainView`对象（或主视图容器）
2. 创建两个子对象：
   - `UVPrintView`（如果不存在）
   - `Print3DView`（新建）

## 步骤2：设置Print3DView

1. 选中`Print3DView`对象
2. 添加以下组件：
   - `RawImage`组件（用于显示3D渲染结果）
   - `Model3DViewer`组件
   - `Model3DController`组件

3. 配置`Model3DViewer`：
   - `Target Image`：拖入刚才创建的RawImage
   - `Texture Width`：1024（或根据需要调整）
   - `Texture Height`：1024（或根据需要调整）

4. 配置`Model3DController`：
   - `Model Viewer`：拖入Model3DViewer组件
   - `Rotation Speed`：2.0
   - `Zoom Speed`：0.1

## 步骤3：设置模式切换按钮

1. 在工具栏或菜单栏创建两个按钮：
   - `UVPrintModeButton`：文本显示"UV打印"
   - `Print3DModeButton`：文本显示"3D打印"

2. 在StudioUIManager对象上添加`PrintModeManager`组件

3. 配置`PrintModeManager`：
   - `UV Print Mode Button`：拖入UV打印按钮
   - `Print 3D Mode Button`：拖入3D打印按钮
   - `UV Print View`：拖入UVPrintView对象
   - `Print 3D View`：拖入Print3DView对象
   - `Image Viewer`：拖入现有的ImageViewer组件
   - `Model 3D Viewer`：拖入Model3DViewer组件
   - `Model 3D Controller`：拖入Model3DController组件

## 步骤4：更新StudioUIManager

1. 选中包含`StudioUIManager`组件的对象
2. 在Inspector中找到新增的字段：
   - `Print Mode Manager`：拖入PrintModeManager组件
   - `UV Print View Container`：拖入UVPrintView对象
   - `Print 3D View Container`：拖入Print3DView对象
   - `Model 3D Viewer`：拖入Model3DViewer组件
   - `Model 3D Controller`：拖入Model3DController组件

## 步骤5：测试功能

1. 运行场景
2. 点击"3D打印"按钮切换到3D模式
3. 点击"打开项目"按钮，选择STL或OBJ文件
4. 使用鼠标操作模型：
   - 左键拖拽：旋转
   - 滚轮：缩放
   - 中键拖拽：平移

## 常见问题

### Q: 看不到3D模型？
A: 检查：
- Model3DViewer的targetImage是否正确设置
- RenderTexture是否正确创建
- 模型是否成功加载

### Q: 模型显示太小或太大？
A: 调整Model3DViewer的相机设置，或使用Model3DController的ResetTransform方法

### Q: 模式切换不工作？
A: 检查PrintModeManager的所有引用是否正确设置

### Q: 如何添加更多模型格式支持？
A: 在ModelLoader.cs中添加新的格式解析方法

## 下一步

- 实现切片功能
- 添加模型编辑工具
- 集成makerworld API
- 优化性能和内存使用

