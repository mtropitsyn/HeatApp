# HeatApp

Веб-приложение для расчёта теплообмена в противоточном слое.
Позволяет сравнивать температурные профили материала и газа, сохранять расчёты и выгружать результаты в CSV.

![C#](https://img.shields.io/badge/C%23-512BD4?logo=csharp&logoColor=white)
![ASP.NET Core 8](https://img.shields.io/badge/ASP.NET_Core-8-512BD4)
![EF Core](https://img.shields.io/badge/EF_Core-8-512BD4)
![SQLite](https://img.shields.io/badge/SQLite-003B57?logo=sqlite&logoColor=white)
[![Build](https://github.com/mtropitsyn/HeatApp/actions/workflows/build.yml/badge.svg)](https://github.com/mtropitsyn/HeatApp/actions/workflows/build.yml)

## Возможности

- Задание геометрии слоя, расходов, температур и теплоёмкостей потоков.
- Графики температур материала и газа, график их разности и таблица результатов.
- Сохранение входных параметров и результатов в SQLite; загрузка и удаление расчётов.
- Экспорт полного температурного профиля в CSV.
- Серверная проверка положительных параметров и числа шагов от 1 до 10 000.

## Быстрый запуск

Нужен [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) или совместимый установленный SDK с .NET 8 Runtime.

```bash
git clone https://github.com/mtropitsyn/HeatApp.git
cd HeatApp
dotnet restore HeatExchangeApp/HeatExchangeApp.csproj
dotnet run --project HeatExchangeApp/HeatExchangeApp.csproj --launch-profile http
```

Откройте **http://localhost:5154**. Форма уже содержит пример входных данных.
База `HeatExchangeApp/calculations.db` создаётся автоматически при первом запуске и не хранится в Git.
Для загрузки Bootstrap, jQuery и Chart.js на основной странице требуется интернет.

## Как попробовать

1. Оставьте пример в форме или введите свои параметры.
2. Нажмите «Выполнить расчёт» — ниже появятся графики и таблица.
3. Перезагрузите страницу: расчёт появится в списке сохранённых.
4. Нажмите «Загрузить», чтобы восстановить параметры, или «CSV» для экспорта.

## Устройство проекта

```text
HeatExchangeApp/
├── Controllers/HomeController.cs  # Расчёт, история и экспорт
├── Models/                       # EF Core и модели входных/выходных данных
├── Views/Home/Index.cshtml        # Форма, графики и таблица
└── Program.cs                    # DI, SQLite и маршруты
.github/workflows/build.yml       # Сборка в GitHub Actions
```

Данные расчёта сохраняются как JSON в сущности `Calculation`. На главной странице отображаются последние 30 записей.
Для собственной базы задайте стандартную настройку ASP.NET Core `ConnectionStrings__DefaultConnection`, например `Data Source=/path/to/calculations.db`.

## Проверка сборки

```bash
dotnet build HeatExchangeApp/HeatExchangeApp.csproj --configuration Release
```

GitHub Actions восстанавливает зависимости и собирает приложение при push и pull request.

## Границы проекта

Это учебная реализация расчётной модели. Физические допущения и корректность формул для конкретного аппарата требуют отдельной проверки; приложение не прошло инженерную валидацию.
Текущая версия использует общее локальное хранилище без разделения пользователей. Запуск выше предназначен для локального знакомства.

## Дальнейшее развитие

- Выделить вычислительную модель в отдельный сервис.
- Добавить тесты по проверенным эталонным расчётам.
- Добавить сравнение нескольких сохранённых профилей.
