# Plano de testes da V02

## Inicialização

- Abrir no Windows 11 sem PowerShell e sem terminal.
- Confirmar o estado “FFmpeg BtbN incluído e validado”.
- Confirmar que o aplicativo não acessa a internet.

## Arquivos

- Adicionar um MP4.
- Adicionar um AVI.
- Arrastar vários arquivos de pastas diferentes.
- Confirmar rejeição silenciosa de formatos não aceitos.
- Confirmar que arquivos repetidos não são duplicados.

## Processamento

- Testar +10 dB com limitador.
- Testar +15 dB sem limitador.
- Testar +30 dB com limitador.
- Verificar que o codec de vídeo permanece idêntico.
- Verificar AAC 192 kb/s no MP4.
- Verificar MP3 192 kb/s no AVI.
- Confirmar metadados e datas quando a opção estiver ativa.
- Confirmar criação de `Audio_Aumentado`.
- Confirmar destino personalizado.
- Confirmar nomes `_2`, `_3` sem sobrescrita.

## Falhas

- Testar vídeo sem áudio.
- Remover temporariamente `ffmpeg.exe`.
- Corromper uma cópia do FFmpeg para validar o SHA-256.
- Cancelar no meio de um arquivo.
- Confirmar remoção do arquivo parcial.
- Confirmar geração de log.

## Antivírus

- Verificar o ZIP e o EXE no Microsoft Defender e no antivírus usado para teste.
- Registrar SHA-256 do executável.
- Não criar exclusões automáticas.
