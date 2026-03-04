using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO.Compression;
using System.Xml;
using PocoRender.Utils;

namespace PocoRender.Core
{
    /// <summary>
    /// 3D模型加载器，支持STL、OBJ、3MF等格式
    /// </summary>
    public class ModelLoader : MonoBehaviour
    {
        public delegate void ModelLoadCallback(GameObject model, string error);

        /// <summary>
        /// 异步加载3D模型
        /// </summary>
        public async Task<GameObject> LoadModelTaskAsync(string modelPath)
        {
            try
            {
                if (!File.Exists(modelPath))
                {
                    Debug.LogError($"模型文件不存在: {modelPath}");
                    return null;
                }

                string extension = Path.GetExtension(modelPath).ToLower();
                GameObject model = null;

                // 在后台线程读取文件
                byte[] fileData = await Task.Run(() => File.ReadAllBytes(modelPath));

                // 回到主线程处理
                await Task.Yield();

                switch (extension)
                {
                    case ".stl":
                        model = LoadSTL(fileData, Path.GetFileNameWithoutExtension(modelPath));
                        break;
                    case ".obj":
                        model = LoadOBJ(fileData, Path.GetFileNameWithoutExtension(modelPath));
                        break;
                    case ".3mf":
                        model = await Load3MFAsync(fileData, Path.GetFileNameWithoutExtension(modelPath));
                        break;
                    default:
                        Debug.LogError($"不支持的模型格式: {extension}");
                        return null;
                }

                if (model != null)
                {
                    Debug.Log($"[ModelLoader] 模型加载成功: {modelPath}");
                }

                return model;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelLoader] 加载模型时发生错误: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// 加载STL文件（ASCII和Binary格式）
        /// </summary>
        private GameObject LoadSTL(byte[] data, string name)
        {
            try
            {
                // 尝试作为ASCII STL解析
                string text = Encoding.ASCII.GetString(data);
                if (text.TrimStart().StartsWith("solid", StringComparison.OrdinalIgnoreCase))
                {
                    return LoadSTL_ASCII(text, name);
                }
                else
                {
                    // 二进制STL
                    return LoadSTL_Binary(data, name);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelLoader] STL解析错误: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 加载ASCII STL文件（改进版本，参考OrcaSlicer的解析方式）
        /// </summary>
        private GameObject LoadSTL_ASCII(string text, string name)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            Vector3 currentNormal = Vector3.zero;
            List<Vector3> currentFace = new List<Vector3>();
            bool inFacet = false;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("solid") || trimmed.StartsWith("endsolid"))
                {
                    continue;
                }
                
                if (trimmed.StartsWith("facet normal"))
                {
                    // 解析法线
                    string[] parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        try
                        {
                            currentNormal = new Vector3(
                                float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[4], System.Globalization.CultureInfo.InvariantCulture)
                            ).normalized;
                            inFacet = true;
                            currentFace.Clear();
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[ModelLoader] 解析法线失败: {trimmed}, 错误: {e.Message}");
                            inFacet = false;
                        }
                    }
                }
                else if (trimmed.StartsWith("vertex") && inFacet)
                {
                    // 解析顶点
                    string[] parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        try
                        {
                            Vector3 vertex = new Vector3(
                                float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture),
                                float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture)
                            );
                            currentFace.Add(vertex);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[ModelLoader] 解析顶点失败: {trimmed}, 错误: {e.Message}");
                        }
                    }
                }
                else if (trimmed.StartsWith("endfacet"))
                {
                    // 完成一个面，创建三角形
                    if (inFacet && currentFace.Count >= 3)
                    {
                        int baseIndex = vertices.Count;
                        vertices.AddRange(currentFace);

                        // STL格式通常每个面是三角形，但有些文件可能包含多边形
                        if (currentFace.Count == 3)
                        {
                            // 标准三角形
                            triangles.Add(baseIndex);
                            triangles.Add(baseIndex + 1);
                            triangles.Add(baseIndex + 2);
                            normals.Add(currentNormal);
                            normals.Add(currentNormal);
                            normals.Add(currentNormal);
                        }
                        else if (currentFace.Count > 3)
                        {
                            // 多边形面，进行三角化（扇形三角化）
                            for (int i = 1; i < currentFace.Count - 1; i++)
                            {
                                triangles.Add(baseIndex);
                                triangles.Add(baseIndex + i);
                                triangles.Add(baseIndex + i + 1);
                                normals.Add(currentNormal);
                                normals.Add(currentNormal);
                                normals.Add(currentNormal);
                            }
                        }
                    }
                    inFacet = false;
                    currentFace.Clear();
                }
            }

            Debug.Log($"[ModelLoader] ASCII STL解析完成: 顶点数={vertices.Count}, 三角形数={triangles.Count / 3}");

            if (vertices.Count == 0 || triangles.Count == 0)
            {
                Debug.LogError("[ModelLoader] ASCII STL文件没有有效的几何数据");
                return null;
            }

            return CreateMeshGameObject(vertices, triangles, normals, name);
        }

        /// <summary>
        /// 加载二进制STL文件
        /// </summary>
        private GameObject LoadSTL_Binary(byte[] data, string name)
        {
            if (data.Length < 80) return null;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector3> normals = new List<Vector3>();

            // 跳过80字节头部
            int offset = 80;
            if (data.Length < offset + 4) return null;

            // 读取三角形数量（注意字节序）
            uint triangleCount = BitConverter.ToUInt32(data, offset);
            
            // 检查字节序（如果数量异常大，可能是字节序问题）
            if (triangleCount > 10000000) // 如果超过1000万个三角形，可能是字节序错误
            {
                // 尝试反转字节序
                byte[] countBytes = new byte[4];
                Array.Copy(data, offset, countBytes, 0, 4);
                Array.Reverse(countBytes);
                triangleCount = BitConverter.ToUInt32(countBytes, 0);
                Debug.LogWarning($"[ModelLoader] 检测到可能的字节序问题，已修正三角形数量: {triangleCount}");
            }
            
            offset += 4;
            
            Debug.Log($"[ModelLoader] 二进制STL: 三角形数量={triangleCount}, 文件大小={data.Length}字节");

            // 每个三角形50字节：12字节法线 + 36字节顶点(3个顶点，每个12字节) + 2字节属性
            int expectedSize = offset + (int)triangleCount * 50;
            if (data.Length < expectedSize)
            {
                Debug.LogWarning($"[ModelLoader] STL文件大小不匹配，期望: {expectedSize}, 实际: {data.Length}");
                // 计算实际能读取的三角形数
                int availableBytes = data.Length - offset;
                int maxTriangles = availableBytes / 50;
                triangleCount = (uint)Mathf.Min(triangleCount, maxTriangles);
                Debug.LogWarning($"[ModelLoader] 调整三角形数量为: {triangleCount}");
            }

            for (uint i = 0; i < triangleCount && offset + 50 <= data.Length; i++)
            {
                // 读取法线（12字节，3个float）
                float nx = ReadFloat(data, offset);
                float ny = ReadFloat(data, offset + 4);
                float nz = ReadFloat(data, offset + 8);
                
                // 验证法线有效性
                if (float.IsNaN(nx) || float.IsNaN(ny) || float.IsNaN(nz) ||
                    float.IsInfinity(nx) || float.IsInfinity(ny) || float.IsInfinity(nz))
                {
                    Debug.LogWarning($"[ModelLoader] 三角形 {i} 的法线无效，跳过");
                    offset += 50;
                    continue;
                }
                
                Vector3 normal = new Vector3(nx, ny, nz);
                if (normal.magnitude > 0.001f)
                {
                    normal = normal.normalized;
                }
                else
                {
                    // 如果法线为零，将在后面重新计算
                    normal = Vector3.zero;
                }
                offset += 12;

                // 读取三个顶点（每个12字节）
                Vector3[] faceVertices = new Vector3[3];
                bool validTriangle = true;
                
                for (int v = 0; v < 3; v++)
                {
                    float vx = ReadFloat(data, offset);
                    float vy = ReadFloat(data, offset + 4);
                    float vz = ReadFloat(data, offset + 8);
                    
                    // 验证顶点有效性
                    if (float.IsNaN(vx) || float.IsNaN(vy) || float.IsNaN(vz) ||
                        float.IsInfinity(vx) || float.IsInfinity(vy) || float.IsInfinity(vz))
                    {
                        Debug.LogWarning($"[ModelLoader] 三角形 {i} 的顶点 {v} 无效，跳过");
                        validTriangle = false;
                        break;
                    }
                    
                    faceVertices[v] = new Vector3(vx, vy, vz);
                    offset += 12;
                }
                
                if (!validTriangle)
                {
                    // 跳过2字节属性
                    offset += 2;
                    continue;
                }
                
                // 检查是否为退化三角形（三个顶点共线或相同）
                Vector3 edge1 = faceVertices[1] - faceVertices[0];
                Vector3 edge2 = faceVertices[2] - faceVertices[0];
                Vector3 cross = Vector3.Cross(edge1, edge2);
                
                if (cross.magnitude < 0.000001f)
                {
                    Debug.LogWarning($"[ModelLoader] 三角形 {i} 是退化三角形，跳过");
                    offset += 2;
                    continue;
                }
                
                // 如果法线为零，从三角形计算法线
                if (normal.magnitude < 0.001f)
                {
                    normal = cross.normalized;
                }
                
                // 添加顶点和三角形
                int baseIndex = vertices.Count;
                for (int v = 0; v < 3; v++)
                {
                    vertices.Add(faceVertices[v]);
                    normals.Add(normal);
                }

                // 添加三角形（保持原始顺序）
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);

                // 跳过2字节属性
                offset += 2;
            }
            
            Debug.Log($"[ModelLoader] 二进制STL解析完成: 顶点数={vertices.Count}, 三角形数={triangles.Count / 3}");

            return CreateMeshGameObject(vertices, triangles, normals, name);
        }
        
        /// <summary>
        /// 读取float（处理字节序）
        /// </summary>
        float ReadFloat(byte[] data, int offset)
        {
            if (offset + 4 > data.Length) return 0f;
            
            // 检查是否需要反转字节序
            // 如果读取的值异常大或小，可能需要反转
            float value = BitConverter.ToSingle(data, offset);
            
            // 如果值异常，尝试反转字节序
            if (float.IsNaN(value) || float.IsInfinity(value) || Mathf.Abs(value) > 1e10f)
            {
                byte[] bytes = new byte[4];
                Array.Copy(data, offset, bytes, 0, 4);
                Array.Reverse(bytes);
                value = BitConverter.ToSingle(bytes, 0);
            }
            
            return value;
        }

        /// <summary>
        /// 加载OBJ文件
        /// </summary>
        private GameObject LoadOBJ(byte[] data, string name)
        {
            string text = Encoding.UTF8.GetString(data);
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();

            string[] lines = text.Split('\n');
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                string[] parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "v":
                        // 顶点
                        if (parts.Length >= 4)
                        {
                            vertices.Add(new Vector3(
                                float.Parse(parts[1]),
                                float.Parse(parts[2]),
                                float.Parse(parts[3])
                            ));
                        }
                        break;
                    case "vt":
                        // 纹理坐标
                        if (parts.Length >= 3)
                        {
                            uvs.Add(new Vector2(
                                float.Parse(parts[1]),
                                float.Parse(parts[2])
                            ));
                        }
                        break;
                    case "vn":
                        // 法线
                        if (parts.Length >= 4)
                        {
                            normals.Add(new Vector3(
                                float.Parse(parts[1]),
                                float.Parse(parts[2]),
                                float.Parse(parts[3])
                            ));
                        }
                        break;
                    case "f":
                        // 面
                        if (parts.Length >= 4)
                        {
                            // 解析面的索引（支持 v/vt/vn 格式）
                            List<int> faceIndices = new List<int>();
                            for (int i = 1; i < parts.Length; i++)
                            {
                                string[] indices = parts[i].Split('/');
                                int vertexIndex = int.Parse(indices[0]) - 1; // OBJ索引从1开始
                                faceIndices.Add(vertexIndex);
                            }

                            // 三角化（简单扇形三角化）
                            if (faceIndices.Count >= 3)
                            {
                                for (int i = 1; i < faceIndices.Count - 1; i++)
                                {
                                    triangles.Add(faceIndices[0]);
                                    triangles.Add(faceIndices[i]);
                                    triangles.Add(faceIndices[i + 1]);
                                }
                            }
                        }
                        break;
                }
            }

            // 如果没有法线，计算法线
            if (normals.Count == 0)
            {
                normals = CalculateNormals(vertices, triangles);
            }

            return CreateMeshGameObject(vertices, triangles, normals, name, uvs.Count > 0 ? uvs : null);
        }

        /// <summary>
        /// 计算法线
        /// </summary>
        private List<Vector3> CalculateNormals(List<Vector3> vertices, List<int> triangles)
        {
            List<Vector3> normals = new List<Vector3>(new Vector3[vertices.Count]);

            for (int i = 0; i < triangles.Count; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                Vector3 v0 = vertices[i0];
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];

                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                normals[i0] += normal;
                normals[i1] += normal;
                normals[i2] += normal;
            }

            // 归一化
            for (int i = 0; i < normals.Count; i++)
            {
                normals[i] = normals[i].normalized;
            }

            return normals;
        }

        /// <summary>
        /// 创建网格GameObject（参考OrcaSlicer的网格构建方式）
        /// 使用空间哈希表合并重复顶点，正确计算法线，确保网格完整性
        /// </summary>
        private GameObject CreateMeshGameObject(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, string name, List<Vector2> uvs = null)
        {
            if (vertices.Count == 0 || triangles.Count == 0)
            {
                Debug.LogError("[ModelLoader] 模型没有顶点或三角形");
                return null;
            }

            Debug.Log($"[ModelLoader] 原始数据 - 顶点数: {vertices.Count}, 三角形数: {triangles.Count / 3}");

            // 使用空间哈希表合并重复顶点（参考OrcaSlicer的TriangleMesh实现）
            // 这是关键步骤，确保相邻三角形共享的顶点被正确合并
            List<Vector3> mergedVertices;
            List<int> mergedTriangles;
            List<Vector3> mergedNormals;
            List<Vector2> mergedUVs;
            
            MergeVerticesWithSpatialHash(vertices, triangles, normals, uvs, 
                out mergedVertices, out mergedTriangles, out mergedNormals, out mergedUVs);

            Debug.Log($"[ModelLoader] 合并后 - 顶点数: {mergedVertices.Count} (减少 {vertices.Count - mergedVertices.Count}), 三角形数: {mergedTriangles.Count / 3}");

            if (mergedVertices.Count == 0 || mergedTriangles.Count == 0)
            {
                Debug.LogError("[ModelLoader] 合并后没有有效的顶点或三角形");
                return null;
            }

            GameObject model = new GameObject(name);
            MeshFilter meshFilter = model.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = model.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            
            // 设置顶点
            mesh.vertices = mergedVertices.ToArray();
            mesh.triangles = mergedTriangles.ToArray();
            
            // 设置法线（如果合并后法线数量匹配）
            if (mergedNormals != null && mergedNormals.Count == mergedVertices.Count)
            {
                mesh.normals = mergedNormals.ToArray();
            }
            else
            {
                // 重新计算法线（基于合并后的网格）
                mesh.RecalculateNormals();
            }

            // 设置UV
            if (mergedUVs != null && mergedUVs.Count == mergedVertices.Count)
            {
                mesh.uv = mergedUVs.ToArray();
            }

            // 重新计算边界和切线
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            
            // 验证网格完整性（参考BambuStudio的网格验证）
            ValidateMesh(mesh, name);

            meshFilter.mesh = mesh;

            // 创建默认材质
            Material material = SafeShaderHelper.CreateStandardMaterial();
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            material.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.2f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.6f);
            
            // 使用单面渲染（默认），保持正确的面朝向
            // 如果需要看到背面，可以取消注释下面这行
            // material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            
            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            
            Debug.Log($"[ModelLoader] 创建模型 '{name}': 顶点数={mesh.vertexCount}, 三角形数={mesh.triangles.Length / 3}, 边界={mesh.bounds}");

            return model;
        }

        /// <summary>
        /// 使用空间哈希表合并重复顶点（参考OrcaSlicer的实现）
        /// 这种方法比简单的距离比较更高效和准确
        /// </summary>
        private void MergeVerticesWithSpatialHash(
            List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs,
            out List<Vector3> mergedVertices, out List<int> mergedTriangles, 
            out List<Vector3> mergedNormals, out List<Vector2> mergedUVs)
        {
            mergedVertices = new List<Vector3>();
            mergedTriangles = new List<int>();
            mergedNormals = new List<Vector3>();
            mergedUVs = new List<Vector2>();

            // 精度阈值（参考BambuStudio/PrusaSlicer的实现）
            // STL文件通常精度在0.001mm到0.01mm，使用更宽松的阈值确保共享顶点被正确合并
            // 这个值需要平衡：太小会导致应该合并的顶点没合并（镂空），太大会导致不应该合并的顶点被合并（变形）
            const float epsilon = 0.0001f; // 0.1微米，适合大多数STL文件
            
            // 使用字典存储顶点索引映射（原始索引 -> 合并后索引）
            Dictionary<int, int> vertexMap = new Dictionary<int, int>();
            
            // 空间哈希表：将空间划分为网格，相同网格内的顶点可能重复
            // 使用坐标的整数部分作为哈希键
            Dictionary<long, List<int>> spatialHash = new Dictionary<long, List<int>>();
            
            // 第一步：为每个原始顶点找到或创建合并后的顶点
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 计算空间哈希键（将坐标量化到网格）
                long hashKey = GetSpatialHashKey(vertex, epsilon);
                
                int mergedIndex = -1;
                
                // 检查空间哈希表中是否有相近的顶点
                // 不仅要检查当前网格单元，还要检查相邻的26个网格单元（3x3x3）
                // 这确保即使顶点在网格边界上也能被正确找到
                List<long> hashKeysToCheck = new List<long> { hashKey };
                
                // 添加相邻网格单元的哈希键
                int x = Mathf.FloorToInt(vertex.x / epsilon);
                int y = Mathf.FloorToInt(vertex.y / epsilon);
                int z = Mathf.FloorToInt(vertex.z / epsilon);
                
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            if (dx == 0 && dy == 0 && dz == 0) continue;
                            long neighborKey = ((long)(x + dx) << 32) ^ ((long)(y + dy) << 16) ^ (long)(z + dz);
                            hashKeysToCheck.Add(neighborKey);
                        }
                    }
                }
                
                // 在所有相关网格单元中查找相近顶点
                foreach (long keyToCheck in hashKeysToCheck)
                {
                    if (spatialHash.ContainsKey(keyToCheck))
                    {
                        foreach (int candidateIndex in spatialHash[keyToCheck])
                        {
                            Vector3 candidate = mergedVertices[candidateIndex];
                            
                            // 使用平方距离比较，避免开方运算
                            Vector3 diff = vertex - candidate;
                            float sqrDistance = diff.sqrMagnitude;
                            float sqrEpsilon = epsilon * epsilon;
                            
                            if (sqrDistance < sqrEpsilon)
                            {
                                // 找到重复顶点，使用现有索引
                                mergedIndex = candidateIndex;
                                
                                // 合并法线（加权平均，考虑距离）
                                if (normals != null && i < normals.Count && mergedNormals.Count > candidateIndex)
                                {
                                    Vector3 normal1 = normals[i];
                                    Vector3 normal2 = mergedNormals[candidateIndex];
                                    
                                    // 如果法线方向相似，直接平均；如果相反，可能需要特殊处理
                                    float normalDot = Vector3.Dot(normal1.normalized, normal2.normalized);
                                    if (normalDot > -0.5f) // 法线方向不相反
                                    {
                                        mergedNormals[candidateIndex] = (normal1 + normal2).normalized;
                                    }
                                    else
                                    {
                                        // 法线方向相反，选择更接近原始法线的方向
                                        mergedNormals[candidateIndex] = normal1;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    
                    if (mergedIndex != -1) break;
                }
                
                if (mergedIndex == -1)
                {
                    // 新顶点，添加到合并列表
                    mergedIndex = mergedVertices.Count;
                    mergedVertices.Add(vertex);
                    
                    // 添加法线
                    if (normals != null && i < normals.Count)
                    {
                        mergedNormals.Add(normals[i]);
                    }
                    else
                    {
                        mergedNormals.Add(Vector3.zero);
                    }
                    
                    // 添加UV
                    if (uvs != null && i < uvs.Count)
                    {
                        mergedUVs.Add(uvs[i]);
                    }
                    else
                    {
                        mergedUVs.Add(Vector2.zero);
                    }
                    
                    // 添加到空间哈希表
                    if (!spatialHash.ContainsKey(hashKey))
                    {
                        spatialHash[hashKey] = new List<int>();
                    }
                    spatialHash[hashKey].Add(mergedIndex);
                }
                
                vertexMap[i] = mergedIndex;
            }
            
            // 第二步：重新映射三角形索引
            for (int i = 0; i < triangles.Count; i += 3)
            {
                if (i + 2 >= triangles.Count) break;
                
                int idx0 = triangles[i];
                int idx1 = triangles[i + 1];
                int idx2 = triangles[i + 2];
                
                // 检查索引有效性
                if (idx0 < 0 || idx0 >= vertices.Count ||
                    idx1 < 0 || idx1 >= vertices.Count ||
                    idx2 < 0 || idx2 >= vertices.Count)
                {
                    Debug.LogWarning($"[ModelLoader] 无效的三角形索引: {idx0}, {idx1}, {idx2}");
                    continue;
                }
                
                // 获取合并后的索引
                int newIdx0 = vertexMap[idx0];
                int newIdx1 = vertexMap[idx1];
                int newIdx2 = vertexMap[idx2];
                
                // 检查是否为退化三角形（三个顶点相同或共线）
                if (newIdx0 == newIdx1 || newIdx1 == newIdx2 || newIdx2 == newIdx0)
                {
                    Debug.LogWarning($"[ModelLoader] 过滤退化三角形: {newIdx0}, {newIdx1}, {newIdx2}");
                    continue;
                }
                
                // 验证三角形方向（确保法线方向正确）
                Vector3 v0 = mergedVertices[newIdx0];
                Vector3 v1 = mergedVertices[newIdx1];
                Vector3 v2 = mergedVertices[newIdx2];
                
                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector3 calculatedNormal = Vector3.Cross(edge1, edge2);
                
                // 如果三角形面积太小，跳过
                if (calculatedNormal.magnitude < epsilon * epsilon)
                {
                    Debug.LogWarning($"[ModelLoader] 过滤面积过小的三角形");
                    continue;
                }
                
                // 检查法线方向（参考BambuStudio的实现）
                // 只有在法线明显相反时才反转三角形，避免过度修正
                bool reverseTriangle = false;
                if (mergedNormals.Count > newIdx0 && mergedNormals[newIdx0].magnitude > 0.1f)
                {
                    Vector3 originalNormal = mergedNormals[newIdx0];
                    Vector3 normalizedCalculated = calculatedNormal.normalized;
                    float dot = Vector3.Dot(normalizedCalculated, originalNormal.normalized);
                    
                    // 只有在法线明显相反（dot < -0.7）时才反转，避免误判
                    if (dot < -0.7f)
                    {
                        reverseTriangle = true;
                    }
                    else if (dot < 0.3f && dot > -0.3f)
                    {
                        // 法线几乎垂直，使用计算的法线
                        mergedNormals[newIdx0] = normalizedCalculated;
                        if (mergedNormals.Count > newIdx1) mergedNormals[newIdx1] = normalizedCalculated;
                        if (mergedNormals.Count > newIdx2) mergedNormals[newIdx2] = normalizedCalculated;
                    }
                }
                else
                {
                    // 如果没有原始法线，使用计算的法线
                    Vector3 normalizedCalculated = calculatedNormal.normalized;
                    if (mergedNormals.Count > newIdx0) mergedNormals[newIdx0] = normalizedCalculated;
                    if (mergedNormals.Count > newIdx1) mergedNormals[newIdx1] = normalizedCalculated;
                    if (mergedNormals.Count > newIdx2) mergedNormals[newIdx2] = normalizedCalculated;
                }
                
                // 添加三角形（保持一致的绕序）
                if (reverseTriangle)
                {
                    mergedTriangles.Add(newIdx0);
                    mergedTriangles.Add(newIdx2);
                    mergedTriangles.Add(newIdx1);
                }
                else
                {
                    mergedTriangles.Add(newIdx0);
                    mergedTriangles.Add(newIdx1);
                    mergedTriangles.Add(newIdx2);
                }
            }
            
            // 归一化所有法线
            for (int i = 0; i < mergedNormals.Count; i++)
            {
                if (mergedNormals[i].magnitude > 0.001f)
                {
                    mergedNormals[i] = mergedNormals[i].normalized;
                }
                else
                {
                    // 如果法线为零，将在后面重新计算
                    mergedNormals[i] = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 计算空间哈希键（将3D坐标映射到整数网格）
        /// 参考BambuStudio/PrusaSlicer的实现，使用更稳定的哈希函数
        /// </summary>
        private long GetSpatialHashKey(Vector3 vertex, float epsilon)
        {
            // 将坐标量化到网格（使用Floor确保一致性）
            int x = Mathf.FloorToInt(vertex.x / epsilon);
            int y = Mathf.FloorToInt(vertex.y / epsilon);
            int z = Mathf.FloorToInt(vertex.z / epsilon);
            
            // 使用更稳定的哈希函数（避免整数溢出）
            // 将坐标映射到合理的范围
            unchecked
            {
                long hash = 17;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + z;
                return hash;
            }
        }
        
        /// <summary>
        /// 验证网格完整性（参考BambuStudio的网格验证）
        /// 检查是否有非流形边、孤立顶点等问题
        /// </summary>
        private void ValidateMesh(Mesh mesh, string name)
        {
            if (mesh == null) return;
            
            int vertexCount = mesh.vertexCount;
            int triangleCount = mesh.triangles.Length / 3;
            
            // 检查基本统计
            if (vertexCount == 0 || triangleCount == 0)
            {
                Debug.LogError($"[ModelLoader] 网格 '{name}' 没有有效的几何数据");
                return;
            }
            
            // 检查边界
            Bounds bounds = mesh.bounds;
            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning($"[ModelLoader] 网格 '{name}' 的边界大小为0");
            }
            
            // 检查是否有孤立顶点（没有被任何三角形使用的顶点）
            bool[] vertexUsed = new bool[vertexCount];
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i++)
            {
                int idx = triangles[i];
                if (idx >= 0 && idx < vertexCount)
                {
                    vertexUsed[idx] = true;
                }
            }
            
            int isolatedVertexCount = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                if (!vertexUsed[i])
                {
                    isolatedVertexCount++;
                }
            }
            
            if (isolatedVertexCount > 0)
            {
                Debug.LogWarning($"[ModelLoader] 网格 '{name}' 有 {isolatedVertexCount} 个孤立顶点（未被任何三角形使用）");
            }
            
            // 检查三角形索引有效性
            int invalidTriangleCount = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (i + 2 >= triangles.Length) break;
                
                int idx0 = triangles[i];
                int idx1 = triangles[i + 1];
                int idx2 = triangles[i + 2];
                
                if (idx0 < 0 || idx0 >= vertexCount ||
                    idx1 < 0 || idx1 >= vertexCount ||
                    idx2 < 0 || idx2 >= vertexCount ||
                    idx0 == idx1 || idx1 == idx2 || idx2 == idx0)
                {
                    invalidTriangleCount++;
                }
            }
            
            if (invalidTriangleCount > 0)
            {
                Debug.LogWarning($"[ModelLoader] 网格 '{name}' 有 {invalidTriangleCount} 个无效三角形");
            }
            
            Debug.Log($"[ModelLoader] 网格验证完成 '{name}': 顶点={vertexCount}, 三角形={triangleCount}, 边界={bounds.size}, 孤立顶点={isolatedVertexCount}, 无效三角形={invalidTriangleCount}");
        }
        
        /// <summary>
        /// 递归查找mesh节点
        /// </summary>
        private void FindMeshNodesRecursive(XmlNode node, List<XmlNode> result)
        {
            if (node == null) return;
            
            if (node.LocalName == "mesh" || node.Name == "mesh" || 
                (node.Name.Contains("mesh") && node.HasChildNodes))
            {
                result.Add(node);
            }
            
            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    FindMeshNodesRecursive(child, result);
                }
            }
        }
        
        /// <summary>
        /// 解析单个mesh节点
        /// </summary>
        private void ParseSingleMeshNode(XmlNode meshNode, XmlNamespaceManager nsmgr, 
            List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Color> colors)
        {
            if (meshNode == null) return;
            
            // 解析顶点 - 尝试多种方式
            XmlNode verticesNode = null;
            
            // 方法1: 直接子节点
            foreach (XmlNode child in meshNode.ChildNodes)
            {
                if (child.LocalName == "vertices" || child.Name == "vertices" || 
                    child.Name.EndsWith(":vertices"))
                {
                    verticesNode = child;
                    break;
                }
            }
            
            // 方法2: XPath查询
            if (verticesNode == null)
            {
                verticesNode = meshNode.SelectSingleNode(".//vertices");
            }
            if (verticesNode == null)
            {
                verticesNode = meshNode.SelectSingleNode(".//m:vertices", nsmgr);
            }
            if (verticesNode == null)
            {
                verticesNode = meshNode.SelectSingleNode(".//*[local-name()='vertices']");
            }
            
            if (verticesNode != null)
            {
                int baseVertexIndex = vertices.Count;
                
                // 查找所有vertex节点
                List<XmlNode> vertexNodes = new List<XmlNode>();
                foreach (XmlNode child in verticesNode.ChildNodes)
                {
                    if (child.LocalName == "vertex" || child.Name == "vertex" || 
                        child.Name.EndsWith(":vertex"))
                    {
                        vertexNodes.Add(child);
                    }
                }
                
                // 如果直接子节点没找到，尝试XPath
                if (vertexNodes.Count == 0)
                {
                    XmlNodeList tempList = verticesNode.SelectNodes(".//vertex");
                    if (tempList != null)
                    {
                        foreach (XmlNode n in tempList)
                        {
                            vertexNodes.Add(n);
                        }
                    }
                }
                if (vertexNodes.Count == 0)
                {
                    XmlNodeList tempList = verticesNode.SelectNodes(".//m:vertex", nsmgr);
                    if (tempList != null)
                    {
                        foreach (XmlNode n in tempList)
                        {
                            vertexNodes.Add(n);
                        }
                    }
                }
                
                foreach (XmlNode vertexNode in vertexNodes)
                {
                    XmlAttribute xAttr = vertexNode.Attributes["x"];
                    XmlAttribute yAttr = vertexNode.Attributes["y"];
                    XmlAttribute zAttr = vertexNode.Attributes["z"];
                    
                    if (xAttr != null && yAttr != null && zAttr != null)
                    {
                        try
                        {
                            float x = float.Parse(xAttr.Value, System.Globalization.CultureInfo.InvariantCulture);
                            float y = float.Parse(yAttr.Value, System.Globalization.CultureInfo.InvariantCulture);
                            float z = float.Parse(zAttr.Value, System.Globalization.CultureInfo.InvariantCulture);
                            
                            vertices.Add(new Vector3(x, y, z));
                            colors.Add(Color.white); // 默认白色
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[ModelLoader] 解析顶点失败: {e.Message}");
                        }
                    }
                }
                
                // 解析三角形
                XmlNode trianglesNode = null;
                
                // 方法1: 直接子节点
                foreach (XmlNode child in meshNode.ChildNodes)
                {
                    if (child.LocalName == "triangles" || child.Name == "triangles" || 
                        child.Name.EndsWith(":triangles"))
                    {
                        trianglesNode = child;
                        break;
                    }
                }
                
                // 方法2: XPath查询
                if (trianglesNode == null)
                {
                    trianglesNode = meshNode.SelectSingleNode(".//triangles");
                }
                if (trianglesNode == null)
                {
                    trianglesNode = meshNode.SelectSingleNode(".//m:triangles", nsmgr);
                }
                if (trianglesNode == null)
                {
                    trianglesNode = meshNode.SelectSingleNode(".//*[local-name()='triangles']");
                }
                
                if (trianglesNode != null)
                {
                    List<XmlNode> triangleNodes = new List<XmlNode>();
                    foreach (XmlNode child in trianglesNode.ChildNodes)
                    {
                        if (child.LocalName == "triangle" || child.Name == "triangle" || 
                            child.Name.EndsWith(":triangle"))
                        {
                            triangleNodes.Add(child);
                        }
                    }
                    
                    // 如果直接子节点没找到，尝试XPath
                    if (triangleNodes.Count == 0)
                    {
                        XmlNodeList tempList = trianglesNode.SelectNodes(".//triangle");
                        if (tempList != null)
                        {
                            foreach (XmlNode n in tempList)
                            {
                                triangleNodes.Add(n);
                            }
                        }
                    }
                    if (triangleNodes.Count == 0)
                    {
                        XmlNodeList tempList = trianglesNode.SelectNodes(".//m:triangle", nsmgr);
                        if (tempList != null)
                        {
                            foreach (XmlNode n in tempList)
                            {
                                triangleNodes.Add(n);
                            }
                        }
                    }
                    
                    foreach (XmlNode triangleNode in triangleNodes)
                    {
                        XmlAttribute v1Attr = triangleNode.Attributes["v1"];
                        XmlAttribute v2Attr = triangleNode.Attributes["v2"];
                        XmlAttribute v3Attr = triangleNode.Attributes["v3"];
                        
                        if (v1Attr != null && v2Attr != null && v3Attr != null)
                        {
                            try
                            {
                                int v1 = int.Parse(v1Attr.Value) + baseVertexIndex;
                                int v2 = int.Parse(v2Attr.Value) + baseVertexIndex;
                                int v3 = int.Parse(v3Attr.Value) + baseVertexIndex;
                                
                                // 检查索引有效性
                                if (v1 >= 0 && v1 < vertices.Count &&
                                    v2 >= 0 && v2 < vertices.Count &&
                                    v3 >= 0 && v3 < vertices.Count &&
                                    v1 != v2 && v2 != v3 && v3 != v1)
                                {
                                    triangles.Add(v1);
                                    triangles.Add(v2);
                                    triangles.Add(v3);
                                }
                                else
                                {
                                    Debug.LogWarning($"[ModelLoader] 无效的三角形索引: v1={v1Attr.Value}, v2={v2Attr.Value}, v3={v3Attr.Value}, baseIndex={baseVertexIndex}, totalVertices={vertices.Count}");
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning($"[ModelLoader] 解析三角形失败: {e.Message}");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 加载3MF文件（3D Manufacturing Format）
        /// 3MF是基于ZIP的XML格式，包含3D模型、材料、纹理等信息
        /// </summary>
        private async Task<GameObject> Load3MFAsync(byte[] data, string name)
        {
            try
            {
                Debug.Log($"[ModelLoader] 开始解析3MF文件: {name}");
                
                // 3MF文件是一个ZIP压缩包
                using (MemoryStream zipStream = new MemoryStream(data))
                {
                    using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                    {
                        // 查找3D模型文件（通常是3D/3dmodel.model）
                        ZipArchiveEntry modelEntry = null;
                        
                        // 尝试常见的3MF模型文件路径
                        string[] possiblePaths = {
                            "3D/3dmodel.model",
                            "3dmodel.model",
                            "Models/3dmodel.model"
                        };
                        
                        foreach (string path in possiblePaths)
                        {
                            modelEntry = archive.GetEntry(path);
                            if (modelEntry != null)
                            {
                                Debug.Log($"[ModelLoader] 找到3MF模型文件: {path}");
                                break;
                            }
                        }
                        
                        // 如果没找到，查找所有.model文件
                        if (modelEntry == null)
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                if (entry.Name.EndsWith(".model", StringComparison.OrdinalIgnoreCase))
                                {
                                    modelEntry = entry;
                                    Debug.Log($"[ModelLoader] 找到3MF模型文件: {entry.FullName}");
                                    break;
                                }
                            }
                        }
                        
                        if (modelEntry == null)
                        {
                            Debug.LogError("[ModelLoader] 3MF文件中未找到模型文件");
                            return null;
                        }
                        
                        // 读取XML模型数据
                        string xmlContent = null;
                        await Task.Run(() =>
                        {
                            using (Stream entryStream = modelEntry.Open())
                            using (StreamReader reader = new StreamReader(entryStream))
                            {
                                xmlContent = reader.ReadToEnd();
                            }
                        });
                        
                        // 解析XML并提取3D模型数据
                        return Parse3MFModel(xmlContent, name, archive);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelLoader] 3MF解析错误: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// 解析3MF XML模型数据
        /// </summary>
        private GameObject Parse3MFModel(string xmlContent, string name, ZipArchive archive)
        {
            try
            {
                List<Vector3> vertices = new List<Vector3>();
                List<int> triangles = new List<int>();
                List<Vector3> normals = new List<Vector3>();
                List<Color> colors = new List<Color>();
                
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);
                
                // 输出XML内容的前5000个字符用于调试
                string xmlPreview = xmlContent.Length > 5000 ? xmlContent.Substring(0, 5000) : xmlContent;
                Debug.Log($"[ModelLoader] ========== 3MF XML内容预览 (前5000字符) ==========");
                Debug.Log($"{xmlPreview}...");
                Debug.Log($"[ModelLoader] =================================================");
                
                // 3MF使用XML命名空间，但不同版本可能使用不同的命名空间
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                string defaultNS = doc.DocumentElement?.NamespaceURI ?? "http://schemas.microsoft.com/3dmanufacturing/core/2015/02";
                nsmgr.AddNamespace("m", defaultNS);
                nsmgr.AddNamespace("default", defaultNS);
                nsmgr.AddNamespace("p", "http://schemas.microsoft.com/3dmanufacturing/production/2015/06");
                
                Debug.Log($"[ModelLoader] 默认命名空间: {defaultNS}");
                Debug.Log($"[ModelLoader] XML根节点: {doc.DocumentElement?.Name}, 命名空间: {doc.DocumentElement?.NamespaceURI}");
                
                // 先输出XML结构
                Debug.Log("[ModelLoader] ========== XML结构 (前5层) ==========");
                if (doc.DocumentElement != null)
                {
                    OutputXmlStructure(doc.DocumentElement, 0, 5);
                }
                else
                {
                    Debug.LogError("[ModelLoader] DocumentElement为null!");
                }
                Debug.Log("[ModelLoader] =====================================");
                
                // 尝试多种方式查找mesh对象
                XmlNodeList meshNodes = null;
                
                // 方法1: 使用命名空间（标准3MF格式）
                try
                {
                    meshNodes = doc.SelectNodes("//m:mesh", nsmgr);
                    Debug.Log($"[ModelLoader] 方法1 (//m:mesh): 找到 {meshNodes?.Count ?? 0} 个mesh");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ModelLoader] 方法1失败: {e.Message}");
                }
                
                // 方法2: 使用local-name（忽略命名空间）
                if (meshNodes == null || meshNodes.Count == 0)
                {
                    try
                    {
                        meshNodes = doc.SelectNodes("//*[local-name()='mesh']");
                        Debug.Log($"[ModelLoader] 方法2 (//*[local-name()='mesh']): 找到 {meshNodes?.Count ?? 0} 个mesh");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ModelLoader] 方法2失败: {e.Message}");
                    }
                }
                
                // 方法3: 查找object下的mesh（3MF常见结构）
                if (meshNodes == null || meshNodes.Count == 0)
                {
                    try
                    {
                        meshNodes = doc.SelectNodes("//m:object//m:mesh", nsmgr);
                        Debug.Log($"[ModelLoader] 方法3 (//m:object//m:mesh): 找到 {meshNodes?.Count ?? 0} 个mesh");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ModelLoader] 方法3失败: {e.Message}");
                    }
                }
                
                // 方法4: 查找resources下的mesh
                if (meshNodes == null || meshNodes.Count == 0)
                {
                    try
                    {
                        meshNodes = doc.SelectNodes("//m:resources//m:mesh", nsmgr);
                        Debug.Log($"[ModelLoader] 方法4 (//m:resources//m:mesh): 找到 {meshNodes?.Count ?? 0} 个mesh");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ModelLoader] 方法4失败: {e.Message}");
                    }
                }
                
                // 方法5: 不使用命名空间
                if (meshNodes == null || meshNodes.Count == 0)
                {
                    try
                    {
                        meshNodes = doc.SelectNodes("//mesh");
                        Debug.Log($"[ModelLoader] 方法5 (//mesh): 找到 {meshNodes?.Count ?? 0} 个mesh");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ModelLoader] 方法5失败: {e.Message}");
                    }
                }
                
                // 方法6: 查找所有object节点，然后在其下查找mesh
                if (meshNodes == null || meshNodes.Count == 0)
                {
                    try
                    {
                        XmlNodeList objectNodes = doc.SelectNodes("//m:object", nsmgr);
                        Debug.Log($"[ModelLoader] 方法6: 找到 {objectNodes?.Count ?? 0} 个object节点");
                        
                        if (objectNodes != null && objectNodes.Count > 0)
                        {
                            List<XmlNode> allMeshes = new List<XmlNode>();
                            foreach (XmlNode objNode in objectNodes)
                            {
                                Debug.Log($"[ModelLoader] 检查object节点: {objNode.Name}, 子节点数: {objNode.ChildNodes.Count}");
                                
                                // 尝试多种方式在object下查找mesh
                                XmlNodeList objMeshes = objNode.SelectNodes(".//m:mesh", nsmgr);
                                if (objMeshes != null && objMeshes.Count > 0)
                                {
                                    Debug.Log($"[ModelLoader] 在object下找到 {objMeshes.Count} 个mesh");
                                    foreach (XmlNode m in objMeshes)
                                    {
                                        allMeshes.Add(m);
                                    }
                                }
                                
                                // 也尝试不使用命名空间
                                if (objMeshes == null || objMeshes.Count == 0)
                                {
                                    objMeshes = objNode.SelectNodes(".//*[local-name()='mesh']");
                                    if (objMeshes != null && objMeshes.Count > 0)
                                    {
                                        Debug.Log($"[ModelLoader] 在object下(使用local-name)找到 {objMeshes.Count} 个mesh");
                                        foreach (XmlNode m in objMeshes)
                                        {
                                            allMeshes.Add(m);
                                        }
                                    }
                                }
                            }
                            
                            if (allMeshes.Count > 0)
                            {
                                Debug.Log($"[ModelLoader] 方法6 (object下查找): 总共找到 {allMeshes.Count} 个mesh");
                                // 直接解析这些mesh
                                foreach (XmlNode meshNode in allMeshes)
                                {
                                    ParseSingleMeshNode(meshNode, nsmgr, vertices, triangles, normals, colors);
                                }
                                if (vertices.Count > 0 && triangles.Count > 0)
                                {
                                    goto CreateMesh; // 跳转到创建网格
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ModelLoader] 方法6失败: {e.Message}");
                    }
                }
                
                // 方法6: 查找object节点，然后在其下查找mesh
                if (meshNodes == null || meshNodes.Count == 0)
                {
                    XmlNodeList objectNodes = doc.SelectNodes("//m:object", nsmgr);
                    if (objectNodes != null && objectNodes.Count > 0)
                    {
                        Debug.Log($"[ModelLoader] 找到 {objectNodes.Count} 个object节点，尝试在其下查找mesh");
                        foreach (XmlNode objNode in objectNodes)
                        {
                            XmlNodeList objMeshes = objNode.SelectNodes(".//m:mesh", nsmgr);
                            if (objMeshes != null && objMeshes.Count > 0)
                            {
                                if (meshNodes == null)
                                {
                                    // 创建一个临时列表来收集所有mesh
                                    List<XmlNode> allMeshes = new List<XmlNode>();
                                    foreach (XmlNode m in objMeshes)
                                    {
                                        allMeshes.Add(m);
                                    }
                                    // 继续查找其他object下的mesh
                                    foreach (XmlNode otherObj in objectNodes)
                                    {
                                        if (otherObj != objNode)
                                        {
                                            XmlNodeList otherMeshes = otherObj.SelectNodes(".//m:mesh", nsmgr);
                                            if (otherMeshes != null)
                                            {
                                                foreach (XmlNode m in otherMeshes)
                                                {
                                                    allMeshes.Add(m);
                                                }
                                            }
                                        }
                                    }
                                    if (allMeshes.Count > 0)
                                    {
                                        Debug.Log($"[ModelLoader] 方法6 (object下查找): 找到 {allMeshes.Count} 个mesh");
                                        // 直接解析这些mesh
                                        foreach (XmlNode meshNode in allMeshes)
                                        {
                                            ParseSingleMeshNode(meshNode, nsmgr, vertices, triangles, normals, colors);
                                        }
                                        if (vertices.Count > 0 && triangles.Count > 0)
                                        {
                                            goto CreateMesh; // 跳转到创建网格
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                // 方法4: 查找所有可能的mesh节点
                if (meshNodes == null || meshNodes.Count == 0)
                {
                    XmlNodeList allNodes = doc.SelectNodes("//*");
                    List<XmlNode> meshList = new List<XmlNode>();
                    if (allNodes != null)
                    {
                        foreach (XmlNode node in allNodes)
                        {
                            if (node.LocalName == "mesh" || node.Name == "mesh")
                            {
                                meshList.Add(node);
                            }
                        }
                    }
                    
                    if (meshList.Count > 0)
                    {
                        Debug.Log($"[ModelLoader] 通过遍历所有节点找到 {meshList.Count} 个mesh对象");
                        // 直接使用列表解析
                        foreach (XmlNode meshNode in meshList)
                        {
                            ParseSingleMeshNode(meshNode, nsmgr, vertices, triangles, normals, colors);
                        }
                    }
                }
                
                // 如果还是找不到，使用递归查找
                if (meshNodes == null || meshNodes.Count == 0)
                {
                    List<XmlNode> foundMeshes = new List<XmlNode>();
                    FindMeshNodesRecursive(doc.DocumentElement, foundMeshes);
                    if (foundMeshes.Count > 0)
                    {
                        Debug.Log($"[ModelLoader] 通过递归查找找到 {foundMeshes.Count} 个mesh对象");
                        // 直接使用列表解析
                        foreach (XmlNode meshNode in foundMeshes)
                        {
                            ParseSingleMeshNode(meshNode, nsmgr, vertices, triangles, normals, colors);
                        }
                    }
                }
                else
                {
                    Debug.Log($"[ModelLoader] 找到 {meshNodes.Count} 个mesh对象");
                    // 解析每个mesh
                    foreach (XmlNode meshNode in meshNodes)
                    {
                        ParseSingleMeshNode(meshNode, nsmgr, vertices, triangles, normals, colors);
                    }
                }
                
                CreateMesh:
                
                // 检查是否成功解析到数据
                if (vertices.Count == 0 || triangles.Count == 0)
                {
                    Debug.LogError("[ModelLoader] 3MF文件中未找到mesh数据");
                    Debug.LogError($"[ModelLoader] XML根节点: {doc.DocumentElement?.Name}, 命名空间: {doc.DocumentElement?.NamespaceURI}");
                    
                    // 输出XML结构用于调试（更详细）
                    Debug.LogError("[ModelLoader] ========== 详细XML结构 (前5层) ==========");
                    if (doc.DocumentElement != null)
                    {
                        OutputXmlStructure(doc.DocumentElement, 0, 5);
                    }
                    else
                    {
                        Debug.LogError("[ModelLoader] DocumentElement为null!");
                    }
                    Debug.LogError("[ModelLoader] ========================================");
                    
                    // 尝试手动查找所有可能的节点
                    Debug.LogError("[ModelLoader] 尝试手动查找所有节点:");
                    ManualFindAllNodes(doc.DocumentElement, vertices, triangles, normals, colors, nsmgr);
                    
                    if (vertices.Count == 0 || triangles.Count == 0)
                    {
                        return null;
                    }
                }
                
                Debug.Log($"[ModelLoader] 3MF解析完成: 顶点数={vertices.Count}, 三角形数={triangles.Count / 3}");
                
                if (vertices.Count == 0 || triangles.Count == 0)
                {
                    Debug.LogError("[ModelLoader] 3MF解析后没有有效的几何数据");
                    return null;
                }
                
                // 计算法线
                if (normals.Count == 0 || normals.Count != vertices.Count)
                {
                    normals = CalculateNormals(vertices, triangles);
                }
                
                // 创建网格对象
                GameObject model = CreateMeshGameObject(vertices, triangles, normals, name);
                
                // 如果有颜色信息，应用到材质
                if (colors.Count > 0 && model != null)
                {
                    MeshRenderer renderer = model.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        // 3MF可能包含多个颜色，这里使用第一个或平均颜色
                        Color avgColor = Color.white;
                        if (colors.Count > 0)
                        {
                            Vector4 colorSum = Vector4.zero;
                            foreach (Color c in colors)
                            {
                                colorSum += new Vector4(c.r, c.g, c.b, c.a);
                            }
                            avgColor = new Color(
                                colorSum.x / colors.Count,
                                colorSum.y / colors.Count,
                                colorSum.z / colors.Count,
                                1f
                            );
                        }
                        renderer.material.color = avgColor;
                        Debug.Log($"[ModelLoader] 3MF材质颜色: {avgColor}");
                    }
                }
                
                return model;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModelLoader] 3MF XML解析错误: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
        /// <summary>
        /// 输出XML结构用于调试
        /// </summary>
        private void OutputXmlStructure(XmlNode node, int depth, int maxDepth)
        {
            if (node == null || depth > maxDepth) return;
            
            string indent = new string(' ', depth * 2);
            string nodeInfo = $"{indent}{node.Name} (LocalName: {node.LocalName}, Namespace: {node.NamespaceURI})";
            
            if (node.HasChildNodes)
            {
                int elementCount = 0;
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        elementCount++;
                    }
                }
                nodeInfo += $" - {elementCount} 个元素子节点";
            }
            
            // 输出属性信息
            if (node.Attributes != null && node.Attributes.Count > 0)
            {
                nodeInfo += " [属性: ";
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    if (i > 0) nodeInfo += ", ";
                    nodeInfo += $"{node.Attributes[i].Name}={node.Attributes[i].Value}";
                }
                nodeInfo += "]";
            }
            
            Debug.Log($"[ModelLoader] {nodeInfo}");
            
            if (node.HasChildNodes && depth < maxDepth)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        OutputXmlStructure(child, depth + 1, maxDepth);
                    }
                }
            }
        }
        
        /// <summary>
        /// 手动查找所有可能的节点（最后的尝试）
        /// </summary>
        private void ManualFindAllNodes(XmlNode node, List<Vector3> vertices, List<int> triangles, 
            List<Vector3> normals, List<Color> colors, XmlNamespaceManager nsmgr)
        {
            if (node == null) return;
            
            // 检查当前节点是否是mesh
            if (node.LocalName == "mesh" || node.Name == "mesh" || node.Name.Contains("mesh"))
            {
                Debug.Log($"[ModelLoader] 手动找到mesh节点: {node.Name}, 位置: {GetNodePath(node)}");
                ParseSingleMeshNode(node, nsmgr, vertices, triangles, normals, colors);
            }
            
            // 递归查找子节点
            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        ManualFindAllNodes(child, vertices, triangles, normals, colors, nsmgr);
                    }
                }
            }
        }
        
        /// <summary>
        /// 获取节点的路径（用于调试）
        /// </summary>
        private string GetNodePath(XmlNode node)
        {
            if (node == null) return "";
            List<string> path = new List<string>();
            XmlNode current = node;
            while (current != null && current.NodeType == XmlNodeType.Element)
            {
                path.Insert(0, current.Name);
                current = current.ParentNode;
            }
            return string.Join("/", path);
        }
    }
}


