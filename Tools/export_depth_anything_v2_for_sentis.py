"""
Re-export Depth-Anything-V2 (ViT-S) to an ONNX file that Unity Sentis 2.1.x can import.

Usage:
  1)  pip install torch torchvision onnx onnxsim
  2)  pip install huggingface_hub   (to auto-download weights)
  3)  python export_depth_anything_v2_for_sentis.py

The script will produce  depth_anything_v2_vits_sentis.onnx  in the current directory.
Copy it into  Assets/Resources/Models/  and rename to depth_anything_v2_vits.onnx,
then right-click -> Reimport in Unity.
"""

import torch
import torch.nn as nn
import os, sys

# ---------- 1. Try to load from HuggingFace hub (easiest) ----------

def load_model_huggingface():
    """Download & load the official checkpoint via huggingface_hub."""
    try:
        from huggingface_hub import hf_hub_download
        # ViT-S (small) – ~99 MB
        ckpt_path = hf_hub_download(
            repo_id="depth-anything/Depth-Anything-V2-Small",
            filename="depth_anything_v2_vits.pth",
        )
        print(f"[INFO] Downloaded checkpoint: {ckpt_path}")
    except Exception as e:
        print(f"[WARN] huggingface_hub download failed: {e}")
        print("[INFO] Trying local fallback...")
        ckpt_path = None

    # Try local path fallback
    if ckpt_path is None:
        candidates = [
            "depth_anything_v2_vits.pth",
            os.path.join(os.path.dirname(__file__), "depth_anything_v2_vits.pth"),
        ]
        for c in candidates:
            if os.path.exists(c):
                ckpt_path = c
                break
        if ckpt_path is None:
            print("[ERROR] No checkpoint found. Please download depth_anything_v2_vits.pth")
            print("        from https://huggingface.co/depth-anything/Depth-Anything-V2-Small")
            print("        and place it next to this script.")
            sys.exit(1)

    # Load the model architecture
    try:
        from depth_anything_v2.dpt import DepthAnythingV2
        model = DepthAnythingV2(
            encoder='vits',
            features=64,
            out_channels=[48, 96, 192, 384],
        )
    except ImportError:
        print("[INFO] 'depth_anything_v2' package not found, using minimal stub...")
        model = load_model_minimal(ckpt_path)
        return model

    state = torch.load(ckpt_path, map_location='cpu')
    model.load_state_dict(state)
    model.eval()
    return model


def load_model_minimal(ckpt_path):
    """
    If the depth_anything_v2 pip package is not available,
    try loading via torch.jit or a simpler wrapper.
    """
    print("[ERROR] Cannot load model without 'depth_anything_v2' package.")
    print("        Install it:  pip install depth-anything-v2")
    print("        Or:  git clone https://github.com/DepthAnything/Depth-Anything-V2")
    print("             cd Depth-Anything-V2 && pip install -e .")
    sys.exit(1)


# ---------- 2. Export ----------

def export_onnx(model, output_path="depth_anything_v2_vits_sentis.onnx", input_size=518):
    """Export with opset 14 and static shapes for maximum Sentis compatibility.
    input_size must be a multiple of 14 (ViT patch size). Default 518 = 14*37."""
    model.eval()

    dummy = torch.randn(1, 3, input_size, input_size)

    # Wrap the model to ensure output is a single tensor (not dict)
    class Wrapper(nn.Module):
        def __init__(self, m):
            super().__init__()
            self.m = m

        def forward(self, x):
            out = self.m(x)
            if isinstance(out, dict):
                # DepthAnythingV2 returns a dict with key 'predicted_depth' or similar
                for k in ('predicted_depth', 'depth', 'out'):
                    if k in out:
                        return out[k]
                return list(out.values())[0]
            return out

    wrapped = Wrapper(model)
    wrapped.eval()

    print(f"[INFO] Exporting with input shape [1, 3, {input_size}, {input_size}] ...")

    torch.onnx.export(
        wrapped,
        dummy,
        output_path,
        opset_version=14,               # Sentis 2.1.x supports opset ≤ 15
        input_names=["image"],
        output_names=["predicted_depth"],
        dynamic_axes=None,              # Static shapes only – no dynamic dims
        do_constant_folding=True,       # Fold constants to eliminate aten ops
        export_params=True,
    )

    print(f"[INFO] Exported: {output_path}")

    # Simplify
    try:
        import onnxsim, onnx
        print("[INFO] Running onnx-simplifier ...")
        m = onnx.load(output_path)
        m_sim, ok = onnxsim.simplify(m)
        if ok:
            onnx.save(m_sim, output_path)
            print(f"[INFO] Simplified: {output_path}")
        else:
            print("[WARN] Simplification returned check=False, keeping original.")
    except ImportError:
        print("[INFO] onnxsim not installed, skipping simplification.")

    print(f"\n=== Done! ===")
    print(f"Copy '{output_path}' into your Unity project at:")
    print(f"  Assets/Resources/Models/depth_anything_v2_vits.onnx")
    print(f"Then right-click -> Reimport in Unity Editor.")


if __name__ == "__main__":
    model = load_model_huggingface()
    export_onnx(model)
