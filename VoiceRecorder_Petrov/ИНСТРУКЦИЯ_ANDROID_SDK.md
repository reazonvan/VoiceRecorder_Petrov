# Инструкция: Принятие лицензий Android SDK

## Проблема
```
Не удалось принять лицензии пакета SDK для Android.
```

## Решение

### Вариант 1: Через Visual Studio (РЕКОМЕНДУЕТСЯ)

1. Откройте **Visual Studio 2022**
2. Перейдите в **Tools** → **Android** → **Android SDK Manager**
3. В окне SDK Manager:
   - Нажмите **Accept** напротив всех компонентов
   - Нажмите **Apply**
   - Дождитесь установки
4. Перезапустите Visual Studio

### Вариант 2: Через командную строку

1. Откройте **PowerShell** от имени администратора

2. Найдите путь к SDK:
```powershell
# Обычно находится здесь:
cd "C:\Program Files (x86)\Android\android-sdk"
```

3. Выполните команду для принятия всех лицензий:
```powershell
.\cmdline-tools\latest\bin\sdkmanager.bat --licenses
```

4. Нажимайте **y** и **Enter** для каждой лицензии

5. Перезапустите Visual Studio

### Вариант 3: Установка Android SDK через Visual Studio Installer

1. Откройте **Visual Studio Installer**
2. Нажмите **Modify** напротив Visual Studio 2022
3. Во вкладке **Individual components** найдите:
   - ✅ Android SDK setup (API level 31, 33, 34)
   - ✅ Android SDK tools
   - ✅ Android emulator
4. Нажмите **Modify** и дождитесь установки
5. Перезапустите Visual Studio

## Проверка

После выполнения шагов:

1. Откройте проект в Visual Studio
2. Выберите целевую платформу **Android**
3. Попробуйте собрать проект (**Build** → **Build Solution**)

Должно работать без ошибок!

## Альтернатива: Используйте Windows

Если проблемы с Android продолжаются, можно запустить на Windows:

1. В Visual Studio выберите целевую платформу: **Windows Machine**
2. Нажмите **F5**
3. Приложение запустится на вашем компьютере

Для курсового проекта Windows версия подходит отлично!

## Полезные пути

- Android SDK: `C:\Program Files (x86)\Android\android-sdk`
- Android SDK через Users: `C:\Users\%USERNAME%\AppData\Local\Android\Sdk`
- Visual Studio Installer: `C:\Program Files (x86)\Microsoft Visual Studio\Installer\setup.exe`

---

**Примечание:** Для курсового проекта достаточно Windows версии! Android нужен только если хотите запустить на телефоне.

