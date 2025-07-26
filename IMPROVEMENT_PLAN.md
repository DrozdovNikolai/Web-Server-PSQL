# План модернизации Postgres Control Plane

Этот документ описывает работы, необходимые для превращения учебного проекта в безопасный, воспроизводимый и презентабельный open-source проект для GitHub-профиля.

Рабочее название выбрано: **Postgres Control Plane**. Solution, проекты, namespaces, контейнеры и UI уже приведены к этому имени.

## Содержание

- [Цель проекта](#цель-проекта)
- [Текущее состояние](#текущее-состояние)
- [Статус реализации](#статус-реализации)
- [P0: обязательные работы до публикации и хостинга](#p0-обязательные-работы-до-публикации-и-хостинга)
- [P1: восстановление качества и архитектуры](#p1-восстановление-качества-и-архитектуры)
- [P2: портфолио, эксплуатация и хостинг](#p2-портфолио-эксплуатация-и-хостинг)
- [Целевая структура репозитория](#целевая-структура-репозитория)
- [Стратегия тестирования](#стратегия-тестирования)
- [CI/CD](#cicd)
- [План реализации по этапам](#план-реализации-по-этапам)
- [Definition of Done](#definition-of-done)

## Цель проекта

Проект позиционируется как security-first control plane, который:

- обнаруживает PostgreSQL routines и автоматически выбирает перегрузку по именованным JSON-аргументам, не принимая SQL или обязательные типы от клиента;
- раскрывает результаты `refcursor` как JSON и формирует из них настраиваемые Excel-отчёты;
- предоставляет защищённый read-only API для просмотра схем, таблиц, triggers и страниц данных;
- управляет пользователями и ролями самого приложения;
- ведёт безопасный аудит операций;
- при явном включении разворачивает namespaced экземпляры приложения в Kubernetes;
- предоставляет административную панель на Vue.

Короткое описание для README:

> A security-first .NET and Vue gateway that turns allowlisted PostgreSQL routines and cursor-backed reports into typed REST and Excel exports.

Публичная демонстрация не должна предоставлять посетителю произвольный SQL-доступ, PostgreSQL `SUPERUSER` или возможность изменять настоящий Kubernetes-кластер.

## Текущее состояние

Аудит выполнен 26 июля 2026 года. На момент аудита обнаружено следующее:

- JWT signing key захардкожен как минимум в `Program.cs` и `AuthController.cs`;
- tracked-конфигурация содержит секреты, пароли или чувствительные параметры подключения;
- секретные значения и адрес инфраструктуры присутствуют в Git-истории;
- часть административных endpoints не требует авторизации;
- endpoint пользователей может возвращать EF entities с `PasswordHash`;
- endpoint токенов возвращает сырые access tokens;
- модель контейнера содержит пароли БД и возвращается из API;
- request logging сохраняет тела запросов и ответов, включая потенциальные токены и персональные данные;
- API принимает произвольные SQL-фрагменты и интерполирует идентификаторы в SQL;
- приложение при старте выполняет привилегированный SQL и пытается назначать `SUPERUSER`;
- файловые endpoints допускают анонимную загрузку, список и скачивание файлов;
- загруженным файлам и каталогам назначаются права `777`;
- Kubernetes-конфигурация использует `ClusterRole`, default ServiceAccount, privileged init container и `hostPath`;
- `SuperHeroAPI/md2` удалён, но ссылки на namespace остались: чистый `HEAD` не является воспроизводимо собираемым;
- присутствуют `WeatherForecast`, `SuperHero` и другое шаблонное или legacy-содержимое;
- контроллеры достигают 500–2000 строк и содержат HTTP, SQL и бизнес-логику одновременно;
- отсутствуют автоматические тесты и GitHub Actions;
- отсутствуют `.env.example`, `.dockerignore`, Compose-конфигурация и лицензия;
- README содержит устаревшие команды и ссылки на отсутствующие скрипты;
- скомпилированный `admin-panel/dist` хранится в Git;
- `npm audit` по lock-файлу сообщил о 15 уязвимостях: 5 moderate, 9 high и 1 critical;
- проект использует .NET 8, поддержка которого завершается 10 ноября 2026 года.

## Статус реализации

Основная модернизация репозитория выполнена 26 июля 2026 года. Перечень ниже сохранён как полный исходный аудит и roadmap, а фактический результат описан в этой таблице.

| Область | Статус | Результат |
|---|---|---|
| Legacy и структура | Готово | Удалены Weather/SuperHero, arbitrary SQL, архивирование и старые контроллеры; solution переименован и разделён на `src`/`tests`. |
| Secrets и конфигурация | Готово в текущем дереве | Реальные значения удалены; добавлены typed options, startup validation, `.env.example`, ignore rules и пример Kubernetes Secret. |
| Auth и API | Готово | Deny-by-default, короткий JWT с `jti`, server-side revocation, rate limit, DTO-only responses, `ProblemDetails`, pagination и защита последнего администратора. |
| PostgreSQL | Готово | Gateway со стабильным адресом schema/name, именованными JSON-аргументами, автоматическим overload resolution, натуральными JSON-ответами, транзакционным `refcursor`, Excel-экспортом, allowlist схем и независимыми лимитами; типы и OID не требуются обычному клиенту. |
| Audit и files | Готово | Metadata-only audit с correlation ID и retention; uploads закрыты policy, ограничены и проверяются по MIME/signature/Office structure. |
| Kubernetes | Готово как opt-in adapter | Namespaced RBAC без list/watch, restricted pods, PVC, NetworkPolicy, quota/limits, TLS-only public ingress и идемпотентный rollback. |
| Frontend | Готово | Новый responsive Vue UI, Routine Gateway без OID, формы аргументов и Excel, sessionStorage auth, route guards, logout/revocation, error/loading/empty states и CSP. |
| Build и supply chain | Готово | .NET 10/Node 22, lock-файлы, Compose, non-root images, tests, formatting, audits, Gitleaks, CodeQL, Trivy, Dependabot и OpenAPI generation. |
| Документация | Готово, кроме медиа/лицензии | README, architecture, threat model, ADR, deployment/backup guide, SECURITY, CONTRIBUTING и CHANGELOG добавлены. |

### Зафиксированный результат проверки

Проверки повторно выполнены 27 июля 2026 года из текущего рабочего дерева:

- `dotnet restore --locked-mode`, formatting и clean Release build прошли с 0 warnings/0 errors;
- backend: 14 из 14 HTTP/security/PostgreSQL integration tests прошли, включая реальный Testcontainer и матрицу автоматического разрешения overload по именам, JSON-типам, defaults и числовым диапазонам;
- frontend: lint, 12 из 12 session/navigation/routine/result/Excel tests и production build прошли;
- NuGet и npm audits не обнаружили известных уязвимостей;
- EF Core сгенерировал идемпотентный migration script, OpenAPI 3.0.4 содержит 23 path, включая routine invocation и Excel export;
- production Docker images API и web успешно собраны и запускаются от непривилегированных пользователей;
- Compose smoke-test подтвердил вызов без сигнатуры, автоматический выбор `integer`/`text`, структурированный `409` для `null`, натуральный scalar result, `refcursor` как JSON, валидный `.xlsx`, procedure `204`, поле `overload` без OID в клиентском каталоге и web `200`;
- Compose-конфигурация и 12 Kubernetes manifest-файлов проходят локальную валидацию;
- временные smoke-test containers и volumes после проверки удалены.

Остаются действия, которые нельзя безопасно выполнить только изменением кода:

- проверить старую публичную инсталляцию и access logs, затем ротировать реальные внешние credentials;
- очистить публичную Git-историю и включить secret scanning/push protection в настройках GitHub;
- выбрать владельцем проекта лицензию (рекомендуемый простой вариант — MIT);
- развернуть отдельные demo database/frontend/API, настроить domain, HTTPS, backups, alerts и бюджет;
- после реального запуска снять честный screenshot/GIF и выполнить smoke-тест в браузере и ephemeral Kubernetes cluster.

Полная инвентаризация исходных бизнес-возможностей и решения по каждой из них находятся в [`docs/business-capability-inventory.md`](docs/business-capability-inventory.md). Она является обязательным regression checklist для следующих рефакторингов.

Осознанные non-goals текущего demo: arbitrary SQL/DDL и создание routines через HTTP. Вызов уже существующих allowlisted функций/процедур, `refcursor` и Excel-отчёты являются главными возможностями продукта; PostgreSQL `SUPERUSER`, long-lived refresh tokens и доступ demo-пользователя к реальному Kubernetes cluster не предоставляются.

## P0: обязательные работы до публикации и хостинга

### 1. Закрыть потенциально работающую публичную инсталляцию

- [ ] Проверить, работает ли URL, указанный в README.
- [ ] До исправления P0 закрыть приложение reverse-proxy авторизацией, IP allowlist или полностью остановить.
- [ ] Не проверять безопасность production-инсталляции изменяющими запросами.
- [ ] Проверить access-логи на обращения к endpoints пользователей, токенов, контейнеров, файлов и произвольного SQL.
- [ ] При признаках постороннего доступа считать все доступные приложению секреты скомпрометированными.

### 2. Ротировать и вынести секреты

- [ ] Заменить JWT signing key независимо от того, был ли репозиторий публичным.
- [ ] Заменить пароли PostgreSQL, административного пользователя и Kubernetes secrets.
- [ ] Проверить и заменить другие credentials из `appsettings*.json`, PowerShell-скриптов и YAML.
- [ ] Удалить реальные секреты из tracked-файлов.
- [ ] Добавить `.env`, `appsettings.Local.json`, локальные secrets и сертификаты в `.gitignore`.
- [ ] Добавить безопасный `.env.example` только с названиями переменных и фиктивными значениями.
- [ ] Для локальной разработки использовать .NET User Secrets или `.env`.
- [ ] Для CI использовать GitHub Actions Secrets/Environments.
- [ ] Для production использовать secret storage выбранной платформы или Kubernetes Secrets с внешним secret manager.
- [ ] Не считать base64 в Kubernetes Secret шифрованием.

Минимальный набор настроек:

```text
ConnectionStrings__MainDatabase=
Jwt__SigningKey=
Jwt__Issuer=
Jwt__Audience=
Jwt__AccessTokenLifetimeMinutes=
Cors__AllowedOrigins__0=
BootstrapAdmin__Username=
BootstrapAdmin__Password=
```

### 3. Очистить публичную Git-историю

- [ ] Сохранить исходный дипломный репозиторий как private archive.
- [ ] Выбрать один из вариантов публикации:
  - создать новый публичный репозиторий из очищенного состояния;
  - переписать историю с помощью `git filter-repo` и выполнить coordinated force push.
- [ ] До переписывания истории ротировать секреты: удаление из Git не отменяет компрометацию.
- [ ] После очистки проверить все refs, tags и remote branches secret scanner'ом.
- [ ] Включить GitHub secret scanning и push protection, если они доступны для репозитория.
- [ ] Добавить локальную и CI-проверку через Gitleaks или TruffleHog.

Для портфолио предпочтителен новый очищенный репозиторий с понятной серией содержательных коммитов. Старую историю можно сохранить приватно как академический архив.

### 4. Восстановить сборку из чистого клона

- [ ] Определить, какие типы из удалённого `SuperHeroAPI/md2` действительно относятся к продукту.
- [ ] Удалить оставшиеся зависимости от legacy-моделей либо вернуть минимально необходимые модели в корректные модули.
- [ ] Пересоздать или привести в соответствие EF Core migrations и model snapshot.
- [ ] Удалить `using SuperHeroAPI.md2` из актуального кода.
- [ ] Удалить package references, которые больше не используются.
- [ ] Добавить `global.json` с поддерживаемой версией .NET SDK.
- [ ] Зафиксировать поддерживаемую версию Node.js через `.nvmrc` или `.node-version`.
- [ ] Добиться успешного выполнения из чистого клона:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
npm ci
npm run lint
npm run build
docker compose up --build
```

### 5. Ввести авторизацию по принципу deny by default

- [ ] Настроить fallback authorization policy: все endpoints требуют аутентификацию по умолчанию.
- [ ] Использовать `[AllowAnonymous]` только для login, readiness/liveness и явно публичной документации.
- [ ] Закрыть `UserController`, `RoleController`, `PermissionController`, `UserRoleController` и `ContainersController` административной policy.
- [ ] Запретить анонимную регистрацию либо сделать её конфигурируемой только для demo/dev.
- [ ] Закрыть update пользователя от IDOR: пользователь изменяет себя, администратор — любого пользователя.
- [ ] Ограничить просмотр активных сессий и отзыв токенов административной policy.
- [ ] Разделить application roles и PostgreSQL roles; не доверять произвольному названию роли из JWT.
- [ ] Добавить явные policies: `Users.Read`, `Users.Manage`, `Database.Query`, `Database.Ddl`, `Deployments.Manage`, `Audit.Read`.
- [ ] Удалить тестовую policy и placeholder permission value из `Program.cs`.
- [ ] Добавить тесты, подтверждающие `401` и `403` для каждого защищённого сценария.

### 6. Исправить JWT и управление сессиями

- [ ] Перенести JWT-конфигурацию в typed options с validation on startup.
- [ ] Включить `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime` и `ValidateIssuerSigningKey`.
- [ ] Настроить небольшой `ClockSkew` и короткий срок access token.
- [ ] Добавлять `sub`, `jti`, имя пользователя и нормализованные role/permission claims.
- [ ] Не печатать bearer token в консоль и structured logs.
- [ ] Не хранить полный access token в БД.
- [ ] Если нужен logout/revocation, хранить хеш refresh token или server-side session identifier.
- [ ] Реализовать refresh token rotation и обнаружение повторного использования.
- [ ] Отзывать сессии при смене пароля или критическом изменении ролей.
- [ ] Удалить дублирующую ручную JWT-валидацию из контроллера.
- [ ] Возвращать одинаковую ошибку при неверном имени пользователя и пароле.
- [ ] Добавить rate limit на login и регистрацию.

### 7. Перестать возвращать EF entities наружу

- [ ] Создать отдельные DTO для users, roles, permissions, deployments и tokens.
- [ ] Исключить из любых ответов `PasswordHash`, `DbPassword`, `DbPasswordUser`, полный connection string и token value.
- [ ] Исключить sensitive fields из model binding: клиент не должен управлять server-owned полями.
- [ ] Не использовать `ReferenceHandler.IgnoreCycles` как замену корректным контрактам API.
- [ ] Добавить mapping между domain/entity и API contracts.
- [ ] Возвращать `ProblemDetails` для ошибок вместо stack trace и внутренних сообщений PostgreSQL.
- [ ] Добавить pagination для списков пользователей, логов, файлов и deployments.

### 8. Сделать аудит безопасным

- [ ] По умолчанию не сохранять тела запросов и ответов.
- [ ] Логировать operation name, status, duration, correlation ID, actor ID и resource ID.
- [ ] Создать allowlist полей, которые разрешено сохранять.
- [ ] Маскировать `Authorization`, cookies, passwords, tokens, connection strings и SQL parameters.
- [ ] Не парсить непроверенный JWT вручную ради имени пользователя.
- [ ] Не загружать большие ответы полностью в память.
- [ ] Не допускать, чтобы ошибка сохранения audit log ломала или скрывала основной HTTP-ответ.
- [ ] Добавить retention policy и фоновую очистку audit logs.
- [ ] Ограничить доступ к аудиту отдельной policy.
- [ ] Удалить дублирующий или неиспользуемый logging middleware.

### 9. Ограничить выполнение SQL

Произвольный SQL является центральной, но наиболее опасной возможностью проекта.

- [ ] Не выполнять пользовательский SQL от владельца БД или `SUPERUSER`.
- [ ] Удалить автоматическую выдачу PostgreSQL `SUPERUSER` из `DatabaseSetupService`.
- [ ] Создать отдельные DB-роли для чтения, записи, DDL и администрирования.
- [ ] Использовать параметры Npgsql для всех значений.
- [ ] Для имён schema/table/function использовать строгий parser/allowlist и корректное quoting идентификаторов.
- [ ] Не конкатенировать role name, table name и пользовательский query fragment напрямую.
- [ ] Ограничить разрешённые statement types для каждого endpoint.
- [ ] Разделить read-only query endpoint и privileged DDL workflow.
- [ ] Добавить `statement_timeout`, ограничение количества строк и максимального размера ответа.
- [ ] Передавать `CancellationToken` во все DB-вызовы.
- [ ] Выполнять операции в транзакции и гарантированно делать `RESET ROLE` в `finally`.
- [ ] Не возвращать пользователю сырые сообщения PostgreSQL, содержащие детали схемы.
- [ ] В demo-режиме разрешить только read-only запросы к seed database.
- [ ] Добавить integration-тесты SQL injection и privilege escalation.

### 10. Закрыть файловую подсистему

- [ ] Удалить `[AllowAnonymous]` с upload, list и download либо отделить действительно публичные файлы.
- [ ] Ограничить размер запроса на reverse proxy и в ASP.NET Core.
- [ ] Ввести allowlist MIME types и расширений.
- [ ] Проверять magic bytes, не доверять `Content-Type` и исходному имени.
- [ ] Хранить файл под server-generated identifier без пользовательского имени в пути.
- [ ] Не возвращать физический путь файла в API.
- [ ] Удалить вызовы `chmod 777` и debug-файл в upload directory.
- [ ] Запускать приложение от non-root пользователя с минимальными правами.
- [ ] Для production использовать S3-compatible object storage или отдельный ограниченный volume.
- [ ] При необходимости добавить malware scanning и quarantine workflow.
- [ ] Для скачивания использовать streaming, а не `ReadAllBytes`.
- [ ] Добавить lifecycle/retention и безопасное удаление файлов.

### 11. Исправить Kubernetes security model

- [ ] Не предоставлять Kubernetes management API анонимному пользователю.
- [ ] Создать отдельный ServiceAccount вместо `default`.
- [ ] Заменить `ClusterRole` на namespace-scoped `Role`.
- [ ] Удалить доступ к `nodes`, cluster-wide secrets и другим ненужным ресурсам.
- [ ] Разделить read и write permissions.
- [ ] Запретить privileged containers и privilege escalation.
- [ ] Настроить `runAsNonRoot`, `readOnlyRootFilesystem`, seccomp и drop capabilities.
- [ ] Удалить `hostPath`; использовать PVC или object storage.
- [ ] Не использовать `chmod 777` в init container.
- [ ] Добавить CPU/memory requests и limits.
- [ ] Добавить readiness, liveness и startup probes.
- [ ] Добавить NetworkPolicy и ограничить egress к нужной БД.
- [ ] Добавить ResourceQuota и LimitRange для demo namespaces.
- [ ] Валидировать Kubernetes resource names и исключить injection через имя контейнера.
- [ ] Не сохранять DB credentials в основной таблице deployments в открытом виде.
- [ ] Хранить только ссылку на secret/version, а не secret value.
- [ ] Обрабатывать частичный сбой как workflow/saga: корректно очищать уже созданные ресурсы.
- [ ] Сделать операции идемпотентными.
- [ ] Использовать фиксированные immutable image tags или digest вместо `latest`.
- [ ] Добавить TLS и запретить публичные ingress без явного opt-in.

## P1: восстановление качества и архитектуры

### 12. Определить продуктовый scope

- [ ] Решить, является ли основным продуктом PostgreSQL management API, Kubernetes orchestrator или их связка.
- [ ] Вынести экспериментальные функции за feature flags.
- [ ] Удалить код, относящийся только к старой университетской предметной области.
- [ ] Удалить `WeatherForecastController`, `WeatherForecast`, `SuperHeroController`, `SuperHero` и соответствующий service layer.
- [ ] Удалить незавершённый archiving code либо довести его до поддерживаемого модуля с тестами.
- [ ] Удалить дублирующие models, DTO и endpoints.
- [ ] Зафиксировать non-goals в README.

### 13. Переименовать проект

- [ ] Выбрать уникальное название и проверить отсутствие очевидного конфликта с известными продуктами.
- [ ] Переименовать solution и `.csproj`.
- [ ] Переименовать namespaces `SuperHeroAPI` и `PostgreSQL.Data`.
- [ ] Обновить Docker image, Kubernetes labels, OpenAPI title и frontend package name.
- [ ] Удалить доменные пути и hostname конкретного сервера из исходного кода.
- [ ] Использовать нейтральные настройки storage path, base path и public URL.

### 14. Перейти на поддерживаемый .NET LTS

- [ ] После стабилизации тестов обновить backend с .NET 8 до .NET 10 LTS.
- [ ] Обновить ASP.NET Core, EF Core, Npgsql и совместимые Microsoft packages согласованно.
- [ ] Удалить прямые references на пакеты, уже входящие в shared framework, если они не нужны.
- [ ] Не смешивать несовместимые major versions `Microsoft.IdentityModel.*`.
- [ ] Выполнить `dotnet list package --outdated`.
- [ ] Выполнить `dotnet list package --vulnerable --include-transitive`.
- [ ] Включить lock file или central package management для воспроизводимости.

Официальный lifecycle: <https://dotnet.microsoft.com/en-us/platform/support/policy>.

### 15. Разделить ответственность кода

Рекомендуемая модульная структура:

```text
src/
  PostgresControl.Api/
  PostgresControl.Application/
  PostgresControl.Domain/
  PostgresControl.Infrastructure/
web/
  admin-panel/
tests/
  PostgresControl.UnitTests/
  PostgresControl.IntegrationTests/
  PostgresControl.ArchitectureTests/
deploy/
  docker/
  kubernetes/
```

- [ ] Оставить в controllers только HTTP contracts, authorization и вызов application service.
- [ ] Перенести Npgsql/EF Core в Infrastructure.
- [ ] Перенести Kubernetes client за интерфейс `IDeploymentProvisioner`.
- [ ] Перенести file storage за `IFileStorage`.
- [ ] Сгруппировать код по features: Auth, Users, DatabaseObjects, Queries, Deployments, Files, Audit.
- [ ] Избегать service/repository abstractions, которые только повторяют методы EF Core без добавочной логики.
- [ ] Разбить контроллеры размером 500–2000 строк на небольшие сценарии.
- [ ] Включить nullable warnings и устранить предупреждения.
- [ ] Добавить `.editorconfig` и единый code style.
- [ ] Включить analyzers и запретить новые compiler warnings в CI.

### 16. Исправить конфигурацию приложения

- [ ] Удалить `Env.Load()` как обязательный production-механизм; использовать стандартный `IConfiguration` pipeline.
- [ ] Создать typed options: `JwtOptions`, `CorsOptions`, `DatabaseOptions`, `StorageOptions`, `KubernetesOptions`.
- [ ] Валидировать обязательные настройки при старте.
- [ ] Убрать `if (true)` вокруг Swagger.
- [ ] Включать Swagger только в development или защищать отдельной policy.
- [ ] Заменить `AllowAnyOrigin` на allowlist.
- [ ] Настроить forwarded headers и trusted proxies для корректных scheme/host/IP.
- [ ] Убрать глобальный жёсткий `UsePathBase("/server")` либо сделать его настройкой.
- [ ] Удалить повторные вызовы `AddControllers`.
- [ ] Использовать `TimeProvider` для тестируемой работы со временем.
- [ ] Привести все timestamps к UTC и не включать legacy timestamp behavior без необходимости.

### 17. Привести API к единому контракту

- [ ] Использовать resource-oriented routes и согласованные имена.
- [ ] Добавить API versioning.
- [ ] Возвращать корректные status codes: `201`, `204`, `400`, `401`, `403`, `404`, `409`, `422`.
- [ ] Добавить единый exception handler и `ProblemDetails`.
- [ ] Не возвращать `ex.Message` клиенту для `500`.
- [ ] Добавить validation errors в стандартном формате.
- [ ] Добавить OpenAPI descriptions, examples и security scheme Bearer.
- [ ] Удалить комментарии, скопированные из шаблонов, и исправить повреждённую кодировку.
- [ ] Исправить опечатки в именах файлов и классов, например `PermissionContoller.cs`.
- [ ] Ввести cancellation и timeout для долгих endpoints.
- [ ] Добавить idempotency для deployment creation при повторном запросе.

### 18. Привести БД и миграции в порядок

- [ ] Выбрать Code First migrations либо документированный SQL-first подход и не смешивать их хаотично.
- [ ] Удалить model snapshot с удалёнными legacy entities.
- [ ] Разбить огромный `creationscript.txt` на версионируемые миграции.
- [ ] Не выполнять монолитный SQL-файл автоматически при каждом старте API.
- [ ] Создать отдельный migrator job/command.
- [ ] Сделать bootstrap admin явной одноразовой операцией.
- [ ] Не логировать bootstrap password.
- [ ] Добавить constraints, индексы, foreign keys и delete behavior там, где они необходимы.
- [ ] Не хранить access token и DB passwords как plaintext.
- [ ] Добавить optimistic concurrency для изменяемых административных ресурсов.
- [ ] Добавить backup/restore инструкцию для demo и production.

### 19. Обновить frontend

- [ ] Обновить npm dependencies и устранить результаты `npm audit`.
- [ ] Проверить breaking changes Vite, Vue, Vue Router, Vue I18n и PrimeVue.
- [ ] Удалить реальный hostname/API URL из source defaults.
- [ ] Добавить `.env.example` с `VITE_API_URL`.
- [ ] Не хранить DB password или административный password как frontend default.
- [ ] Добавить authentication flow и route guards.
- [ ] Добавить Axios interceptor для bearer token, `401` и correlation ID.
- [ ] Не хранить долгоживущие токены в небезопасном persistent storage без оценки XSS-рисков.
- [ ] Добавить loading, empty и error states для основных экранов.
- [ ] Показывать безопасные DTO без secret fields.
- [ ] Добавить confirmation для destructive operations.
- [ ] Добавить доступные labels, keyboard navigation и responsive layout.
- [ ] Добавить frontend unit/component tests.
- [ ] Добавить e2e smoke test критического user flow.
- [ ] Удалить `admin-panel/dist` из Git и собирать его в CI/deployment image.
- [ ] Исправить README-команду: текущий package использует `npm run dev`, а не `npm run serve`.

### 20. Улучшить Docker image

- [ ] Добавить `.dockerignore` и исключить `.git`, `bin`, `obj`, `node_modules`, `dist`, secrets и IDE-файлы.
- [ ] Обновить base images до выбранной поддерживаемой версии .NET.
- [ ] Запускать final image от non-root пользователя.
- [ ] Явно настроить порт приложения.
- [ ] Добавить health check на `/health/live`.
- [ ] Использовать `dotnet publish --no-restore` после отдельного restore.
- [ ] Не включать SDK и лишние инструменты в final image.
- [ ] Не копировать секреты и environment-specific файлы в image layers.
- [ ] Публиковать image с immutable version/commit SHA tag.
- [ ] Сканировать image через Trivy, Grype или GitHub code scanning integration.
- [ ] Генерировать SBOM и provenance при release build.

### 21. Сделать локальный запуск одной командой

- [ ] Добавить `compose.yaml`.
- [ ] Включить PostgreSQL с health check и persistent development volume.
- [ ] Включить API с безопасными demo credentials из `.env.example`.
- [ ] Включить frontend или настроить Vite proxy для development.
- [ ] Добавить seed database без персональных и production-данных.
- [ ] Добавить fake Kubernetes adapter по умолчанию.
- [ ] Добавить profile для подключения к локальному Minikube/kind только по явному выбору.
- [ ] Добавить `make`, `just` или PowerShell/Bash scripts с одинаковыми командами для setup, test и run.
- [ ] Проверить запуск на Windows/WSL и Linux из чистого клона.

## P2: портфолио, эксплуатация и хостинг

### 22. Переделать README

- [ ] Переименовать заголовок и убрать `SuperHero API`.
- [ ] Добавить одну ясную фразу о ценности проекта.
- [ ] Добавить hero screenshot административной панели.
- [ ] Добавить короткий GIF основного сценария.
- [ ] Добавить architecture diagram.
- [ ] Описать ключевые features и security model.
- [ ] Добавить quick start не более чем на 5–7 команд.
- [ ] Добавить demo URL и безопасные demo credentials, если они нужны.
- [ ] Добавить ссылку на Swagger/OpenAPI только если документация безопасно доступна.
- [ ] Добавить таблицу технологий и объяснить архитектурные решения.
- [ ] Добавить разделы `Limitations`, `Roadmap`, `Security` и `Contributing`.
- [ ] Добавить build/test/security badges только после появления работающего CI.
- [ ] Удалить ссылки на отсутствующие `deploy-container.ps1` и `test-container-access.ps1` либо реализовать их.
- [ ] Исправить противоречия по портам, ingress paths и командам frontend.

Рекомендуемая структура README:

```text
Title + one-line pitch
Screenshot/GIF
Why this project exists
Features
Architecture
Security model
Quick start
Demo
API examples
Tests and CI
Deployment
Limitations
Roadmap
License
```

### 23. Добавить документацию проекта

- [ ] `SECURITY.md` с каналом private disclosure и supported versions.
- [ ] `CONTRIBUTING.md` с setup, tests и code style.
- [ ] `LICENSE` с выбранной open-source лицензией.
- [ ] `CHANGELOG.md` либо GitHub Releases.
- [ ] ADR для ключевых решений: arbitrary SQL policy, JWT/session model, Kubernetes adapter, secret storage.
- [ ] OpenAPI specification, генерируемая и проверяемая в CI.
- [ ] Threat model с trust boundaries и наиболее опасными flows.
- [ ] Runbook для rotate secrets, rollback и восстановления БД.

### 24. Добавить observability

- [ ] Использовать structured logging через `ILogger`/Serilog без sensitive payloads.
- [ ] Добавить correlation/request ID.
- [ ] Добавить `/health/live` и `/health/ready`.
- [ ] Проверять readiness БД отдельно от liveness процесса.
- [ ] Добавить базовые metrics: request duration, error rate, DB duration, deployment duration.
- [ ] При необходимости добавить OpenTelemetry traces и metrics.
- [ ] Не включать usernames, SQL text и filenames в metrics labels.
- [ ] Добавить alerting для ошибок login, deployment failures и исчерпания ресурсов.

### 25. Безопасный публичный demo mode

- [ ] Использовать отдельную seed database без production-данных.
- [ ] Выдавать demo-пользователю только read-only PostgreSQL role.
- [ ] Запретить произвольный DDL и privileged SQL.
- [ ] Ограничить частоту и длительность запросов.
- [ ] Ограничить результат по строкам и размеру.
- [ ] Использовать fake Kubernetes adapter или заранее подготовленный sandbox.
- [ ] Если создаются реальные ресурсы, использовать отдельный namespace на сессию, quota и автоматический TTL cleanup.
- [ ] Не давать demo-инстансу доступ к cluster secrets и nodes.
- [ ] Добавить кнопку reset demo state.
- [ ] Отображать в UI, какие операции являются симуляцией.

### 26. Хостинг

Рекомендуемый первый вариант:

- frontend как static site;
- backend как Docker web service;
- managed PostgreSQL;
- fake/disabled Kubernetes provisioner;
- secrets в настройках hosting platform;
- custom domain и HTTPS;
- автоматический deployment только после успешного CI.

Render поддерживает deployment из Dockerfile и managed PostgreSQL:

- <https://render.com/docs/docker>
- <https://render.com/docs/postgresql>

Полнофункциональный Kubernetes demo следует рассматривать только после закрытия всех P0 и Kubernetes security checklist. Для портфолио безопаснее показать реальную Kubernetes-часть через диаграмму, тесты, GIF и локальный kind/Minikube quick start.

- [ ] Выбрать hosting platform после оценки текущих цен и ограничений.
- [ ] Разделить development, demo и production environments.
- [ ] Использовать отдельные БД и credentials для каждого environment.
- [ ] Настроить HTTPS, security headers и allowed origins.
- [ ] Настроить automated database backups.
- [ ] Настроить health checks и rollback.
- [ ] Не хранить загруженные файлы на ephemeral filesystem.
- [ ] Добавить budget/usage alerts.

## Целевая структура репозитория

```text
.
├── .github/
│   ├── dependabot.yml
│   └── workflows/
│       ├── backend.yml
│       ├── frontend.yml
│       ├── security.yml
│       └── release.yml
├── deploy/
│   ├── docker/
│   └── kubernetes/
├── docs/
│   ├── architecture.md
│   ├── threat-model.md
│   └── adr/
├── src/
│   ├── PostgresControl.Api/
│   ├── PostgresControl.Application/
│   ├── PostgresControl.Domain/
│   └── PostgresControl.Infrastructure/
├── tests/
│   ├── PostgresControl.UnitTests/
│   ├── PostgresControl.IntegrationTests/
│   └── PostgresControl.ArchitectureTests/
├── web/
│   └── admin-panel/
├── .dockerignore
├── .editorconfig
├── .env.example
├── compose.yaml
├── global.json
├── LICENSE
├── README.md
├── SECURITY.md
└── PostgresControl.sln
```

## Стратегия тестирования

### Unit tests

- [ ] JWT/session creation и validation.
- [ ] Authorization policies.
- [ ] Validators и identifier parsing.
- [ ] Secret redaction.
- [ ] Mapping DTO.
- [ ] Deployment workflow state transitions.
- [ ] File validation.

### Integration tests

- [ ] Использовать `WebApplicationFactory` для HTTP-level tests.
- [ ] Использовать Testcontainers PostgreSQL.
- [ ] Проверить migrations на пустой БД.
- [ ] Проверить login, refresh, logout и token revocation.
- [ ] Проверить `401/403` на всех административных endpoints.
- [ ] Проверить, что API никогда не возвращает password hashes, DB passwords и raw stored tokens.
- [ ] Проверить SQL injection payloads и identifier escaping.
- [ ] Проверить statement timeout и cancellation.
- [ ] Проверить безопасный аудит без sensitive bodies.
- [ ] Проверить upload limits и запрещённые типы файлов.

### Kubernetes tests

- [ ] Unit tests через mock/fake `IDeploymentProvisioner`.
- [ ] Проверка генерируемых manifests snapshot/schema tests.
- [ ] Integration smoke tests в ephemeral kind cluster только в отдельном CI job.
- [ ] Проверка отсутствия privileged, `hostPath`, `latest` и cluster-wide RBAC.

### Frontend tests

- [ ] Component tests для login, container list и create flow.
- [ ] Проверка route guards.
- [ ] Проверка отсутствия sensitive fields в UI.
- [ ] Playwright smoke test: login → просмотр данных → logout.

## CI/CD

### Pull request checks

- [ ] `dotnet restore`, build и test.
- [ ] `dotnet format --verify-no-changes`.
- [ ] NuGet vulnerability audit.
- [ ] `npm ci`, lint, test и build.
- [ ] npm audit с согласованным severity threshold.
- [ ] Docker build.
- [ ] Secret scanning.
- [ ] Dependency review.
- [ ] CodeQL/SAST.
- [ ] Container and filesystem vulnerability scan.
- [ ] Проверка Kubernetes manifests через kubeconform и policy scanner.

### Release pipeline

- [ ] Создавать release только из защищённой main branch.
- [ ] Использовать semantic versioning.
- [ ] Публиковать immutable container tag и digest.
- [ ] Генерировать SBOM и provenance.
- [ ] Выполнять migration как отдельный pre-deploy step.
- [ ] Выполнять smoke test после deployment.
- [ ] Автоматически откатывать либо блокировать rollout при failed health check.

### Dependency automation

- [ ] Настроить Dependabot или Renovate для NuGet, npm, Docker и GitHub Actions.
- [ ] Не объединять major upgrades автоматически.
- [ ] Требовать зелёные тесты и audit перед merge.

## План реализации по этапам

### Этап 0. Emergency security

- [ ] Закрыть публичный URL.
- [ ] Ротировать все credentials.
- [ ] Удалить secrets из текущего дерева.
- [ ] Подготовить стратегию чистого публичного репозитория.

Результат: текущие утёкшие значения больше не дают доступ к инфраструктуре.

### Этап 1. Green build

- [ ] Устранить ссылки на `md2`.
- [ ] Удалить legacy SuperHero/Weather код.
- [ ] Исправить migrations.
- [ ] Обновить зависимости с известными уязвимостями.
- [ ] Добиться чистой backend/frontend/Docker сборки.

Результат: проект собирается из чистого клона.

### Этап 2. Secure API baseline

- [ ] Deny-by-default authorization.
- [ ] Безопасный JWT/session flow.
- [ ] DTO вместо entities.
- [ ] Safe audit logging.
- [ ] Закрытые file и deployment endpoints.
- [ ] ProblemDetails и validation.

Результат: базовые security integration tests проходят.

### Этап 3. SQL и Kubernetes isolation

- [ ] Удалить runtime `SUPERUSER` bootstrap.
- [ ] Ввести безопасную модель DB-ролей.
- [ ] Ограничить SQL и добавить timeouts.
- [ ] Ввести `IDeploymentProvisioner` и fake adapter.
- [ ] Ужесточить RBAC, pod security и networking.

Результат: публичный demo не может повредить хостовую БД или кластер.

### Этап 4. Architecture and tests

- [ ] Переименовать проект.
- [ ] Разделить features и infrastructure adapters.
- [ ] Добавить unit/integration/frontend tests.
- [ ] Добавить Compose и seed data.
- [ ] Добавить CI security gates.

Результат: изменения можно безопасно проверять и принимать через pull requests.

### Этап 5. Portfolio release

- [ ] Новый README, screenshots, GIF и diagram.
- [ ] Документация security/architecture.
- [ ] Лицензия и contribution guide.
- [ ] Публичный demo mode.
- [ ] Tagged release `v1.0.0`.

Результат: репозиторий готов к закреплению в GitHub-профиле.

### Этап 6. Hosting

- [ ] Развернуть frontend, API и managed PostgreSQL.
- [ ] Настроить secrets, domain, HTTPS, backups и monitoring.
- [ ] Выполнить smoke/security checks.
- [ ] Добавить demo URL в README.

Результат: безопасная демонстрация доступна из GitHub-профиля.

## Definition of Done

Проект можно считать готовым к публичной презентации, когда выполнены все условия:

- [ ] Все известные реальные credentials ротированы.
- [ ] В текущем дереве и публичной истории нет secrets.
- [ ] Чистый clone собирается документированными командами.
- [ ] Backend, frontend и Compose стартуют без ручного редактирования исходников.
- [ ] Нет endpoints, случайно доступных анонимно.
- [ ] API не возвращает password hashes, DB passwords или stored tokens.
- [ ] Login/token responses не сохраняются целиком в audit logs.
- [ ] Публичный пользователь не выполняет privileged или произвольный SQL.
- [ ] Приложение не выдаёт PostgreSQL `SUPERUSER` при старте.
- [ ] Публичный API не управляет Kubernetes cluster-wide resources.
- [ ] Контейнеры запускаются non-root без privileged и `hostPath`.
- [ ] File upload защищён и ограничен либо выключен.
- [ ] `dotnet test`, frontend tests, lint и security checks проходят в CI.
- [ ] NuGet, npm и container scans не содержат необработанных critical/high findings.
- [ ] README точно соответствует реальным командам и возможностям.
- [ ] Есть screenshot/GIF, architecture diagram, quick start, license и security policy.
- [ ] Demo использует отдельные данные, credentials и инфраструктуру.
- [ ] Настроены HTTPS, health checks, backups и rollback.

## Рекомендуемый порядок первых изменений

1. Ротировать credentials и закрыть текущую инсталляцию.
2. Создать новую ветку модернизации или новый очищенный публичный репозиторий.
3. Восстановить green build после удаления `md2`.
4. Удалить Weather/SuperHero и другой legacy-код.
5. Включить deny-by-default authorization и перестать возвращать entities.
6. Переделать JWT, audit logging и file endpoints.
7. Убрать `SUPERUSER`, ограничить SQL и Kubernetes RBAC.
8. Добавить tests, Compose и CI.
9. Переименовать и оформить README.
10. Запустить ограниченный demo mode и только затем добавить проект в GitHub-профиль.
