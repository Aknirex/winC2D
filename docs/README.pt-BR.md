# winC2D — Ferramenta de Migração de Disco Windows

[English (README)](../README.md) · [简体中文](README.zh-CN.md) · [繁體中文](README.zh-Hant.md) · [English](README.en.md) · [日本語](README.ja.md) · [한국어](README.ko.md) · [Русский](README.ru.md)

---

## Sobre

winC2D é uma ferramenta de migração de disco para Windows. Permite mover softwares instalados e pastas de usuário do drive C para outro disco, além de alterar o caminho de instalação padrão do sistema e os locais das pastas do usuário, liberando espaço no disco do sistema.

## ⚠️ Avisos Importantes

Após migrar softwares, o winC2D cria **links simbólicos (symlinks)** nos caminhos originais para mantê-los funcionando. A maioria dos programas migrados continua funcionando no novo local sem alterar os aplicativos ou atalhos. **A migração padrão não modifica o registro do Windows.**

> A função "Alterar caminho de instalação padrão" nas configurações **modifica o registro do sistema** para redirecionar onde novos aplicativos são instalados. Em caso de problemas, restaure o valor padrão nas configurações ou reverta via ponto de restauração do sistema / backup.

## Funcionalidades

- 📦 Escaneia softwares instalados no drive C com tamanho e status; suporta seleção múltipla para migração em lote
- 📁 Escaneia e migra pastas de usuário comuns (Documentos, Imagens, Downloads, etc.)
- 🖱️ Seleção gráfica do caminho de destino com lista de drives preenchida automaticamente
- 🔗 Criação automática de links simbólicos após migração para preservar os caminhos originais
- ↩️ Suporte a rollback com log completo de migração
- 🌏 Troca de idioma dentro do app — 7 idiomas suportados
- 🌙 Tema escuro / claro segue o sistema, com opção manual
- 🛡️ Solicita privilégios de administrador automaticamente na inicialização

## Tecnologias

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## Download e Execução

1. Baixe a versão mais recente em [Releases](https://github.com/SKR7lex/winC2D/releases)
2. Execute como **Administrador** (o app solicitará elevação automaticamente)
3. Requer Windows 10 / 11
