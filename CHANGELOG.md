# Changelog

All notable changes to `FnsNet.NpdStatus` are documented in this file.

The project uses Semantic Versioning.

## 1.1.0

### Added

- `NpdStatusException.ResponseBody` — тело ответа как пришло, сохраняется всегда. Ошибка приходит редко и без неё в обращении в поддержку нечего показать: незнакомый код или ответ `400`, у которого кода нет вовсе, иначе не объяснить.
- `NpdStatusOptions.CaptureRawResponseBody` и `NpdTaxpayerStatus.RawResponseBody` — тело успешного ответа по флагу, по умолчанию выключено: в нём ИНН, то есть персональные данные.

### Changed

- У `NpdStatusClient` один конструктор, второй параметр необязателен. Перегрузка сломала бы `AddHttpClient<INpdStatusClient, NpdStatusClient>`: активатор типизированного клиента отказывается выбирать между конструкторами.
- Ответ `200` с телом, которое не разобралось, теперь тоже `NpdStatusException` — с телом внутри, а не `NullReferenceException` выше по стеку.

## 1.0.0

### Added

- `FnsNet.NpdStatus`: a typed client for the FNS public service that reports whether an ИНН belongs to a self-employed taxpayer (налог на профессиональный доход) on a given date.
- `AddNpdStatusClient` registers the client through `IHttpClientFactory` and returns the `IHttpClientBuilder`, so retries, rate limiting and logging stay the application's choice.
- `NpdStatusException` carries the service's own `Code` and `ServiceMessage`, and an `IsTransient` flag distinguishing a rejected input from a service that is merely busy.
