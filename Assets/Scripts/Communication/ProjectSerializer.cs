using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PocoRender.Communication
{
    /// <summary>
    /// Serializes/deserializes CanvasController state to/from JSON.
    /// Used by CommandDispatcher for save_project / open_project commands.
    ///
    /// Format:
    /// <code>
    /// {
    ///   "version": 1,
    ///   "project_name": "...",
    ///   "canvas_width": 600,
    ///   "canvas_height": 600,
    ///   "layers": [
    ///     {
    ///       "name": "Layer 0",
    ///       "pos_x": 0, "pos_y": 0,
    ///       "width": 100, "height": 100,
    ///       "rotation": 0,
    ///       "visible": true,
    ///       "locked": false,
    ///       "craft_mode": "Flat",
    ///       "ink_mode": "White > CMYK",
    ///       "sprite_base64": "..." // PNG bytes, base64-encoded
    ///     }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    public static class ProjectSerializer
    {
        private const int FormatVersion = 1;

        public static string Serialize(RectTransform paper,
                                        string projectName,
                                        float canvasWidth,
                                        float canvasHeight)
        {
            if (paper == null)
                return "{}";

            var layers = new List<LayerEntry>();

            for (int i = 0; i < paper.childCount; i++)
            {
                Transform child = paper.GetChild(i);
                if (child.name == "BGDeselector") continue;
                if (!child.gameObject.activeSelf) continue;

                var rt = child.GetComponent<RectTransform>();
                var img = child.GetComponent<Image>();
                var manipulator = child.GetComponent<UI.ObjectManipulator>();
                var layerData = child.GetComponent<UI.LayerData>();

                var entry = new LayerEntry
                {
                    name = child.name,
                    pos_x = rt != null ? rt.anchoredPosition.x : 0,
                    pos_y = rt != null ? rt.anchoredPosition.y : 0,
                    width = rt != null ? rt.sizeDelta.x : 0,
                    height = rt != null ? rt.sizeDelta.y : 0,
                    rotation = rt != null ? rt.localEulerAngles.z : 0,
                    visible = child.gameObject.activeSelf,
                    locked = manipulator != null && manipulator.IsLocked,
                    craft_mode = layerData != null ? layerData.craftMode : "Flat",
                    ink_mode = layerData != null ? layerData.inkMode : "White > CMYK",
                    sprite_base64 = ""
                };

                if (img != null && img.sprite != null && img.sprite.texture != null)
                {
                    try
                    {
                        var tex = img.sprite.texture;
                        if (tex.isReadable)
                        {
                            byte[] png = tex.EncodeToPNG();
                            entry.sprite_base64 = Convert.ToBase64String(png);
                        }
                    }
                    catch
                    {
                        // Texture not readable — skip
                    }
                }

                layers.Add(entry);
            }

            var project = new ProjectEnvelope
            {
                version = FormatVersion,
                project_name = projectName ?? "",
                canvas_width = canvasWidth,
                canvas_height = canvasHeight,
                layers = layers.ToArray()
            };

            return JsonUtility.ToJson(project, true);
        }

        public static ProjectEnvelope Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                return JsonUtility.FromJson<ProjectEnvelope>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProjectSerializer] Deserialize failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Applies deserialized project data to a CanvasController's paper.
        /// Creates Image GameObjects for each layer.
        /// </summary>
        public static void ApplyToCanvas(ProjectEnvelope project, RectTransform paper)
        {
            if (project == null || paper == null) return;

            ClearCanvas(paper);

            paper.sizeDelta = new Vector2(project.canvas_width, project.canvas_height);

            foreach (var layer in project.layers)
            {
                var go = new GameObject(layer.name,
                    typeof(RectTransform), typeof(Image));
                go.transform.SetParent(paper, false);

                var rt = go.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(layer.pos_x, layer.pos_y);
                rt.sizeDelta = new Vector2(layer.width, layer.height);
                rt.localEulerAngles = new Vector3(0, 0, layer.rotation);

                if (!string.IsNullOrEmpty(layer.sprite_base64))
                {
                    try
                    {
                        byte[] png = Convert.FromBase64String(layer.sprite_base64);
                        var tex = new Texture2D(2, 2);
                        if (tex.LoadImage(png))
                        {
                            var sprite = Sprite.Create(tex,
                                new Rect(0, 0, tex.width, tex.height),
                                new Vector2(0.5f, 0.5f));
                            go.GetComponent<Image>().sprite = sprite;
                        }
                    }
                    catch
                    {
                        // Base64 decode failed — leave blank
                    }
                }

                var ld = go.AddComponent<UI.LayerData>();
                ld.craftMode = layer.craft_mode ?? "Flat";
                ld.inkMode = layer.ink_mode ?? "White > CMYK";

                go.SetActive(layer.visible);
            }
        }

        /// <summary>Clears all layer children from paper (keeps BGDeselector). Used by CommandDispatcher.</summary>
        public static void ClearCanvas(RectTransform paper)
        {
            if (paper == null) return;
            for (int i = paper.childCount - 1; i >= 0; i--)
            {
                var child = paper.GetChild(i);
                if (child.name == "BGDeselector") continue;
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        [Serializable]
        public class ProjectEnvelope
        {
            public int version;
            public string project_name;
            public float canvas_width;
            public float canvas_height;
            public LayerEntry[] layers;
        }

        [Serializable]
        public class LayerEntry
        {
            public string name;
            public float pos_x;
            public float pos_y;
            public float width;
            public float height;
            public float rotation;
            public bool visible;
            public bool locked;
            public string craft_mode;
            public string ink_mode;
            public string sprite_base64;
        }
    }
}
