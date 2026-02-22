# Запуск Телеграм бота

Для запуска телеграм бота необходимо получить токен от бота @BotFather в телеграме и сохранить его в по пути "BotSetings:BotToken" в appsettings.json или командой
`dotnet user-secrets init && dotnet user-secrets set "BotSettings:BotToken" "your-token-here"`

(последний вариант с пользовательскими секретами больше рекомендуется потому что токен не хранится в коде и его нельзя случайно закоммитить в репозиторий)