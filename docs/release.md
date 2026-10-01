# Порядок выпуска

Semantic Versioning.

## Перед выпуском

1. Поднять версию в `src/FnsNet.NpdStatus/FnsNet.NpdStatus.csproj` — **все пять полей**:
   `Version`, `PackageVersion`, `AssemblyVersion`, `FileVersion`, `InformationalVersion`. Иначе
   `dotnet pack` молча соберёт пакет со старым номером, а в проекте будет стоять новый.
2. Обновить `CHANGELOG.md`.
3. Обновить `README.md`, если изменился публичный API. Он вкладывается в пакет, поэтому ссылки в
   нём должны быть абсолютными.
4. Прогнать локально:

```bash
dotnet test FnsNet.NpdStatus.slnx
dotnet pack src/FnsNet.NpdStatus/FnsNet.NpdStatus.csproj --configuration Release --output artifacts/packages
```

## Публикация по тегу

```bash
git tag -a v1.0.0 -m "FnsNet.NpdStatus 1.0.0"
git push origin v1.0.0
```

Тег `v*` запускает `release.yml`: сборка, тесты, упаковка, GitHub Release с вложенными артефактами и
публикация в NuGet.org. Тег с суффиксом предрелиза (`v1.1.0-preview.1`) помечается как prerelease.

## Публикация вручную

`publish-nuget.yml` запускается через **Run workflow** и принимает `git_ref` и `version`. Пригодно,
когда тег уже есть, а публикация не прошла, или когда нужно выпустить конкретную ревизию.

## Доступ к NuGet.org

Публикация идёт через **Trusted Publishing**: GitHub выдаёт OIDC-токен, `NuGet/login@v1` меняет его
на временный ключ. Долгоживущего `NUGET_API_KEY` в секретах нет и быть не должно — ключ существует
минуты и привязан к конкретному workflow.

Нужны:

- переменная репозитория `NUGET_USER` — имя владельца пакета на NuGet.org;
- настроенная политика Trusted Publishing на NuGet.org для `ai-iskuzhin/FnsNet.NpdStatus`;
- права `id-token: write` у workflow — уже прописаны.

Политика заводится до первой публикации: пакета ещё нет, поэтому её создают как «Package pattern»
на `FnsNet.NpdStatus`.
