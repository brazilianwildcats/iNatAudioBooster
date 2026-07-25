from __future__ import annotations

import hashlib
import os
import shutil
import subprocess
import sys
import tempfile
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VENDOR = ROOT / "vendor" / "ffmpeg"
BIN = VENDOR / "bin"
LICENSES = VENDOR / "LICENSES"

ASSET_NAME = os.environ.get(
    "BTBN_ASSET",
    "ffmpeg-master-latest-win64-lgpl.zip",
)
BASE_URL = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest"
CHECKSUMS_URL = f"{BASE_URL}/checksums.sha256"
ASSET_URL = f"{BASE_URL}/{ASSET_NAME}"

USER_AGENT = "iNat-TrailCam-Audio-Booster-Build/2.0"


def download(url: str, destination: Path) -> None:
    request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=180) as response:
        if response.status != 200:
            raise RuntimeError(f"Falha no download ({response.status}): {url}")

        destination.parent.mkdir(parents=True, exist_ok=True)
        with destination.open("wb") as output:
            shutil.copyfileobj(response, output)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def expected_hash(checksums_text: str, filename: str) -> str:
    for raw in checksums_text.splitlines():
        line = raw.strip()
        if not line:
            continue
        parts = line.split()
        if len(parts) >= 2 and Path(parts[-1].lstrip("*")).name == filename:
            return parts[0].upper()
    raise RuntimeError(f"SHA-256 não encontrado em checksums.sha256 para {filename}")


def find_file(root: Path, name: str) -> Path:
    matches = list(root.rglob(name))
    if not matches:
        raise RuntimeError(f"{name} não encontrado dentro do pacote BtbN.")
    return matches[0]


def main() -> int:
    if BIN.exists():
        shutil.rmtree(BIN)
    if LICENSES.exists():
        shutil.rmtree(LICENSES)
    BIN.mkdir(parents=True, exist_ok=True)
    LICENSES.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="inat-ffmpeg-") as temp_name:
        temp = Path(temp_name)
        checksums_file = temp / "checksums.sha256"
        archive = temp / ASSET_NAME
        extract = temp / "extract"

        print(f"Baixando checksums: {CHECKSUMS_URL}")
        download(CHECKSUMS_URL, checksums_file)
        checksums_text = checksums_file.read_text(encoding="utf-8", errors="replace")
        expected = expected_hash(checksums_text, ASSET_NAME)

        print(f"Baixando FFmpeg BtbN: {ASSET_URL}")
        download(ASSET_URL, archive)
        actual = sha256(archive)

        if actual != expected:
            raise RuntimeError(
                f"SHA-256 do pacote não confere.\nEsperado: {expected}\nObtido:   {actual}"
            )

        print("Checksum do pacote confirmado.")
        with zipfile.ZipFile(archive) as package:
            package.extractall(extract)

        ffmpeg = find_file(extract, "ffmpeg.exe")
        ffprobe = find_file(extract, "ffprobe.exe")

        shutil.copy2(ffmpeg, BIN / "ffmpeg.exe")
        shutil.copy2(ffprobe, BIN / "ffprobe.exe")

        # Copia documentos de licença disponíveis no pacote.
        copied = set()
        patterns = ("LICENSE*", "COPYING*", "README*")
        package_root = ffmpeg.parents[1]
        for pattern in patterns:
            for item in package_root.glob(pattern):
                if item.is_file() and item.name.lower() not in copied:
                    shutil.copy2(item, LICENSES / item.name)
                    copied.add(item.name.lower())

        ffmpeg_hash = sha256(BIN / "ffmpeg.exe")
        ffprobe_hash = sha256(BIN / "ffprobe.exe")

        (LICENSES / "FFmpeg-SHA256.txt").write_text(
            f"{ffmpeg_hash}  ffmpeg.exe\n"
            f"{ffprobe_hash}  ffprobe.exe\n",
            encoding="utf-8",
        )

        (LICENSES / "FFmpeg-SOURCE.txt").write_text(
            "FFmpeg incluído no iNat TrailCam Audio Booster V02\n"
            "Fornecedor do binário: BtbN/FFmpeg-Builds\n"
            "Projeto: https://github.com/BtbN/FFmpeg-Builds\n"
            f"Pacote: {ASSET_NAME}\n"
            f"Download: {ASSET_URL}\n"
            f"SHA-256 do pacote: {actual}\n"
            "Variante: Windows 64-bit, LGPL, não-shared\n"
            "Código-fonte do FFmpeg: https://github.com/FFmpeg/FFmpeg\n",
            encoding="utf-8",
        )

        try:
            version = subprocess.run(
                [str(BIN / "ffmpeg.exe"), "-hide_banner", "-version"],
                check=False,
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
            )
            version_text = (version.stdout + "\n" + version.stderr).strip()
        except OSError:
            # Em runners não-Windows o executável não pode ser iniciado.
            version_text = "A versão detalhada será confirmada no runner Windows."

        (LICENSES / "FFmpeg-BUILD-INFO.txt").write_text(
            version_text + "\n",
            encoding="utf-8",
        )

        print(f"ffmpeg.exe:  {ffmpeg_hash}")
        print(f"ffprobe.exe: {ffprobe_hash}")
        print("FFmpeg BtbN preparado com sucesso.")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ERRO: {exc}", file=sys.stderr)
        raise
