# winC2D -- Windows 디스크 마이그레이션 도구

[English (README)](../README.md) · [简体中文](README.zh-CN.md) · [繁體中文](README.zh-Hant.md) · [English](README.en.md) · [日本語](README.ja.md) · [Русский](README.ru.md) · [Português](README.pt-BR.md)

---

## 소개

winC2D 는 Windows 디스크 마이그레이션 도구입니다. C 드라이브에 설치된 소프트웨어와 사용자 폴더를 다른 디스크로 이동하며, 표준 Windows **심볼릭 링크(Symlink)** 와 파일 복사를 사용합니다. 애플리케이션 바이너리나 레지스트리를 수정하지 않습니다.

## 주의 사항

소프트웨어 마이그레이션 후 원래 경로에 **심볼릭 링크(Symlink)** 를 생성하여 기존 경로가 계속 작동하도록 합니다. 대부분의 마이그레이션된 소프트웨어는 앱 본체나 바로 가기를 수정하지 않고도 새 위치에서 정상 실행됩니다. **표준 마이그레이션은 레지스트리를 변경하지 않습니다.**

> 설정의 "기본 설치 위치 변경" 기능은 새 앱의 기본 설치 위치를 변경하기 위해 **시스템 레지스트리를 수정합니다**. 문제가 발생하면 설정에서 기본값으로 복원하거나 시스템 복원 지점 / 백업을 통해 롤백하십시오.

## 주요 기능

- **설치된 소프트웨어 스캔** -- C 드라이브 소프트웨어를 크기·상태와 함께 표시, 다중 선택 일괄 마이그레이션
- **사용자 폴더 마이그레이션** -- 문서·사진·다운로드 등 스캔 및 마이그레이션
- **GUI 경로 선택** -- 드라이브 목록 자동 채움
- **심볼릭 링크** -- 마이그레이션 후 자동 생성, 원래 경로 유지
- **롤백 지원** -- 완전한 마이그레이션 로그 제공
- **7개 언어** -- 앱 내 언어 전환
- **다크 / 라이트 테마** -- 시스템 자동 추적, 수동 전환 가능
- **자동 권한 상승** -- 실행 시 관리자 권한 요청
- **Agent 지원** -- AI 에이전트·스크립트용 CLI 포함; [README.ai.md](README.ai.md) 참조

## 기술 스택

- C# · .NET 8.0 · WPF
- [WPF-UI](https://github.com/lepoco/wpfui) (Fluent Design)
- CommunityToolkit.Mvvm · Microsoft.Extensions.DependencyInjection

## 다운로드

1. [Releases](https://github.com/Aknirex/winC2D/releases) 에서 `winC2D-Setup.exe` 다운로드
2. 설치 프로그램 실행 -- 기본 `D:\Program Files\winC2D` (C 드라이브 비점유)
3. GUI, CLI, gsudo 권한 상승 도구 포함, AI Agent skill 을 `%USERPROFILE%\.agents\skills\winc2d\` 에 설치
4. 마이그레이션에는 **관리자 권한** 필요 (앱이 자동 상승)
5. 제어판 -> 프로그램 및 기능에서 제거
6. Windows 10 / 11 지원

## 사용 방법

1. **스캔** -- 폴더를 탐색하고 "크기 스캔" 클릭하여 디렉터리 크기 측정
2. **선택** -- 마이그레이션할 폴더 체크
3. **마이그레이션** -- 폴더를 대상 드라이브로 복사 후 원래 경로에 심볼릭 링크 생성
4. **롤백** -- 로그 페이지에서 완료된 작업 선택 후 롤백

## Agent CLI 모드

AI 에이전트 및 스크립트용 CLI에 대한 전체 참조는 [README.ai.md](README.ai.md) 를 확인하십시오.

심볼릭 링크 생성에는 **관리자 권한** 또는 **Windows Developer Mode** 가 필요합니다. 에이전트 실행 시 [gsudo](https://github.com/gerardog/gsudo) 를 사용하십시오.
