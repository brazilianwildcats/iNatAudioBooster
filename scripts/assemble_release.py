from __future__ import annotations

import hashlib
import json
import shutil
import sys
import zipfile
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLISH = ROOT / "artifacts" / "publish"
DIST = ROOT / "artifacts" / "release"
PACKAGE_DIR = DIST / "iNat_TrailCam_Audio_Booster_V02_Portable"
ZIP_PATH = DIST / "iNat_TrailCam_Audio_Booster_V02_Portable_win-x64.zip"

VENDOR_FFMPEG = ROOT / "vendor" / "ffmpeg"
VENDOR_BIN = VENDOR_FFMPEG / "bin"
VENDOR_LICENSES = VENDOR_FFMPEG / "LICENSES"
PROJECT_ASSETS = ROOT / "src" / "iNatTrailCamAudioBooster" / "assets"

FORBIDDEN_SUFFIXES = {".ps1", ".bat", ".cmd", ".vbs", ".js"}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def main() -> int:
    if not PUBLISH.exists():
        raise RuntimeError(f"Diretório de publicação não encontrado: {PUBLISH}")

    if DIST.exists():
        shutil.rmtree(DIST)
    PACKAGE_DIR.mkdir(parents=True, exist_ok=True)

    for item in PUBLISH.iterdir():
        destination = PACKAGE_DIR / item.name
        if item.is_dir():
            shutil.copytree(item, destination)
        else:
            shutil.copy2(item, destination)

    # Copia explicitamente os recursos visuais. O aplicativo também possui
    # fallbacks, mas o pacote oficial deve manter todos os recursos presentes.
    required_assets = [
        "app-icon.ico",
        "inat-trailcam-logo.png",
        "logo-polones-footer.png",
    ]

    target_assets = PACKAGE_DIR / "assets"
    target_assets.mkdir(parents=True, exist_ok=True)

    missing_assets = []
    for asset_name in required_assets:
        source_asset = PROJECT_ASSETS / asset_name
        if not source_asset.exists():
            missing_assets.append(str(source_asset))
            continue
        shutil.copy2(source_asset, target_assets / asset_name)

    if missing_assets:
        raise RuntimeError(
            "Recursos visuais obrigatórios ausentes:\n" +
            "\n".join(missing_assets)
        )

    # O dotnet publish pode não copiar arquivos externos ao diretório do projeto,
    # mesmo quando eles possuem CopyToPublishDirectory no .csproj. Para tornar o
    # pacote determinístico, copiamos os binários BtbN diretamente de vendor.
    vendor_ffmpeg = VENDOR_BIN / "ffmpeg.exe"
    vendor_ffprobe = VENDOR_BIN / "ffprobe.exe"

    missing_vendor = [
        str(path) for path in (vendor_ffmpeg, vendor_ffprobe)
        if not path.exists()
    ]
    if missing_vendor:
        raise RuntimeError(
            "O FFmpeg BtbN não foi preparado antes da montagem do pacote:\n" +
            "\n".join(missing_vendor) +
            "\n\nExecute primeiro: python scripts/prepare_ffmpeg.py"
        )

    target_bin = PACKAGE_DIR / "tools" / "ffmpeg" / "bin"
    target_bin.mkdir(parents=True, exist_ok=True)
    shutil.copy2(vendor_ffmpeg, target_bin / "ffmpeg.exe")
    shutil.copy2(vendor_ffprobe, target_bin / "ffprobe.exe")

    if not VENDOR_LICENSES.exists():
        raise RuntimeError(
            f"A pasta de licenças do FFmpeg não foi encontrada: {VENDOR_LICENSES}"
        )

    target_licenses = PACKAGE_DIR / "LICENSES"
    target_licenses.mkdir(parents=True, exist_ok=True)

    for item in VENDOR_LICENSES.iterdir():
        destination = target_licenses / item.name
        if item.is_dir():
            shutil.copytree(item, destination, dirs_exist_ok=True)
        else:
            shutil.copy2(item, destination)

    for name in [
        "LEIA-ME.txt",
        "VERSION.txt",
        "CHANGELOG.md",
        "SECURITY.md",
        "LICENSE.txt",
    ]:
        shutil.copy2(ROOT / name, PACKAGE_DIR / name)

    required = [
        PACKAGE_DIR / "iNat TrailCam Audio Booster.exe",
        PACKAGE_DIR / "assets" / "app-icon.ico",
        PACKAGE_DIR / "assets" / "inat-trailcam-logo.png",
        PACKAGE_DIR / "assets" / "logo-polones-footer.png",
        PACKAGE_DIR / "tools" / "ffmpeg" / "bin" / "ffmpeg.exe",
        PACKAGE_DIR / "tools" / "ffmpeg" / "bin" / "ffprobe.exe",
        PACKAGE_DIR / "LICENSES" / "FFmpeg-SHA256.txt",
        PACKAGE_DIR / "LICENSES" / "FFmpeg-SOURCE.txt",
    ]

    missing = [str(path) for path in required if not path.exists()]
    if missing:
        raise RuntimeError("Arquivos obrigatórios ausentes:\n" + "\n".join(missing))

    forbidden = [
        path for path in PACKAGE_DIR.rglob("*")
        if path.is_file() and path.suffix.lower() in FORBIDDEN_SUFFIXES
    ]
    if forbidden:
        raise RuntimeError(
            "O pacote portátil contém scripts proibidos:\n" +
            "\n".join(str(path) for path in forbidden)
        )

    checksum_lines = []
    for path in sorted(PACKAGE_DIR.rglob("*")):
        if path.is_file() and path.name != "SHA256SUMS.txt":
            relative = path.relative_to(PACKAGE_DIR).as_posix()
            checksum_lines.append(f"{sha256(path)}  {relative}")

    (PACKAGE_DIR / "SHA256SUMS.txt").write_text(
        "\n".join(checksum_lines) + "\n",
        encoding="utf-8",
    )

    source_info = (PACKAGE_DIR / "LICENSES" / "FFmpeg-SOURCE.txt").read_text(
        encoding="utf-8", errors="replace"
    )
    manifest = {
        "application": "iNat TrailCam Audio Booster",
        "version": "2.0.3",
        "platform": "win-x64",
        "framework": ".NET 10 self-contained",
        "created_utc": datetime.now(timezone.utc).isoformat(),
        "contains_powershell": False,
        "contains_runtime_downloader": False,
        "ffmpeg_source": source_info,
    }
    (PACKAGE_DIR / "release-manifest.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )

    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED) as package:
        for path in sorted(PACKAGE_DIR.rglob("*")):
            if path.is_file():
                arcname = Path(PACKAGE_DIR.name) / path.relative_to(PACKAGE_DIR)
                package.write(path, arcname)

    print(f"Pacote criado: {ZIP_PATH}")
    print(f"Tamanho: {ZIP_PATH.stat().st_size / 1024 / 1024:.1f} MB")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERRO: {exc}", file=sys.stderr)
        raise
