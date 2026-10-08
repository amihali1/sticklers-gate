"""Generate game art with a local ComfyUI server (SDXL + Lightning).

Usage:
    python tools/artgen/comfy_generate.py "a goblin icon" --name goblin_icon --out SticklersGate/Assets/_Project/Art/Generated
    python tools/artgen/comfy_generate.py "tavern interior" --style notebook --width 1344 --height 768 --count 4

Requires ComfyUI running locally (default http://127.0.0.1:8188) with these
models installed (see tools/artgen/README.md):
    checkpoints/sd_xl_base_1.0.safetensors
    loras/sdxl_lightning_8step_lora.safetensors
    vae/sdxl_vae_fp16_fix.safetensors

Standard library only, so it runs with any Python 3.8+.
"""
import argparse
import json
import random
import sys
import time
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

CHECKPOINT = "sd_xl_base_1.0.safetensors"
LIGHTNING_LORA = "sdxl_lightning_8step_lora.safetensors"
VAE = "sdxl_vae_fp16_fix.safetensors"

# Style presets prepended to the prompt so assets share one look.
STYLES = {
    "none": ("", ""),
    "notebook": (
        "hand-drawn ballpoint pen doodle on lined notebook paper, simple sketchy ink lines, "
        "minimal flat colors, whimsical parody fantasy, clean composition, ",
        "photo, photorealistic, 3d render, blurry, watermark, signature, text, lowres, jpeg artifacts",
    ),
    "icon": (
        "game ability icon, bold simple hand-drawn ink doodle, single centered object, "
        "flat colors, thick outline, plain white background, ",
        "photo, photorealistic, 3d render, busy background, multiple objects, text, watermark, blurry",
    ),
}


def build_workflow(prompt, negative, width, height, seed, lightning, prefix):
    """ComfyUI API-format workflow: SDXL checkpoint (+ Lightning LoRA) -> sampler -> fp16-fix VAE -> save."""
    model_ref, clip_ref = ["ckpt", 0], ["ckpt", 1]
    workflow = {
        "ckpt": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": CHECKPOINT}},
        "vae": {"class_type": "VAELoader", "inputs": {"vae_name": VAE}},
    }
    if lightning:
        workflow["lora"] = {
            "class_type": "LoraLoader",
            "inputs": {"lora_name": LIGHTNING_LORA, "strength_model": 1.0, "strength_clip": 1.0,
                       "model": model_ref, "clip": clip_ref},
        }
        model_ref, clip_ref = ["lora", 0], ["lora", 1]

    # Lightning is distilled for ~8 steps at CFG 1 with Euler / sgm_uniform
    steps, cfg, sampler, scheduler = (8, 1.0, "euler", "sgm_uniform") if lightning else (28, 6.0, "dpmpp_2m", "karras")

    workflow.update({
        "pos": {"class_type": "CLIPTextEncode", "inputs": {"text": prompt, "clip": clip_ref}},
        "neg": {"class_type": "CLIPTextEncode", "inputs": {"text": negative, "clip": clip_ref}},
        "latent": {"class_type": "EmptyLatentImage", "inputs": {"width": width, "height": height, "batch_size": 1}},
        "sample": {
            "class_type": "KSampler",
            "inputs": {"seed": seed, "steps": steps, "cfg": cfg, "sampler_name": sampler, "scheduler": scheduler,
                       "denoise": 1.0, "model": model_ref, "positive": ["pos", 0], "negative": ["neg", 0],
                       "latent_image": ["latent", 0]},
        },
        "decode": {"class_type": "VAEDecode", "inputs": {"samples": ["sample", 0], "vae": ["vae", 0]}},
        "save": {"class_type": "SaveImage", "inputs": {"filename_prefix": prefix, "images": ["decode", 0]}},
    })
    return workflow


def call(server, path, payload=None):
    data = json.dumps(payload).encode() if payload is not None else None
    req = urllib.request.Request(f"{server}{path}", data=data, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=60) as resp:
        return resp.read()


def generate_one(server, workflow, timeout_s):
    client_id = uuid.uuid4().hex
    queued = json.loads(call(server, "/prompt", {"prompt": workflow, "client_id": client_id}))
    prompt_id = queued["prompt_id"]

    deadline = time.time() + timeout_s
    while time.time() < deadline:
        history = json.loads(call(server, f"/history/{prompt_id}"))
        if prompt_id in history:
            entry = history[prompt_id]
            status = entry.get("status", {})
            if status.get("status_str") == "error":
                raise RuntimeError(f"ComfyUI error: {json.dumps(status.get('messages', []))[:800]}")
            images = []
            for node_output in entry.get("outputs", {}).values():
                for img in node_output.get("images", []):
                    query = urllib.parse.urlencode(
                        {"filename": img["filename"], "subfolder": img["subfolder"], "type": img["type"]})
                    images.append(call(server, f"/view?{query}"))
            return images
        time.sleep(1.0)
    raise TimeoutError(f"Generation did not finish within {timeout_s}s")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("prompt")
    parser.add_argument("--negative", default="")
    parser.add_argument("--style", choices=sorted(STYLES), default="notebook")
    parser.add_argument("--width", type=int, default=1024)
    parser.add_argument("--height", type=int, default=1024)
    parser.add_argument("--count", type=int, default=1)
    parser.add_argument("--seed", type=int, default=None, help="first seed; later images use seed+1, +2...")
    parser.add_argument("--name", default="art", help="output file name prefix")
    parser.add_argument("--out", default="art_out")
    parser.add_argument("--quality", action="store_true", help="full 28-step SDXL instead of 8-step Lightning")
    parser.add_argument("--server", default="http://127.0.0.1:8188")
    parser.add_argument("--timeout", type=int, default=600)
    args = parser.parse_args()

    style_prefix, style_negative = STYLES[args.style]
    prompt = style_prefix + args.prompt
    negative = ", ".join(x for x in (style_negative, args.negative) if x)

    out_dir = Path(args.out)
    out_dir.mkdir(parents=True, exist_ok=True)
    seed = args.seed if args.seed is not None else random.randint(0, 2**31 - 1)

    for i in range(args.count):
        workflow = build_workflow(prompt, negative, args.width, args.height, seed + i,
                                  lightning=not args.quality, prefix=f"artgen/{args.name}")
        started = time.time()
        images = generate_one(args.server, workflow, args.timeout)
        for j, png in enumerate(images):
            path = out_dir / f"{args.name}_{seed + i}{'' if j == 0 else f'_{j}'}.png"
            path.write_bytes(png)
            print(f"{path}  ({time.time() - started:.1f}s, seed {seed + i})")

    # Record what made these so they can be regenerated
    with open(out_dir / "artgen_log.jsonl", "a", encoding="utf-8") as log:
        log.write(json.dumps({"name": args.name, "prompt": prompt, "negative": negative, "seed": seed,
                              "count": args.count, "size": [args.width, args.height],
                              "mode": "quality" if args.quality else "lightning"}) + "\n")


if __name__ == "__main__":
    try:
        main()
    except (urllib.error.URLError, ConnectionError) as e:
        sys.exit(f"Can't reach ComfyUI ({e}). Start it with tools/artgen/start_comfyui.bat")
