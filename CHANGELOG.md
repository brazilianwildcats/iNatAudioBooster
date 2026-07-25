# Changelog

## V02.1 — correção da compilação .NET 10 / WinForms

- Corrigidos os erros `WFO1000` em `ThemeControls.cs`.
- As propriedades personalizadas `Radius`, `BorderColor` e `Value` agora usam
  `DesignerSerializationVisibility.Hidden`, pois são configuradas apenas em código.
- Adicionado `Browsable(false)` para evitar exposição desnecessária no Designer.
- A configuração de DPI foi removida de `app.manifest`.
- Adicionado `ApplicationHighDpiMode=PerMonitorV2` ao arquivo `.csproj`.
- Atualizada a versão interna para `2.0.1`.

## V02 — aplicativo nativo para Windows

- Reescrito integralmente em C# com WinForms e .NET 10.
- Removidos PowerShell, arquivos BAT e download de executáveis em tempo de execução.
- FFmpeg e FFprobe BtbN incluídos no pacote portátil produzido pela compilação.
- Validação SHA-256 do pacote BtbN durante a criação da versão.
- Verificação SHA-256 de `ffmpeg.exe` e `ffprobe.exe` ao iniciar.
- Interface baseada no tema verde-escuro e creme do iNat TrailCam V78.
- Arrastar e soltar arquivos MP4 e AVI.
- Processamento em lote.
- Ganhos de +10, +15, +20, +30, +40, +50, +60, +70, +80 e +100 dB.
- Vídeo copiado sem recodificação.
- MP4 com áudio AAC 192 kb/s.
- AVI com áudio MP3 192 kb/s.
- Limitador opcional.
- Preservação de metadados e datas do arquivo.
- Pasta automática `Audio_Aumentado` ou destino personalizado.
- Progresso por arquivo e progresso geral.
- Cancelamento seguro e remoção de arquivos parciais.
- Proteção contra sobrescrita.
- Logs locais e página Sobre com identificação do FFmpeg.
