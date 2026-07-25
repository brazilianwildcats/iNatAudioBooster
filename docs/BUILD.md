# Como gerar o ZIP portátil

## Método recomendado: GitHub Actions

1. Envie esta pasta para um repositório GitHub.
2. Abra a aba **Actions**.
3. Escolha **Build V02 Portable Windows**.
4. Clique em **Run workflow**.
5. Ao terminar, baixe o artefato:
   `iNat-TrailCam-Audio-Booster-V02-Portable-win-x64`.

A Action:

1. instala o .NET 10 no runner;
2. baixa `checksums.sha256` da release `latest` do BtbN;
3. baixa `ffmpeg-master-latest-win64-lgpl.zip`;
4. verifica o SHA-256 antes de extrair;
5. copia somente `ffmpeg.exe` e `ffprobe.exe`;
6. publica o aplicativo como `win-x64` self-contained e single-file;
7. confirma que o pacote final não contém PowerShell ou BAT;
8. gera `SHA256SUMS.txt`;
9. cria o ZIP portátil final.

## Compilação local

Requisitos somente para o computador do desenvolvedor:

- Windows 11;
- SDK .NET 10;
- Python 3.11 ou superior.

Comandos:

```text
python scripts/prepare_ffmpeg.py
dotnet publish src/iNatTrailCamAudioBooster/iNatTrailCamAudioBooster.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/publish
python scripts/assemble_release.py
```

O usuário final não precisa instalar .NET, Python ou FFmpeg.
