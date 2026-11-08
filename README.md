# HeatApp

Веб-приложение для расчёта теплообмена в противоточном слое.
По заданным параметрам строит графики температуры материала и газа.

Стек: C#, ASP.NET Core MVC, EF Core, SQLite, Chart.js.

## Что умеет

- Рассчитывать температурный профиль по высоте слоя.
- Показывать результаты на графиках и в таблице.
- Сохранять расчёты и загружать их повторно.
- Выгружать результаты в CSV.

## Запуск

Нужен .NET 8 SDK или более новый SDK с установленным .NET 8 Runtime.
Из корня репозитория:

```bash
dotnet restore HeatExchangeApp/HeatExchangeApp.csproj
dotnet run --project HeatExchangeApp/HeatExchangeApp.csproj --launch-profile http
```

Приложение откроется на http://localhost:5154. В форме уже заполнен пример.
База `HeatExchangeApp/calculations.db` создаётся при первом запуске.
Для загрузки библиотек графиков и оформления нужен интернет.

Проверить сборку:

```bash
dotnet build HeatExchangeApp/HeatExchangeApp.csproj --configuration Release
```
