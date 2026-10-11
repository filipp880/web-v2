# TramWebApp - Система мониторинга трамвая

Веб-приложение для оценки продольной скорости и положения трамвая без GNSS и IMU, используя только положение ручки контроллера водителя и скорости передней/задней тележек.

## 🚀 Быстрый запуск

### Через Docker Compose (рекомендуется)

```bash
# Клонирование и запуск
git clone https://github.com/filipp880/web-v2.git
cd web-v2/TramWebApp
docker compose up --build -d
```

Приложение будет доступно по адресам:
- **Frontend**: http://localhost
- **Backend API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **pgAdmin**: http://localhost:5050 (admin@tramwebapp.local / admin123)

### Локальная разработка

#### Backend (.NET 8)
```bash
cd src/TramWebApp.Api
dotnet restore
dotnet run
```

#### Frontend (React + Vite)
```bash
cd frontend
npm install
npm run dev
```

## 🏗 Архитектура

```
TramWebApp/
├── src/
│   ├── TramWebApp.Domain/        # Доменные сущности, VO, интерфейсы
│   ├── TramWebApp.Application/   # DTO, UseCases, Validators, Mappings
│   ├── TramWebApp.Infrastructure/# EF Core, Repositories, Model Service, Auth
│   └── TramWebApp.Api/           # Controllers, Middleware, Swagger
├── frontend/                     # React 18 + TypeScript + Vite
├── docker-compose.yml
└── README.md
```

### Стек технологий

**Backend:**
- .NET 8 / ASP.NET Core Web API
- Clean Architecture (Onion)
- Entity Framework Core + PostgreSQL
- JWT Authentication + Role-based Authorization
- Swagger/OpenAPI
- InMemory / PostgreSQL переключаемое хранилище

**Frontend:**
- React 18 + TypeScript + Vite
- React Router v6
- Chart.js (графики)
- Leaflet (карты, опционально)
- CSS Modules / Custom CSS

**DevOps:**
- Docker / Docker Compose
- PostgreSQL 16
- pgAdmin 4

## 🎯 Функциональность

### Основные возможности
1. **Управление прогонами** - CRUD для тестовых прогонов трамвая
2. **Телеметрия** - загрузка и просмотр данных с датчиков (ручка контроллера, скорости тележек, GPS)
3. **Модель ML** - инференс модели оценки скорости/положения в реальном времени
4. **Интерактивная визуализация** - графики скорости/положения, 2D карта трассы
5. **Вмешательство оператора** - корректировка предсказаний модели вручную
6. **История инференсов** - просмотр всех запусков модели с метриками ошибок
7. **Аутентификация/Авторизация** - JWT, роли Admin/User

### API Endpoints

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/api/auth/login` | Вход в систему |
| POST | `/api/auth/register` | Регистрация |
| GET | `/api/auth/me` | Профиль пользователя |
| GET | `/api/tram-runs` | Список прогонов |
| POST | `/api/tram-runs` | Создать прогон |
| GET | `/api/tram-runs/{id}` | Детали прогона |
| PUT | `/api/tram-runs/{id}` | Обновить прогон |
| DELETE | `/api/tram-runs/{id}` | Удалить прогон (Admin) |
| GET | `/api/tram-runs/{id}/telemetry` | Телеметрия прогона |
| POST | `/api/model/predict` | Предсказание модели |
| POST | `/api/model/inference` | Запуск инференса |
| PUT | `/api/model/inferences/{id}/intervene` | Вмешательство пользователя |
| GET | `/api/model/inferences/{runId}` | История инференсов |

## 🔐 Демо-аккаунты

| Роль | Логин | Пароль |
|------|-------|--------|
| Admin | `admin` | `admin123` |
| User | `operator` | `user123` |

## 📊 Тестовые данные

При первом запуске автоматически создаются:
- 3 тестовых прогона (кольцевая линия, радиальный маршрут, городской центр)
- ~750 точек телеметрии на прогон
- Инференсы модели с реалистичными метриками
- Пользователи admin/operator

## 🛠 Разработка

### Структура базы данных

```sql
-- Основные таблицы
tram_runs              -- Прогоны трамвая
tram_telemetry_points  -- Точки телеметрии (ручка, скорости тележек, GPS)
model_inferences       -- Результаты работы модели
users                  -- Пользователи системы
```

### Миграции

```bash
# Создание миграции
dotnet ef migrations add <Name> -p src/TramWebApp.Infrastructure -s src/TramWebApp.Api

# Применение миграций
dotnet ef database update -p src/TramWebApp.Infrastructure -s src/TramWebApp.Api
```

### Переменные окружения

| Переменная | Описание | По умолчанию |
|------------|----------|--------------|
| `StorageMode` | `InMemory` или `PostgreSQL` | `PostgreSQL` |
| `ConnectionStrings__DefaultConnection` | Строка подключения к БД | - |
| `JwtSettings__SecretKey` | Секретный ключ JWT | - |
| `JwtSettings__Issuer` | Издатель токена | `TramWebApp` |
| `JwtSettings__Audience` | Аудитория токена | `TramWebAppUsers` |
| `JwtSettings__ExpiryMinutes` | Время жизни токена | `60` |

## 📝 Лицензия

MIT License - учебный проект для лабораторных работ 2, 3, 4.

## 🤝 Авторы

- **Backend**: Clean Architecture, .NET 8, EF Core
- **Frontend**: React 18, TypeScript, Vite
- **ML Model**: Mock-реализация (готова к замене на ONNX/Python)