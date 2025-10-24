# Python Interpretation Server

Сервер на C#, предназначенный для обработки и архивирования данных прогнозирования солнечной активности, полученных из Python-скрипта. Сервер использует RabbitMQ для получения запросов, SQL Server для хранения данных и Serilog для логирования.

## Описание

Проект представляет собой серверное приложение, которое:
- Выполняет Python-скрипт (`Prediction_MoreInfo.py`) для получения данных прогноза солнечной активности.
- Сохраняет результаты в базе данных SQL Server с использованием хранимой процедуры.
- Обрабатывает запросы через RabbitMQ, возвращая последние данные прогноза.
- Логирует действия и ошибки с помощью Serilog (в консоль и файлы).
- При ошибках декодирует и сохраняет данные в текстовые файлы для анализа.

## Требования

- .NET 6.0 SDK
- SQL Server (с базой данных `Storm_Prediction` и хранимой процедурой `InsertAndUpdateRecords`)
- RabbitMQ сервер
- Python 3.8+ (для выполнения скрипта `Prediction_MoreInfo.py`)
- Git

## Установка

### Зависимости

Убедитесь, что у вас установлены следующие компоненты:
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [RabbitMQ](https://www.rabbitmq.com/download.html)
- [Python 3.8+](https://www.python.org/downloads/)

Установите зависимости проекта:

```bash
dotnet restore
```

### Установка проекта

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/VladPomian/ServerPythonInterpretation.git
   cd ServerPythonInterpretation
   ```

2. Настройте конфигурацию:
   - В файле `RabbitService.cs` укажите параметры подключения к RabbitMQ (хост, порт, имя пользователя, пароль).
   - В файле `ArchiveRepository.cs` укажите строку подключения к SQL Server.
   - В файле `Process.cs` укажите путь к исполняемому файлу Python и скрипту `Prediction_MoreInfo.py`.

3. Получите Python-скрипт:
   - Скрипт `Prediction_MoreInfo.py` доступен в репозитории: [Solar Activity Forecasting](https://github.com/VladPomian/prediction_script). Клонируйте его и поместите файл `Prediction_MoreInfo.py` в корень этого проекта или укажите правильный путь в `Process.cs`.

4. Настройте базу данных:
   - Создайте базу данных `Storm_Prediction` в SQL Server.
   - Создайте таблицы `Archive`, `Actual` и `LastSuccess` со следующей структурой:
     ```sql
     CREATE TABLE [dbo].[Archive] (
         [time_enter] DATETIME NOT NULL,
         [data] NVARCHAR(MAX) NOT NULL,
         [size_data] INT NOT NULL,
         [status] NVARCHAR(50) NOT NULL
     );

     CREATE TABLE [dbo].[Actual] (
         [time_enter] DATETIME NOT NULL,
         [data] NVARCHAR(MAX) NOT NULL,
         [size_data] INT NOT NULL,
         [status] NVARCHAR(50) NOT NULL
     );

     CREATE TABLE [dbo].[LastSuccess] (
         [time_enter] DATETIME NOT NULL,
         [data] NVARCHAR(MAX) NOT NULL,
         [size_data] INT NOT NULL,
         [status] NVARCHAR(50) NOT NULL
     );
     ```
   - Выполните SQL-скрипт для создания хранимой процедуры `InsertAndUpdateRecords`, доступный в [sql/InsertAndUpdateRecords.sql](sql/InsertAndUpdateRecords.sql). Процедура вставляет данные в таблицу `Archive`, обновляет `Actual` и, если статус `Success`, обновляет `LastSuccess`.

## Использование

1. Скомпилируйте и запустите проект:
   ```bash
   dotnet run
   ```

2. Сервер начнет:
   - Выполнять Python-скрипт каждые 30 дней для обновления данных прогноза.
   - Слушать сообщения в очереди RabbitMQ `request_queue`.
   - Отвечать на запросы, возвращая последние данные из таблицы `Actual` или `LastSuccess` (если `Actual` содержит ошибку).
   - Логировать действия в консоль и файлы в папке `logs/`.

3. Для отправки тестового сообщения в очередь RabbitMQ используйте клиент RabbitMQ или отправьте сообщение в очередь `request_queue`.

## Структура проекта

- **Program.cs**: Точка входа приложения, инициализация логирования и сервисов.
- **RabbitService.cs**: Управляет подключением к RabbitMQ, обработкой входящих сообщений и отправкой ответов.
- **TaskService.cs**: Периодически (раз в 30 дней) запускает Python-скрипт и архивирует данные.
- **ArchiveRepository.cs**: Взаимодействует с базой данных SQL Server для хранения и получения данных.
- **Process.cs**: Запускает Python-скрипт и обрабатывает его вывод.
- **Decoder.cs**: Декодирует данные ошибок из base64 и сохраняет их в текстовые файлы.
- **StormPredictionModel.cs**: Модель данных для хранения информации о прогнозах.

## Пример вывода

При получении сообщения в очереди `request_queue` сервер вернет строку вида:
```
<time_enter> <base64_encoded_data> <size_data> <status>
```

Пример лога:
```
01.10.2025 12:00:00 [INF] Получено сообщение: <сообщение>
01.10.2025 12:00:01 [INF] [Actual] Ответ отправлен: 2025-10-01 12:00:00 1234 Success
```

Ошибки сохраняются в папке `FailureData/` в виде текстовых файлов, например:
```
Decoded Error Message: Ошибка: <описание_ошибки>
```

## Обработка ошибок

- Ошибки логируются в консоль и файлы в папке `logs/`.
- Если данные из таблицы `Actual` содержат ошибку, сервер возвращает данные из таблицы `LastSuccess`.
- Ошибки выполнения Python-скрипта или SQL-запросов логируются с подробным описанием.

## Ограничения

- Требуется наличие Python-скрипта `Prediction_MoreInfo.py` в корне проекта или по указанному пути. Скрипт доступен в [репозитории Python-скрипта](https://github.com/VladPomian/prediction_script).
- Необходимо настроить SQL Server и RabbitMQ с соответствующими учетными данными.
- Хранимая процедура `InsertAndUpdateRecords` должна быть предварительно создана в базе данных.

## Контакты

Если у вас есть вопросы или предложения, создайте issue в репозитории или свяжитесь с автором: VladPomian.
