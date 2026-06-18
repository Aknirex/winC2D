# winC2D -- Ferramenta de Migracao de Disco Windows

[English (README)](../README.md) · [简体中文](README.zh-CN.md) · [繁體中文](README.zh-Hant.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md)

---

## Sobre

winC2D e uma ferramenta de migracao de disco para Windows. Permite mover softwares instalados e pastas de usuario do drive C para outro disco, usando **links simbolicos (symlinks)** padrao do Windows e copia de arquivos -- sem modificar binarios ou o registro.

## Avisos Importantes

Apos migrar softwares, o winC2D cria **links simbolicos (symlinks)** nos caminhos originais para mante-los funcionando. A maioria dos programas migrados continua funcionando no novo local sem alterar os aplicativos ou atalhos. **A migracao padrao nao modifica o registro do Windows.**

> A funcao "Alterar caminho de instalacao padrao" nas configuracoes **modifica o registro do sistema** para redirecionar onde novos aplicativos sao instalados. Em caso de problemas, restaure o valor padrao nas configuracoes ou reverta via ponto de restauracao do sistema / backup.

## Funcionalidades

- **Escanear softwares instalados** -- exibe softwares do drive C com tamanho e status; selecao multipla para migracao em lote
- **Migracao de pastas de usuario** -- escaneia e migra pastas comuns (Documentos, Imagens, Downloads, etc.)
- **Selecao grafica de caminho** -- lista de drives preenchida automaticamente
- **Links simbolicos** -- criados automaticamente apos migracao para preservar os caminhos originais
- **Suporte a rollback** -- log completo de migracao com rollback em um clique
- **7 idiomas** -- troca de idioma dentro do app
- **Tema escuro / claro** -- segue o sistema, com opcao manual
- **Elevacao automatica** -- solicita privilegios de administrador na inicializacao
- **Pronto para agentes** -- inclui CLI para agentes de IA e scripts; veja [README.ai.md](README.ai.md)

## Tecnologias

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## Download

1. Baixe `winC2D-Setup.exe` em [Releases](https://github.com/Aknirex/winC2D/releases)
2. Execute o instalador -- padrao `D:\Program Files\winC2D` (nao ocupa o drive C)
3. O instalador inclui GUI, CLI, gsudo, e instala o AI Agent skill em `%USERPROFILE%\.agents\skills\winc2d\`
4. Privilégios de administrador sao necessarios para migracao (o app eleva automaticamente)
5. Desinstale via Painel de Controle -> Programas e Recursos
6. Requer Windows 10 / 11

## Como Funciona

1. **Escanear** -- navegue ate uma pasta e clique em "Escanear Tamanhos"
2. **Selecionar** -- marque as pastas que deseja migrar
3. **Migrar** -- o winC2D copia a pasta para o drive de destino e cria um symlink no caminho original
4. **Rollback** -- na pagina de Logs, selecione uma tarefa concluida e clique em Rollback

## Modo Agent CLI

O winC2D fornece uma CLI para agentes de IA e scripts. Consulte a referencia completa em [README.ai.md](README.ai.md).

Criar links simbolicos requer privilegios de **Administrador** ou **Windows Developer Mode**. Para execucao elevada por agentes, use [gsudo](https://github.com/gerardog/gsudo).
