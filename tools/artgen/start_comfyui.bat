@echo off
REM Starts the local ComfyUI image server on http://127.0.0.1:8188 (localhost only).
REM Close Unity first if you're about to generate a lot: both share the laptop GPU's 8 GB.
set COMFY_DIR=C:\Dev\ComfyUI_windows_portable
if not exist "%COMFY_DIR%\python_embeded\python.exe" (
  echo ComfyUI not found at %COMFY_DIR%. See tools\artgen\README.md
  exit /b 1
)
cd /d "%COMFY_DIR%"
.\python_embeded\python.exe -s ComfyUI\main.py --windows-standalone-build --listen 127.0.0.1 --port 8188
