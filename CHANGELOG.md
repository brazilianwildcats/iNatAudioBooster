# Changelog

## V02.4 — reconstrução responsiva da interface

- Corrigida a sobreposição generalizada em telas com escala de 125%, 150% ou superior.
- Cabeçalho, área principal e rodapé agora ocupam linhas independentes.
- Removido o posicionamento absoluto dos elementos do cabeçalho.
- Painel esquerdo ampliado e equipado com rolagem vertical.
- Grade de ganhos usa linhas estáveis e botões maiores.
- Controles da pasta de saída passaram para disposição vertical.
- Botões INICIAR e Cancelar usam colunas proporcionais.
- Cabeçalho da lista de vídeos permite quebra e reorganização dos botões.
- Rodapé responsivo com texto abreviado automaticamente.
- Tabela de arquivos recebeu linhas e cabeçalhos maiores.
- A janela usa escala DPI explícita e maximiza automaticamente em telas menores.
- Versão interna atualizada para `2.0.4`.

## V02.3 — correção dos recursos visuais na versão portátil

- Corrigida a falha fatal quando `assets/app-icon.ico` não era copiado para o ZIP.
- O aplicativo agora usa prioritariamente o ícone incorporado ao próprio `.exe`.
- Se o ícone externo estiver ausente, o programa usa um fallback e continua aberto.
- Os logotipos PNG também passaram a ter carregamento protegido.
- `app-icon.ico`, `inat-trailcam-logo.png` e `logo-polones-footer.png` são copiados
  explicitamente pelo empacotador.
- A Action confirma a presença e o tamanho dos três recursos antes da montagem.
- Versão interna atualizada para `2.0.3`.

## V02.2 — inclusão determinística do FFmpeg BtbN

- Corrigida a montagem do ZIP quando o `dotnet publish` não copia arquivos externos ao projeto.
- `assemble_release.py` agora copia diretamente:
  - `vendor/ffmpeg/bin/ffmpeg.exe`;
  - `vendor/ffmpeg/bin/ffprobe.exe`;
  - todos os documentos de `vendor/ffmpeg/LICENSES`.
- O empacotamento não depende mais de `CopyToPublishDirectory` para o FFmpeg.
- A Action confirma a existência e o tamanho dos binários antes da publicação.
- Ativado UTF-8 no Python da Action para evitar caracteres corrompidos nos logs.
- Versão interna atualizada para `2.0.2`.

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
