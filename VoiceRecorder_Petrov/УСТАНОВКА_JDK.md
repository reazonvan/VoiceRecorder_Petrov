# Установка JDK для Android разработки

## Проблема
```
ERROR: JAVA_HOME is not set and no 'java' command could be found
```

## Решение: Установите JDK

### Шаг 1: Скачайте JDK

**Рекомендуется: Microsoft OpenJDK 17**

Прямая ссылка:
https://aka.ms/download-jdk/microsoft-jdk-17.0.13-windows-x64.msi

Или перейдите:
https://learn.microsoft.com/en-us/java/openjdk/download#openjdk-17

### Шаг 2: Установите

1. Запустите скачанный `.msi` файл
2. Следуйте инструкциям установщика
3. Оставьте путь по умолчанию: `C:\Program Files\Microsoft\jdk-17.x.x`

### Шаг 3: Проверьте установку

Откройте **новое** окно PowerShell:

```powershell
java -version
```

Должно показать:
```
openjdk version "17.0.13"
```

### Шаг 4: Настройте переменные окружения (если нужно)

Если `java -version` не работает:

1. Откройте **Панель управления** → **Система** → **Дополнительные параметры системы**
2. Нажмите **Переменные среды**
3. В **Системные переменные** нажмите **Создать**:
   - Имя: `JAVA_HOME`
   - Значение: `C:\Program Files\Microsoft\jdk-17.0.13.11-hotspot` (ваш путь)
4. Найдите переменную `Path`, нажмите **Изменить**
5. Добавьте: `%JAVA_HOME%\bin`
6. Нажмите **OK** везде

### Шаг 5: Перезапустите Visual Studio

После установки JDK обязательно перезапустите Visual Studio!

---

## ⚠️ ВАЖНО: Для курсового проекта JDK НЕ ОБЯЗАТЕЛЬНА!

### Можно просто использовать Windows версию:

1. В Visual Studio выберите целевую платформу: **Windows Machine**
2. Нажмите **F5**
3. Приложение запустится на компьютере

**Android нужен только если хотите запустить на телефоне!**

Для защиты курсового проекта Windows версия подходит отлично ✅

---

## Альтернативные JDK (если Microsoft не подошла):

**Oracle JDK 17:**
https://www.oracle.com/java/technologies/downloads/#java17

**Adoptium (Eclipse Temurin) JDK 17:**
https://adoptium.net/temurin/releases/?version=17

---

## Проверка после установки

```powershell
# Проверка Java
java -version

# Проверка переменной
echo $env:JAVA_HOME

# Принятие лицензий Android (после установки JDK)
cd "C:\Program Files (x86)\Android\android-sdk\cmdline-tools\latest\bin"
.\sdkmanager.bat --licenses
# Нажимайте 'y' для каждой лицензии
```

---

**Автор:** Петров Иван  
**Дата:** 15.12.2025

