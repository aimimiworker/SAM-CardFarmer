# 🃏 SAM Card Farmer

<p align="center">
  <b>A lightweight, fan-made card-idling companion for Steam Achievement Manager (SAM)</b><br>
  <i>Безопасная фанатская утилита-компаньон для фарма карточек Steam без скачивания игр и без передачи паролей/куки.</i>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows&logoColor=white" alt="Platform">
  <img src="https://img.shields.io/badge/Language-C%23-239120?logo=csharp&logoColor=white" alt="Language">
  <img src="https://img.shields.io/badge/.NET-4.0%20%7C%204.7.2%20%7C%204.8%20%7C%208.0-512BD4?logo=dotnet&logoColor=white" alt=".NET">
  <img src="https://img.shields.io/badge/Size-~40%20KB-orange" alt="Size">
  <img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License">
  <img src="https://img.shields.io/badge/Privacy-100%25%20Offline%20Safe-success" alt="Privacy">
</p>

---

> [!IMPORTANT]
> ### ⚠️ Fan Project & Legal Disclaimer / Юридический дисклеймер
> - **Unofficial Fan Companion:** This project is an **independent, open-source fan utility** created for personal convenience to work alongside the existing [Steam Achievement Manager](https://github.com/gibbed/SteamAchievementManager) created by Rick (Gibbed). It is **not** an official Gibbed product and **not** a fork of SAM itself, but an automation companion.
> - **Not Affiliated with Valve:** This software is **NOT affiliated with, authorized, maintained, sponsored, or endorsed by Valve Corporation, Steam, or any of their affiliates.** Valve, Steam, and their respective logos are trademarks and/or registered trademarks of Valve Corporation.
> - **No Proprietary Binary Redistribution:** This repository **does not contain or redistribute** any proprietary Valve code, Steamworks binaries (`steam_api.dll`), game data, or original SAM executables. Users must provide their own legitimate Steam installation and copy of SAM.
> - **Zero Login Credentials / 100% Local Privacy:** This software **NEVER** asks for your Steam account password, username, Steam Guard 2FA codes, Web API keys, or browser cookies. Everything works 100% locally via offline HTML badge page parsing.
> - **VAC Safe Reminder:** As with any tool communicating with the Steam client, **never idle games while playing online multiplayer games protected by Valve Anti-Cheat (VAC)** such as *Counter-Strike 2, Team Fortress 2, Rust*, etc. Always click **«⏹ Stop All»** before joining protected servers.

---

## 🌐 Language / Язык
- [🇬🇧 English Documentation](#-english)
- [🇷🇺 Русская документация](#-русский)

---

<a name="-english"></a>
## 🇬🇧 English

### 💡 Why SAM Card Farmer?
To idle Steam Trading Cards, conventional methods usually require:
1. **Downloading games:** Filling your storage drive with hundreds of gigabytes of games just to get a couple of card drops.
2. **Untrusted third-party apps:** Handing over your Steam login credentials, passwords, or sensitive browser cookies (`steamLoginSecure`, `sessionid`), risking your account security.
3. **Manual SAM usage:** Looking up AppIDs manually and having dozens of `SAM.Game.exe` windows cluttering your desktop, causing massive rendering lag on low-end CPUs and laptops due to hundreds of achievement icons being loaded simultaneously.

**SAM Card Farmer solves all of this in one lightweight (~40 KB) executable!**

---

### ✨ Features
- 🚀 **Zero Downloads (0 MB Disk Usage):** Signals Steam that games are running via SAM without downloading or installing a single game file.
- 🔒 **100% Privacy & Zero Cookies:** Detects available card drops simply by parsing your saved Steam Badges HTML file (`Ctrl + S` in any browser). No logins, no cookies, no token extraction.
- 🪟 **Hidden Background Idling (`SW_HIDE`):** Suppresses and hides all SAM GUI windows automatically. Games idle silently in the background with ~15 MB RAM usage and near-zero CPU/GPU impact.
- 🔄 **Smart Auto-Restart Timer (Default: 20 min):** Soft-restarts idling games on a configurable timer. This triggers Steam's drop check and prevents idle sessions from getting stalled.
- ⚡ **Live File Watcher:** Automatically monitors your saved badges file. When cards drop, just press `Ctrl + S` in your browser again — the app refreshes the list instantly.
- 📦 **Ultra-Portable Single File:** Standalone executable with zero external dependencies. Runs on any Windows OS with .NET Framework 4.0+.
- 📊 **Real-time Status & Control:** Filter games, search by title or AppID, right-click to view store/badge pages, copy AppIDs, and safely stop all running processes on exit.

---

### 🚀 Quick Start Guide

1. **Step 1: Save Badges Webpage**
   - Open your badges page in any web browser: [https://steamcommunity.com/my/badges](https://steamcommunity.com/my/badges)
   - Press <kbd>Ctrl</kbd> + <kbd>S</kbd> (or right-click ➔ *Save As...*) and save the HTML file anywhere (e.g. Downloads or Desktop).

2. **Step 2: Place & Launch SAM Card Farmer**
   - Place `SAM.CardFarmer.exe` inside your **Steam Achievement Manager** folder (next to `SAM.Game.exe`).
   - Run `SAM.CardFarmer.exe`.
   - **Drag & drop** the saved HTML file into the program window (or click the green *«📂 Open HTML File»* button).

3. **Step 3: Start Idling**
   - Click **«🚀 START IDLE (BACKGROUND)»**.
   - All selected games start in hidden background mode without popping up windows or lagging your PC.
   - Go about your day — Steam will drop trading cards directly into your inventory!

---

<a name="-русский"></a>
## 🇷🇺 Русский

### 💡 Зачем нужен SAM Card Farmer?
Обычно для фарма карточек Steam приходится либо:
1. **Скачивать игры:** Тратить сотни гигабайт дискового пространства и интернет-трафик ради пары карточек.
2. **Вводить пароли в сомнительные программы:** Доверять логины, пароли или секретные куки авторизации (`steamLoginSecure`) стороннему софту с риском угона аккаунта.
3. **Запускать SAM вручную:** Вбивать каждый AppID руками и терпеть десятки всплывающих окон SAM, которые забивают рабочий стол и вызывают тормоза на слабых ПК и ноутбуках из-за отрисовки сотен иконок достижений.

**SAM Card Farmer решает эту проблему раз и навсегда!**

---

### ✨ Ключевые возможности
- 🚀 **0 байт скачивания:** Игры **НЕ нужно скачивать и устанавливать**. Программа отправляет сигнал в Steam через SAM, что игра активна.
- 🔒 **100% Безопасность и отсутствие куки:** Читает только сохранённую вами HTML-страницу значков. Никаких паролей, токенов, куки браузера или доступа к аккаунту.
- 🪟 **Скрытый фоновый режим (`SW_HIDE`):** Полностью скрывает графические окна `SAM.Game.exe`. Процессы потребляют минимум оперативной памяти (~15 МБ) и не нагружают видеокарту и процессор отрисовкой картинок.
- 🔄 **Авто-перезапуск по таймеру (по умолчанию 20 мин):** Программа мягко перезапускает игры по таймеру, что сбрасывает зависшие сессии Steam и стимулирует выпадение карточек.
- ⚡ **Авто-слежение за файлом:** Программа непрерывно отслеживает файл значков. Выпали карточки? Нажмите <kbd>Ctrl</kbd> + <kbd>S</kbd> в браузере снова — список обновится сам!
- 📦 **Один портативный файл (~40 КБ):** Без установки, без сторонних DLL. Работает на любой Windows с .NET Framework 4.0+.
- 📊 **Полный контроль и статистика:** Поиск по названию или AppID, сортировка по оставшимся карточкам, запуск отдельных игр по правому клику, корректное завершение всех процессов при выходе.

---

### 🚀 Пошаговая инструкция

1. **Шаг 1: Сохранить страницу значков**
   - Откройте в браузере страницу ваших значков: [https://steamcommunity.com/my/badges](https://steamcommunity.com/my/badges)
   - Нажмите сочетание клавиш <kbd>Ctrl</kbd> + <kbd>S</kbd> и сохраните HTML-страницу в любую папку.

2. **Шаг 2: Поместить в папку с SAM и открыть файл**
   - Поместите `SAM.CardFarmer.exe` в папку со **Steam Achievement Manager** (рядом с `SAM.Game.exe`).
   - Запустите программу и **перетащите сохранённый HTML-файл мышкой** прямо в окно.

3. **Шаг 3: Нажать запуск**
   - Нажмите большую зелёную кнопку **«🚀 ЗАПУСТИТЬ ФАРМ (В ФОНЕ)»**.
   - Игры запустятся в скрытом режиме без всплывающих окон и тормозов. Карточки начнут падать в ваш инвентарь Steam!

---

## 🛠️ Сборка из исходников / Building from Source

У проекта **нет внешних зависимостей** — только стандартные библиотеки WinForms.

### Вариант 1: Через скрипт `build.bat` (Рекомендуется)
Просто дважды кликните по файлу `build.bat` в корне репозитория. Он найдет установленный компилятор и соберет исполняемый файл в папку `dist/`.

### Вариант 2: Через .NET CLI
```bash
dotnet build src/SAMCardFarmer.csproj -c Release -o dist
```

### Вариант 3: Через стандартный компилятор Windows (`csc.exe`)
(Работает на любой Windows даже без установленной Visual Studio):
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ /out:dist\SAM.CardFarmer.exe src\SAMCardFarmer.cs
```

---

## ❓ Вопросы и ответы (FAQ)

#### ❓ Действительно ли игры не нужно скачивать?
> **Да.** Программа и SAM отправляют сигнал в Steamworks API о запуске игры. Клиент Steam считает, что вы находитесь в игре, и активирует таймер выпадения карточек.

#### ❓ Мой компьютер не будет зависать, если запустить много игр?
> **Нет.** В отличие от ручного запуска SAM, все графические окна подавляются через Win32 API (`SW_HIDE`). Компьютер не тратит ресурсы видеокарты и процессора на отрисовку сотен картинок достижений.

#### ❓ Могут ли дать VAC-бан?
> Фарм карточек через эмуляцию сессий используется сообществом уже более 10 лет. Никаких банов за карточки нет. **Единственное правило:** никогда не играйте в соревновательные игры с защитой VAC (*CS2, TF2*) одновременно с запущенным фармом.

#### ❓ Как остановить работу программы?
> Нажмите кнопку **«⏹ ОСТАНОВИТЬ ВСЕ»** внизу окна или просто закройте программу — она автоматически завершит все запущенные фоновые процессы.

---

## 📜 Лицензия / License

Этот проект распространяется под свободной лицензией [MIT](LICENSE).

---

## ❤️ Благодарности / Acknowledgements

- **Rick (Gibbed)** и разработчикам [Steam Achievement Manager](https://github.com/gibbed/SteamAchievementManager) за создание легендарного инструмента для работы со Steam API.
- Сообществу Steam за поддержку открытых инструментов автоматизации.
