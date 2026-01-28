# MakerWorld 3D模型下载指南

本指南说明如何从 makerworld.com.cn 下载3D模型并在Unity中加载。

## 方法一：手动下载（推荐）

### 步骤1：访问MakerWorld网站
1. 打开浏览器，访问 https://makerworld.com.cn/zh/3d-models
2. 浏览或搜索你想要的3D模型

### 步骤2：下载模型文件
1. 点击进入模型详情页
2. 找到"下载"按钮或"文件"选项
3. 选择STL格式（推荐）或OBJ格式
4. 将文件保存到本地

### 步骤3：将模型文件放入Unity项目
1. 将下载的STL或OBJ文件复制到 `Assets/3DModels` 文件夹
2. 如果文件夹不存在，在Unity中创建：
   - 在Project窗口右键点击 `Assets` 文件夹
   - 选择 `Create > Folder`
   - 命名为 `3DModels`

### 步骤4：在Unity中加载模型
1. 运行Unity场景
2. 点击"3D打印"按钮切换到3D模式
3. 系统会自动加载 `Assets/3DModels` 文件夹下的第一个STL文件
4. 或者点击"打开项目"按钮，手动选择STL文件

## 方法二：使用下载工具（需要实现API）

如果MakerWorld提供公开API，可以使用 `ModelDownloader` 工具自动下载。

### 使用示例代码：
```csharp
ModelDownloader downloader = GetComponent<ModelDownloader>();
string modelPath = await downloader.DownloadModelAsync(
    "https://makerworld.com.cn/download/model.stl",
    Application.dataPath + "/3DModels/model.stl"
);
```

## 方法三：浏览器扩展下载

某些浏览器扩展可以帮助批量下载模型文件。

## 注意事项

1. **文件格式**：推荐使用STL格式，Unity已支持
2. **文件大小**：大型模型可能需要较长时间加载
3. **文件路径**：确保文件路径中没有中文字符（可能导致问题）
4. **文件权限**：确保Unity有读取文件的权限

## 常见问题

### Q: 下载的文件在哪里？
A: 通常在浏览器的"下载"文件夹中，需要手动复制到Unity项目

### Q: 支持哪些格式？
A: 当前支持STL和OBJ格式，其他格式需要扩展ModelLoader

### Q: 如何批量下载？
A: 目前需要手动逐个下载，或使用浏览器扩展工具

### Q: 下载的模型无法加载？
A: 检查：
- 文件格式是否正确（.stl或.obj）
- 文件是否损坏
- Console中是否有错误信息

