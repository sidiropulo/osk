# Auth

JWT-аутентификация на ASP.NET Core Minimal API. Пользователи хранятся в PostgreSQL, а `logout` увеличивает `AuthVersion` и отзывает все ранее выданные токены пользователя.

## Запуск

```shell
docker compose up --build -d
```

После запуска доступны:

- [UI для проверки авторизации](http://localhost:5037)
- [pgAdmin](http://localhost:5050)

Остановить контейнеры:

```shell
docker compose down
```

## pgAdmin

Вход в pgAdmin:

```text
Email: admin@admin.com
Password: secret
```

Регистрация сервера в PgAdmin:

```text
Host: postgres
Port: 5432
Database: oskauth
Username: oskauth
Password: passwordstrong123
```

Таблица пользователей `Users`

## Эндпоинты

### Регистрация

```http
POST /auth/register
Content-Type: application/json

{
  "login": "osk",
  "password": "password123",
  "displayName": "osk"
}
```

Возвращает `201 Created`. При некорректных данных возвращает `400 Bad Request`, при занятом логине — `409 Conflict`.

### Вход

```http
POST /auth/login
Content-Type: application/json

{
  "login": "osk",
  "password": "password123"
}
```

Успешный ответ `200 OK`:

```json
{
  "accessToken": "JWT-токен",
  "expiresAt": "2026-09-09T18:00:00Z"
}
```

При неправильном логине или пароле возвращает `401 Unauthorized`.

### Текущий пользователь

```http
GET /whoami
Authorization: Bearer <accessToken>
```

Успешный ответ `200 OK`:

```json
{
  "id": "идентификатор пользователя",
  "login": "osk",
  "displayName": "osk"
}
```

Без действующего токена  `401 Unauthorized`.

### Выход со всех устройств

```http
POST /auth/logout
Authorization: Bearer <accessToken>
```

Возвращает `204 No Content` и увеличивает `AuthVersion` пользователя. После этого все токены, выданные с предыдущей версией, получают `401 Unauthorized` при следующем запросе.
