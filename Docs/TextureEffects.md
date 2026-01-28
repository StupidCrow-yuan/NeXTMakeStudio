# Texture Effects（2.5D 深度/视差）技术文档

本文档说明 NeXTMakeStudio 当前“纹理模式（Texture Modes）”的**原理、处理流程、实现位置、性能建议**以及 **DepthAnything v2（Sentis）** 的接入与配置方式。

---

## 1. 目标与范围

- **目标**：在略缩图（Mini Preview）与全屏预览（Preview Page）中，为指定 craft mode 提供 **2.5D 视觉效果**（视差/浮雕感），并支持导出深度图 PNG。
- **覆盖模式**：
  - `Flat`：无深度信息（不走 2.5D）
  - `Flat Raised`：简单凸起
  - `Pattern Texture`：带细节纹理起伏
  - `Relief Texture`：浮雕（优先 AI 深度：DepthAnything v2；失败回退 CPU）
  - `Customize Texture`：用户上传深度图（必须与图层尺寸一致）

---

## 2. 从图层到最终显示：端到端流程

### 2.1 输入
- 编辑页的图层对象：`UnityEngine.UI.Image`（带 `Sprite`）
- 图层属性：`LayerData.craftMode` / `LayerData.inkMode`

### 2.2 深度图生成（Height/Depth Map）
输出为一张灰度图（0..1），并在最终转换为 `Texture2D`（RGBA32，灰度写入 RGB）。

- **Flat Raised / Pattern Texture**：纯 CPU 生成（快速、确定性、无需模型）
- **Relief Texture**：
  - **优先**：DepthAnything v2（Sentis 推理）
  - **回退**：CPU（亮度/噪声占位，保证在无模型/无 GPU 时仍可用）
- **Customize Texture**：用户上传深度图（不生成）

### 2.3 2.5D 渲染
将：
- 原图作为材质主纹理（Albedo）
- 深度图作为 `_ParallaxMap`

使用视差映射（Parallax Mapping）在摄像机变化时形成“伪立体”效果。

### 2.4 显示位置
- **略缩图（Mini Preview）**：只显示当前选中图层（隔离其它图层）
- **Preview 页**：显示所有图层叠加效果（每层分别走 2.5D 或 Flat clone）

### 2.5 深度图导出（Depth Image）
略缩图右下角按钮 `Depth Image`：
- Editor：弹出保存对话框
- Runtime：保存到 `Application.persistentDataPath`

---

## 3. 模式原理（CPU / AI）详解

### 3.1 Flat Raised（简单凸起）
**理想输入**：带透明通道的 PNG（透明背景、主体不透明）。

- 主要利用 `alpha` 作为“主体遮罩/高度来源”。
- 当输入为 **JPG 或完全不透明**时（alpha=1 全图），仅用 alpha 会导致“整张统一抬高”，所以需要做 fallback（见 5.2）。

### 3.2 Pattern Texture（纹理起伏）
在 alpha 掩膜范围内（如果 alpha 无效则退化为全图范围）：
- 亮度（luminance）提供基础高低
- 叠加少量 PerlinNoise 提供微小纹理细节

### 3.3 Relief Texture（浮雕纹理）
**优先使用真实深度模型**，对照片/复杂纹理效果更稳定。

- Sentis 方式：DepthAnything v2 base ONNX → 推理得到 depth → 归一化到 0..1
- 若模型不可用：回退 CPU（亮度近似 + 少量噪声）

### 3.4 Customize Texture（自定义深度图）
用户上传深度图（png/jpg/webp...）：
- **必须与当前图层 sprite 像素尺寸一致**，否则弹提示并拒绝应用
- 用于 2.5D 时直接作为 `_ParallaxMap`

---

## 4. 关键实现位置（代码导航）

- 枚举与模式判断：`Assets/Scripts/UI/TextureEffects/TextureMode.cs`
  - `TextureModeUtil.TryParseCraftMode()`
  - `TextureModeUtil.IsParallaxMode()`
- 图像读取与裁剪：
  - `TextureReadback.ToReadableTexture(Texture, maxSize)`
  - `SpriteTextureUtil.ExtractSpriteTexture(Sprite, maxSize)`
- 深度图生成：
  - `HeightMapGenerator.GenerateHeightMap(Texture2D, TextureMode)`
- 2.5D quad + 材质：
  - `PreviewMeshBuilder.BuildImageLayerQuad(..., heightOverride, ...)`
  - `ParallaxMaterialUtil.CreateParallaxMaterial(...)`
- 略缩图渲染：`CanvasController.UpdateMiniPreview()`
- Preview 页渲染：`HomeModule.Setup3DDesign(...)`
- 自定义深度上传：`CanvasController.OnUploadDepthMap()`
- 深度图导出：`CanvasController.OnDownloadDepthImage()`

---

## 5. Alpha/JPG 的处理策略（非常重要）

### 5.1 为什么 alpha 会“失效”
- **JPG 没有 alpha 通道**：Unity 里读取到的 alpha 通常恒为 1
- 很多用户图片即便是 PNG，也可能是“整张不透明”（alpha 仍为 1）

### 5.2 当前的工程策略（推荐实践）
当检测到整张图 alpha 几乎恒定（没有有效透明信息）时：
- 将 `mask` 视为 **全图 1**（相当于“主体=整张图”）
- Flat Raised 不再使用纯 alpha 作为高度，而会退化为：
  - 使用 **亮度/对比**来构建“起伏”，避免整张“统一抬高”
- Pattern/Relief 本身就依赖亮度/模型深度，因此对 JPG 更友好

> 真实生产建议：如果你们希望 Flat Raised 对“主体区域”更准确，应引导用户使用带透明背景的 PNG，或提供“背景抠图/自动分割”功能生成 mask。

---

## 6. DepthAnything v2（Sentis）模型配置指南

### 6.1 模型文件是否内置？
当前工程**不内置** ONNX 模型文件。需要团队自行下载并导入 Unity（通常因为模型体积大、版本迭代快、以及避免二次分发问题）。

### 6.2 推荐的资产组织方式
- 将 `.onnx` 放在 `Assets/AI/DepthAnythingV2/`（示例路径）
  - small: `depth_anything_v2_vits.onnx`
  - base: `depth_anything_v2_vitb.onnx`
- Unity 会生成对应的 `ModelAsset`
- 在 `Assets/Resources/DepthAnythingV2Settings.asset` 中配置：
  - `baseOnnxModel`：指向 `ModelAsset`
  - `inputName/outputName`：填 ONNX 实际 I/O 名（用 Netron 查看）
  - `inputSize`：建议 256/384/512（越大越慢但细节更好）
  - `preferGPU`：GPU 可用时优先，失败自动回退 CPU

### 6.3 输入输出名示例（需要以你的 ONNX 为准）
- 常见输入名：`image` / `input` / `x`
- 常见输出名：`predicted_depth` / `output` / `y`

如果名字不匹配，会导致推理输出拿不到（会回退 CPU 伪深度）。

---

## 7. 性能建议（重要）
- **缓存**：
  - Relief（AI）推理建议按（图层纹理 hash + inputSize + 模式）缓存深度图，避免每次刷新都推理。
- **分辨率**：
  - 预览用 256/384 往往足够；512 以上对 CPU 会明显慢。
- **线程/卡顿**：
  - Sentis 推理尽量不要每帧执行；建议在模式切换或图层内容变化时触发。
- **内存**：
  - 导出 PNG/生成深度图会产生临时 Texture2D，注意及时 Destroy（项目当前实现已尽量避免泄漏）。

---

## 8. 团队协作：依赖与 Unity 版本兼容性

### 8.1 不同 Unity 版本会自动匹配 Sentis 吗？
结论：
- **不会“自动跨版本匹配一切”**。Unity Package Manager 会按你的项目 `manifest.json` 去解析依赖；如果某个包在当前环境的包源/版本不可用，就会解析失败。
- 你目前使用的 `2022.3.62f2c1` 环境中，`com.unity.sentis` 可能需要走全球源 `packages.unity.com`（镜像源不一定有）。

### 8.2 本项目的推荐做法
- 我们在 `Packages/manifest.json` 增加了 scoped registry：
  - `com.unity.sentis` → `https://packages.unity.com`
- **不强制在 manifest 中写死 Sentis 依赖版本**（避免不同环境“找不到版本”导致项目打不开）。
  - 团队成员第一次打开项目时，在 Package Manager 里手动安装 `com.unity.sentis` 即可。
- 代码层面使用 `HAS_SENTIS` 宏做了降级：
  - 没装 Sentis 仍可编译运行（Relief Texture 自动回退 CPU 伪深度）。

### 8.3 如果团队必须“开箱即用”（不想手动装包）
建议统一：
- **Unity 版本**（同一条 LTS 分支/同一发行渠道）
- **包源**（能访问 `packages.unity.com` 或公司内网制品源）
- 然后再在 manifest 里固定 `com.unity.sentis` 的具体版本号。


