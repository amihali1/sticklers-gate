# Art generation (local, ComfyUI + SDXL)

Generates images on this PC's GPU (RTX 2070 Max-Q, 8 GB). Nothing leaves the machine. The game itself uses no AI at runtime; this is an offline art tool only.

## Setup (already done once on the dev PC)
- ComfyUI portable **nvidia_cu126** build (works with older NVIDIA drivers, 525+) extracted to `C:\Dev\ComfyUI_windows_portable`
- Models in `C:\Dev\ComfyUI_windows_portable\ComfyUI\models\`:

| File | Folder | Source | License |
|---|---|---|---|
| `sd_xl_base_1.0.safetensors` | `checkpoints/` | stabilityai/stable-diffusion-xl-base-1.0 | CreativeML OpenRAIL++-M (commercial use OK) |
| `sdxl_lightning_8step_lora.safetensors` | `loras/` | ByteDance/SDXL-Lightning | OpenRAIL++ |
| `sdxl_vae_fp16_fix.safetensors` | `vae/` | madebyollin/sdxl-vae-fp16-fix | MIT |

Why these: SDXL fits the 8 GB Turing GPU with room to spare, has the largest ecosystem (style LoRAs, ControlNet, IP-Adapter), and permits commercial use. Lightning makes images in 8 steps (about 4× faster). The fp16-fix VAE avoids SDXL's black-image bug on GPUs without bf16. Flux.1-dev is **non-commercial**, so it's not used.

## Use
1. `tools\artgen\start_comfyui.bat` (leave it running; UI at http://127.0.0.1:8188).
2. `python tools/artgen/comfy_generate.py "<prompt>" --style notebook --name <name> --out <dir> [--count N] [--width W --height H] [--quality]`
   - styles: `notebook` (doodle on lined paper, the game's look), `icon` (single object, white background), `none`
   - `--quality`: full 28-step SDXL instead of 8-step Lightning
   - every run appends its prompt and seed to `<out>/artgen_log.jsonl` so images can be regenerated

Don't generate while Unity or the autoplay tests are running; they share the GPU.
