# Segurança e distribuição

A V02 foi projetada para eliminar o comportamento que gerou a detecção
`PDM:Trojan.Win32.Generic` na versão PowerShell anterior.

## O pacote portátil final

- não executa PowerShell;
- não contém `.ps1`, `.bat`, `.cmd` ou `.vbs`;
- não baixa nem extrai executáveis;
- não grava persistência no Registro;
- não cria tarefas agendadas;
- não solicita privilégios de administrador;
- chama apenas o FFmpeg incluído na pasta `tools/ffmpeg/bin`;
- valida os hashes SHA-256 de `ffmpeg.exe` e `ffprobe.exe`.

## Reputação e assinatura

Um executável novo e sem assinatura digital ainda pode receber alerta de
reputação de algum antivírus ou do SmartScreen. Para distribuição pública,
recomenda-se assinar o executável com um certificado de assinatura de código.

Nunca recomende ao usuário desativar o antivírus ou criar uma exclusão ampla.
Investigue qualquer detecção usando o nome do arquivo e seu SHA-256.
