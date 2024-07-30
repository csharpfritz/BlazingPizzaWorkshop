<kbd style="width: 2em; height: 2em;"><a href="#en">ENGLISH</a></kbd> | <kbd><a href="#ru">РУССКИЙ</a></kbd>

<h1 name="en" id="en">Beyond Blazing Pizza eShop: .NET 8, ASP.NET Core Blazor, Redis, Postgres, OAuth2, SMTP mailer (Gmail)</h1>

Based on https://github.com/csharpfritz/BlazingPizzaWorkshop

Official Microsoft tutorial videos available at: https://www.youtube.com/watch?v=sWTpxFcHbfY&list=PLdo4fOcmZ0oXv32dOd36UydQYLejKR61R&index=78

Added to the original project:
- Redis, Postgres, OAuth2, SMTP mailer (Gmail), site containerization upon publishing.
- For self-education purposes, dummy services have been added, which can be further developed or removed along with the corresponding three interfaces and project classes (client-server interaction):

  - `builder.Services.AddScoped<IOrderService, OrderService>();` (program.cs server)
  - `builder.Services.AddScoped<IOrderService, HttpOrderService>();` (program.cs client)

## Project Structure

- `docker-compose.yml` launches 4 containers: Redis, Redis Commander, Postgres, PgAdmin.
- Blazor site on .NET 8 (full-stack, focused on the server side with integrated APIs, the client is part of the project). When publishing the site on a hosting using the `dotnet publish` command with the appropriate commands, the project is automatically packaged into a container with all dependencies, containerization settings via .NET SDK are in the file `/BlazingPizza/BlazingPizza.csproj`.

### Additional manual configuration required before launch:

#### Blazor:
- SMTP provider settings, in this project an app key for Gmail is required;
- OAuth2 key settings for GitHub;

#### Redis Commander:
- Add a Redis database as in `docker-compose.yml`, by default Redis Commander won't see it.

#### PgAdmin:
- Add a Server, then a Postgres database as in `docker-compose.yml`, by default PgAdmin won't see the database.

## Project Launch on Windows

- Install .NET 8 SDK;
- Install and launch Podman Desktop to run the containers;
- Run `docker-compose up -d` from the root directory of the project;
- Run the site from the `BlazingPizza` folder with the `dotnet run` command.

## Passwords

Important! Secrets and passwords should not get into Git!!! Therefore, before using Git, replace all passwords with environment variables or load passwords from files not included in Git. In Visual Studio, right-click on the `BlazingPizza` folder, select `Manage User Secrets`.

## Files where password transmission via environment variables needs to be reconfigured

- SMTP Mail: `/BlazingPizza/appsettings.json`
- OAuth2 provider settings, GitHub example provided, see its section: `BlazingPizza/Program.cs`

## Free Customized GPT for Consultations

- [ASP.NET Core Blazor](https://chatgpt.com/g/g-CgZBXGHdH-asp-net-blazor "ASP.NET Blazor GPT")
- [God's C#](https://chatgpt.com/g/g-Ild4ouEke-god-s-c "C# GPT")
- [God's Docker](https://chatgpt.com/g/g-MS7I12iLc-god-s-docker "Docker GPT")
- [God's PostgreSQL](https://chatgpt.com/g/g-PfbNDcNso-god-s-postgresql "PostreSQL GPT")
- [God's JavaScript](https://chatgpt.com/g/g-gJ75YnrSy-god-s-javascript "JavaScript GPT")
- [God's CSS](https://chatgpt.com/g/g-FlIKrZIMv-god-s-css "CSS GPT")
- [God's HTML](https://chatgpt.com/g/g-PhvWYdSRA-god-s-html "HTML GPT")
- [PowerShell Breaker](https://chatgpt.com/g/g-5zIR2fcma-powershell-breaker "PowerShell GPT")

Add stars to the pepositories!!!

<hr>
<h1 name="ru" id="ru">Нечто большее, чем онлайн-пиццерия: .NET 8, ASP.NET core Blazor, Redis, Postgres, Oauth2, SMTP mailer (Gmail)</h1>

Проект основан на  https://github.com/csharpfritz/BlazingPizzaWorkshop

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

## Бесплатные кастомизированные GPT для консультаций

- [ASP.NET Core Blazor](https://chatgpt.com/g/g-CgZBXGHdH-asp-net-blazor "ASP.NET Blazor GPT")
- [God's C#](https://chatgpt.com/g/g-Ild4ouEke-god-s-c "C# GPT")
- [God's Docker](https://chatgpt.com/g/g-MS7I12iLc-god-s-docker "Docker GPT")
- [God's PostgreSQL](https://chatgpt.com/g/g-PfbNDcNso-god-s-postgresql "PostreSQL GPT")
- [God's JavaScript](https://chatgpt.com/g/g-gJ75YnrSy-god-s-javascript "JavaScript GPT")
- [God's CSS](https://chatgpt.com/g/g-FlIKrZIMv-god-s-css "CSS GPT")
- [God's HTML](https://chatgpt.com/g/g-PhvWYdSRA-god-s-html "HTML GPT")
- [PowerShell Breaker](https://chatgpt.com/g/g-5zIR2fcma-powershell-breaker "PowerShell GPT")

<h2 style="margin: 0 auto" align="center">Put stars on GitHub and share!!!</h2>
<br>
<p style="margin: 0 auto" align="center">Please cast an eye on my website:</p>
<h1><a href="https://nakigoe.org/" style="background-color: black;" target="_blank">
  <img src="https://nakigoe.org/_IMG/logo.png" 
    srcset="https://nakigoe.org/_IMG/logo.png 4800w,
      https://nakigoe.org/_SRC/logo-3840.png 3840w,
      https://nakigoe.org/_SRC/logo-2560.png 2560w,
      https://nakigoe.org/_SRC/logo-2400.png 2400w,
      https://nakigoe.org/_SRC/logo-2048.png 2048w,
      https://nakigoe.org/_SRC/logo-1920.png 1920w,
      https://nakigoe.org/_SRC/logo-1600.png 1600w,
      https://nakigoe.org/_SRC/logo-1440.png 1440w,
      https://nakigoe.org/_SRC/logo-1280.png 1280w,
      https://nakigoe.org/_SRC/logo-1200.png 1200w,
      https://nakigoe.org/_SRC/logo-1080.png 1080w,
      https://nakigoe.org/_SRC/logo-960.png 960w,
      https://nakigoe.org/_SRC/logo-720.png 720w,
      https://nakigoe.org/_SRC/logo-600.png 600w,
      https://nakigoe.org/_SRC/logo-480.png 480w,
      https://nakigoe.org/_SRC/logo-300.png 300w"
    sizes="100vw" 
    alt="NAKIGOE.ORG">
<img src="https://nakigoe.org/_IMG/nakigoe-academy-night.jpg" 
  srcset="https://nakigoe.org/_IMG/nakigoe-academy-night.jpg 2800w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-2560.jpg 2560w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-2048.jpg 2048w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-1920.jpg 1920w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-1600.jpg 1600w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-1440.jpg 1440w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-1400.jpg 1400w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-1280.jpg 1280w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-1200.jpg 1200w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-960.jpg 960w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-720.jpg 720w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-600.jpg 600w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-480.jpg 480w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-360.jpg 360w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-300.jpg 300w,
    https://nakigoe.org/_SRC/nakigoe-academy-night-240.jpg 240w"
  sizes="100vw" 
  alt="Nakigoe Academy">
  <img src="https://nakigoe.org/_IMG/logo-hot-bevel.png" 
    srcset="https://nakigoe.org/_IMG/logo-hot-bevel.jpg 4800w,
      https://nakigoe.org/_SRC/logo-hot-bevel-3840.jpg 3840w,
      https://nakigoe.org/_SRC/logo-hot-bevel-2560.jpg 2560w,
      https://nakigoe.org/_SRC/logo-hot-bevel-2400.jpg 2400w,
      https://nakigoe.org/_SRC/logo-hot-bevel-2048.jpg 2048w,
      https://nakigoe.org/_SRC/logo-hot-bevel-1920.jpg 1920w,
      https://nakigoe.org/_SRC/logo-hot-bevel-1600.jpg 1600w,
      https://nakigoe.org/_SRC/logo-hot-bevel-1440.jpg 1440w,
      https://nakigoe.org/_SRC/logo-hot-bevel-1280.jpg 1280w,
      https://nakigoe.org/_SRC/logo-hot-bevel-1200.jpg 1200w,
      https://nakigoe.org/_SRC/logo-hot-bevel-1080.jpg 1080w,
      https://nakigoe.org/_SRC/logo-hot-bevel-960.jpg 960w,
      https://nakigoe.org/_SRC/logo-hot-bevel-720.jpg 720w,
      https://nakigoe.org/_SRC/logo-hot-bevel-600.jpg 600w,
      https://nakigoe.org/_SRC/logo-hot-bevel-480.jpg 480w,
      https://nakigoe.org/_SRC/logo-hot-bevel-300.jpg 300w"
    sizes="100vw" 
    alt="NAKIGOE.ORG">
</a></h1>

<p style="margin: 0 auto" align="center">© NAKIGOE.ORG</p>

<p style="margin: 0 auto" align="center">All rights reserved and no permissions are granted.</p>

<p style="margin: 0 auto" align="center">Please add stars to the repositories!</p>