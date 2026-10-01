<table>
  <tr>
    <td width="170" align="center" valign="middle">
      <img src="https://raw.githubusercontent.com/ai-iskuzhin/FnsNet.NpdStatus/main/assets/FnsSignNet.png" width="140" alt="Логотип FnsNet.NpdStatus" />
    </td>
    <td valign="middle">
      <h1>FnsNet.NpdStatus</h1>
      <p>.NET SDK для публичного сервиса ФНС России <a href="https://npd.nalog.ru/check-status/">«Проверка статуса налогоплательщика налога на профессиональный доход»</a>: по ИНН и дате отвечает, был ли человек самозанятым на эту дату.</p>
      <p>
        <a href="https://github.com/ai-iskuzhin/FnsNet.NpdStatus/actions/workflows/ci.yml"><img src="https://github.com/ai-iskuzhin/FnsNet.NpdStatus/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI" /></a>
        <a href="https://www.nuget.org/packages/FnsNet.NpdStatus"><img src="https://img.shields.io/nuget/v/FnsNet.NpdStatus?logo=nuget" alt="NuGet" /></a>
        <a href="https://www.nuget.org/packages/FnsNet.NpdStatus"><img src="https://img.shields.io/nuget/dt/FnsNet.NpdStatus" alt="Downloads" /></a>
        <a href="https://github.com/ai-iskuzhin/FnsNet.NpdStatus/blob/main/LICENSE"><img src="https://img.shields.io/github/license/ai-iskuzhin/FnsNet.NpdStatus" alt="License: MIT" /></a>
        <img src="https://img.shields.io/badge/targets-net8.0%20%7C%20net10.0-512BD4?logo=dotnet" alt="Targets" />
      </p>
    </td>
  </tr>
</table>

> Сервис открытый: ни ключа, ни регистрации, ни договора. Ограничения — **два запроса в минуту с
> одного IP-адреса**, и доступ только из российских сетей.

## Установка

```bash
dotnet add package FnsNet.NpdStatus
```

## Быстрый старт

```csharp
using FnsNet.NpdStatus;
using Microsoft.Extensions.DependencyInjection;

services.AddNpdStatusClient();
```

```csharp
public sealed class SelfEmployedCheck(INpdStatusClient fns)
{
    public async Task<bool?> IsSelfEmployedTodayAsync(string inn, CancellationToken ct)
    {
        try
        {
            var status = await fns.GetStatusAsync(inn, DateOnly.FromDateTime(DateTime.UtcNow), ct);

            return status.IsRegistered;
        }
        catch (NpdStatusException exception) when (exception.IsTransient)
        {
            // Сервис занят или недоступен — статус неизвестен. Это не «не самозанятый».
            return null;
        }
    }
}
```

`AddNpdStatusClient` возвращает `IHttpClientBuilder` — повторы, ограничение частоты и логирование
добавляются как обычно:

```csharp
services.AddNpdStatusClient()
    .AddPolicyHandler(retryPolicy)
    .AddHttpMessageHandler<MyRateLimitingHandler>();
```

## Без контейнера зависимостей

```csharp
using var client = NpdStatusClient.Create();

var status = await client.GetStatusAsync("525741209968", new DateOnly(2026, 10, 1));
```

## Что умеет

| Метод | Эндпоинт | Назначение |
|---|---|---|
| `GetStatusAsync(inn, date, ct)` | `POST /api/v1/tracker/taxpayer_status` | Был ли ИНН плательщиком НПД на указанную дату |

Ответ — `NpdTaxpayerStatus(Inn, Date, IsRegistered, Message)`. `Message` — фраза самого сервиса
по-русски, её можно показывать пользователю.

`requestDate` принимается не раньше 01.01.2019, когда режим начал действовать, и не позже
сегодняшнего дня. Регистрацию можно прекратить в любой момент, поэтому ответ верен только для того
дня, о котором спросили: храните дату рядом с результатом.

## Ошибки

Неуспешный ответ приходит как `NpdStatusException` с кодом и сообщением сервиса.

| `Code` | HTTP | Что значит | `IsTransient` |
|---|---|---|---|
| `validation.failed` | 422 | Некорректный ИНН или дата вне допустимого окна. Подробности — в сообщении. | `false` |
| `taxpayer.status.service.unavailable.error` | 422 | Сервис временно недоступен. | `true` |
| `taxpayer.status.service.limited.error` | 422 | Превышены два запроса в минуту. | `true` |
| *(нет)* | 400 | Тело не разобралось, поэтому кода сервис не прислал. | `false` |
| *(нет)* | 500 | Внутренняя ошибка сервиса. | `true` |

Ошибка не значит «не самозанятый»: неудачная проверка оставляет статус неизвестным.

## Источник

[Описание API публичного сервиса ФНС России «Проверка статуса налогоплательщика налога на
профессиональный доход (самозанятого)»](https://npd.nalog.ru/html/sites/www.npd.nalog.ru/api_statusnpd_nalog_ru.pdf),
версия 1.0.0 от 21.02.2019. Краткий конспект — в [`docs/api.md`](docs/api.md).

## Лицензия

MIT.
