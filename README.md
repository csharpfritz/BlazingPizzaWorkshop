# Курсовая работа, 4-й семестр

Мосполитех, гр. 221-321, Ангел Максим Витальевич

## «Хочешь похудеть? Подари мне пиццу»

Проект на .NET 8, ASP.NET core Blazor, Redis, Postgres, Oauth2, SMTP mailer (Gmail)

Основан на  https://github.com/csharpfritz/BlazingPizzaWorkshop

Официальные обучающие видео от Microsoft доступны:  https://www.youtube.com/watch?v=sWTpxFcHbfY&list=PLdo4fOcmZ0oXv32dOd36UydQYLejKR61R&index=78 

Что добавлено в исходный проект: 
- Redis, Postgres, Oauth2, SMTP mailer (Gmail), контейнеризация сайта при публикации.
- В целях самообразования добавлены dummy-сервисы, которые можно или развивать далее, или удалить вместе с сответствующими тремя интерфейсами и классами проекта (взаимодействие клиент-сервер):

  - builder.Services.AddScoped<IOrderService, OrderService>(); (program.cs сервер)
  - builder.Services.AddScoped<IOrderService, HttpOrderService>(); (progrm.cs клиент)

## Структура проекта

- docker-compose.yml запускает 4 контейнера: Redis, Redis Commander, Postgres, PgAdmin.
- сайт Blazor на .NET 8 (фул-стек, упор на серверную часть с интегрированными API, клиент является частью проекта). При публикации сайта на хостинг командой `dotnet publish` с сответсвующими командами проект упаковывается в контейнер автоматически со всеми зависимостями, настройки контейнеризации средствами .NET SDK находятся в файле `/BlazingPizza/BlazingPizza.csproj`. 

### дополнительной ручной конфигурации перед запуском требуют:

#### Blazor:
- настройки SMTP-провайдера, в данном проекте требуется ключ приложения для Gmail (app-key);
- настройки Oauth2 ключей для GitHub;

#### Redis Commander:
-  добавить базу данных Redis, как в docker-compose.yml, просто по дефолтным настройкам Redis Commander её не будет видно. 

#### PgAdmin:
-  добавить Server, затем базу данных Postgres, как в docker-compose.yml, просто по дефолтным настройкам PgAdmin базы данных не будет видно.

## Запуск проекта на Windows

- Установить .NET 8 SDK;
- установить и запустить Podman Desktop для запуска контейнеров;
- запустить из командной строки из корневой директории проекта docker-compose.yml командой `docker-compose up -d`
- запустить сайт из командной строки из папки `BlazingPizza` командой `dotnet run` 

## Пароли

Важно! Секреты и пароли не должны попадать в Git!!! Поэтому перед использованием Git замените все пароли на переменные окружения или загружайте пароли из файлов, не входящих в Git. При разработке в Visual Studio правый щелчок мыши на папке `BlazingPizza`, опция `Manage User Secrets`.


## Файлы, где необходимо перенастроить передачу пароля по переменной окружения

- Почта SMTP: `/BlazingPizza/appsettings.json`
- Настройки провайдеров Oauth2, для демонстрации приведён GitHub, смотрите его раздел: `BlazingPizza/Program.cs`
