# MakerWorld 3D模型下载和使用指南

## 快速开始

### 方法一：手动下载（最简单）

1. **访问MakerWorld网站**
   - 打开 https://makerworld.com.cn/zh/3d-models
   - 搜索或浏览你想要的模型（如"banana cat"）

2. **下载模型文件**
   - 点击进入模型详情页
   - 找到"下载"按钮
   - 选择 **STL格式**（推荐）或OBJ格式
   - 保存到你的下载文件夹

3. **将文件放入Unity项目**
   - 在Unity的Project窗口中，找到 `Assets` 文件夹
   - 如果 `3DModels` 文件夹不存在，创建它：
     - 右键点击 `Assets` → `Create` → `Folder`
     - 命名为 `3DModels`
   - 将下载的 `.stl` 或 `.obj` 文件拖入 `Assets/3DModels` 文件夹

4. **在Unity中加载**
   - 运行场景
   - 点击"3D打印"按钮
   - 系统会自动加载 `Assets/3DModels` 文件夹下的第一个模型文件

### 方法二：使用文件对话框

1. 在Unity中运行场景
2. 点击"3D打印"按钮切换到3D模式
3. 点击"打开项目"按钮
4. 在文件对话框中选择你下载的STL或OBJ文件
5. 如果文件不在 `Assets/3DModels` 文件夹，系统会询问是否复制到该文件夹

### 方法三：使用下载助手（如果知道直接下载链接）

如果你有模型的直接下载链接（.stl文件的URL），可以使用代码下载：

```csharp
// 在Unity中
MakerWorldDownloadHelper helper = GetComponent<MakerWorldDownloadHelper>();
if (helper == null)
{
    helper = gameObject.AddComponent<MakerWorldDownloadHelper>();
}

// 下载模型
string savedPath = await helper.DownloadModelFromUrlAsync(
    "https://makerworld.com.cn/download/model.stl"
);
```

## 详细步骤（以Banana Cat为例）

### 步骤1：在MakerWorld上找到模型

1. 访问 https://makerworld.com.cn/zh/3d-models
2. 搜索"banana cat"或"香蕉猫"
3. 点击进入模型详情页

### 步骤2：下载STL文件

1. 在模型详情页找到"下载"或"文件"按钮
2. 选择STL格式（通常有多个格式可选）
3. 点击下载，文件会保存到浏览器的下载文件夹

### 步骤3：复制到Unity项目

**Windows系统：**
```
1. 打开文件资源管理器
2. 找到下载文件夹（通常在 C:\Users\你的用户名\Downloads）
3. 找到下载的 .stl 文件
4. 复制文件
5. 在Unity的Project窗口中，导航到 Assets/3DModels 文件夹
6. 粘贴文件
```

**Mac系统：**
```
1. 打开Finder
2. 找到下载文件夹
3. 找到下载的 .stl 文件
4. 复制文件
5. 在Unity的Project窗口中，导航到 Assets/3DModels 文件夹
6. 粘贴文件
```

### 步骤4：在Unity中查看

1. 运行Unity场景
2. 点击"3D打印"按钮
3. 模型会自动加载并显示

## 常见问题

### Q: 如何知道文件是否下载成功？
A: 检查 `Assets/3DModels` 文件夹，应该能看到 `.stl` 或 `.obj` 文件

### Q: 支持哪些文件格式？
A: 当前支持：
- **STL**（推荐）- 最常用的3D打印格式
- **OBJ** - 也支持，但可能包含纹理信息

### Q: 可以一次加载多个模型吗？
A: 当前版本每次只加载一个模型。如果要切换模型：
- 将其他模型文件放入 `Assets/3DModels` 文件夹
- 删除或移走当前模型文件
- 重新切换到3D模式，会自动加载新的模型

### Q: 模型文件很大，加载很慢？
A: 
- 大型模型（>10MB）可能需要几秒钟加载
- 可以在Console中查看加载进度
- 如果加载失败，检查文件是否损坏

### Q: 如何打开3D模型文件夹？
A: 可以在代码中调用：
```csharp
MakerWorldDownloadHelper helper = GetComponent<MakerWorldDownloadHelper>();
helper.OpenModelsFolder();
```

## 文件路径说明

- **Unity编辑器模式**：`Assets/3DModels/model.stl`
- **实际文件系统路径**：`项目根目录/Assets/3DModels/model.stl`
- **代码中访问**：`Application.dataPath + "/3DModels/model.stl"`

## 注意事项

1. **文件命名**：避免使用中文字符，可能导致加载失败
2. **文件大小**：建议单个文件不超过50MB
3. **文件格式**：确保是有效的STL或OBJ格式
4. **文件位置**：必须放在 `Assets/3DModels` 文件夹下才能自动加载

## 下一步

- 实现模型列表选择功能
- 支持多模型同时加载
- 添加模型预览缩略图
- 实现从URL直接下载功能

