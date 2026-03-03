## Настройка токенов API

# Токен для StackOverflow

Добавляется командой в этой папке:

```
dotnet user-secrets set "StackOverflow:Token" "your_token_here"
```

Так как токен не обязателен для запросов, то это делать не обязательно.

# Токен для GitHub

Создается на сайте https://github.com/settings/tokens (должен начинаться с ghp_...)

Добавляется по аналогии с токеном StackOverflow

```
dotnet user-secrets set "GitHub:Token" "your_token_here"
```

Внимание: скраппер не будет работать без токена