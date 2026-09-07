# DM Admin API

API REST + SignalR para la aplicación [DM Admin](../DM-admin-app). Gestiona autenticación, campañas, construcción de mundos y colaboración en tiempo real para directores de juego de rol de mesa.

## Stack tecnológico

| Capa | Tecnología |
|------|-----------|
| Framework | ASP.NET Core 9 Web API |
| Base de datos | PostgreSQL 16 + EF Core 9 (Npgsql) |
| Autenticación | JWT Bearer (access token 15 min + refresh token 7 días) |
| Tiempo real | ASP.NET Core SignalR |
| Logging | Serilog (consola) |
| Documentación | Scalar (OpenAPI) — disponible en dev |
| Contraseñas | BCrypt.Net |
| Pagos | Stripe.net |

## Estructura del proyecto

```
src/DmAdminApi/
├── Common/
│   ├── Controllers/        # ApiControllerBase (CurrentUserId helper)
│   ├── Extensions/         # Registro de servicios en Program.cs
│   └── Middleware/         # FeatureGatingMiddleware (desactivado por ahora)
├── Features/
│   ├── Auth/               # Registro, login, refresh, logout, /me
│   ├── Campaigns/          # CRUD campañas, miembros, invitaciones, join por código
│   ├── Hubs/               # CampaignHub (SignalR), PresenceTracker, HubEvents
│   ├── Permissions/        # PermissionService (lógica de acceso centralizada)
│   ├── Subscriptions/      # Checkout y portal de Stripe
│   └── World/
│       ├── EntityType*     # Tipos de entidad y campos personalizados
│       ├── WorldEntity*    # Entidades del mundo con campos custom y permisos
│       ├── Relationship*   # Tipos de relación y relaciones entre entidades
│       └── Export*         # Exportación de campaña en JSON
└── Infrastructure/
    ├── Auth/               # JwtService, JwtSettings
    ├── Data/               # AppDbContext, entidades EF Core, migraciones
    ├── Email/              # SmtpEmailService
    └── Stripe/             # StripeSettings
```

## Requisitos previos

- .NET SDK 9
- PostgreSQL 16 (o Docker)

## Inicio rápido con Docker

La forma más sencilla de levantar el entorno completo (API + base de datos):

```bash
docker-compose up --build
```

La API quedará disponible en `http://localhost:5000`.
Scalar UI (documentación interactiva) en `http://localhost:5000/scalar/v1`.

## Desarrollo local sin Docker

1. Instala y levanta PostgreSQL 16 localmente.

2. Crea el archivo `src/DmAdminApi/appsettings.Local.json` (gitignoreado) con tus secretos:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dm_admin_dev;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "clave-secreta-de-al-menos-32-caracteres-aqui"
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "ProPriceId": "price_...",
    "MasterPriceId": "price_..."
  },
  "Email": {
    "Host": "smtp.example.com",
    "Port": 587,
    "Username": "user",
    "Password": "pass",
    "FromAddress": "noreply@example.com"
  }
}
```

3. Ejecuta la API (las migraciones se aplican automáticamente al arrancar):

```bash
dotnet run --project src/DmAdminApi
```

## Configuración (`appsettings.json`)

| Clave | Descripción |
|-------|-------------|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a PostgreSQL |
| `Jwt:Key` | Clave secreta para firmar tokens (mín. 32 caracteres) |
| `Jwt:ExpiryMinutes` | Duración del access token (default: 15) |
| `Jwt:RefreshTokenExpiryDays` | Duración del refresh token (default: 7) |
| `Cors:AllowedOrigins` | Orígenes permitidos (e.g. `http://localhost:4200`) |
| `Stripe:SecretKey` | Clave secreta de Stripe |
| `Stripe:WebhookSecret` | Secreto del webhook de Stripe |
| `Email:Host` / `Port` / `Username` / `Password` | Configuración SMTP |

## Endpoints principales

### Auth — `/api/auth`
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/register` | Crear cuenta |
| POST | `/login` | Iniciar sesión → devuelve access + refresh token |
| POST | `/refresh` | Renovar access token con refresh token |
| POST | `/logout` | Invalidar refresh tokens del usuario |
| GET  | `/me` | Datos del usuario autenticado |

### Campañas — `/api/campaigns`
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/` | Listar campañas del usuario |
| POST | `/` | Crear campaña |
| GET | `/{id}` | Detalle con miembros y roles |
| PUT | `/{id}` | Actualizar campaña (solo DM) |
| DELETE | `/{id}` | Eliminar campaña (solo DM) |
| GET | `/preview/{code}` | Vista previa pública por código de invitación |
| POST | `/join` | Unirse mediante token de invitación |
| POST | `/join-by-code` | Unirse mediante código directo |
| POST | `/{id}/invitations` | Generar enlace de invitación (solo DM) |
| POST | `/{id}/regenerate-code` | Regenerar código de unión (solo DM) |
| PUT | `/{id}/members/{memberId}/role` | Cambiar rol de miembro |
| DELETE | `/{id}/members/{memberId}` | Expulsar miembro |
| POST | `/{id}/leave` | Abandonar campaña |

### Mundo — `/api/campaigns/{campaignId}`

**Entidades**
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/entities` | Listar entidades visibles para el usuario |
| POST | `/entities` | Crear entidad |
| GET | `/entities/{entityId}` | Detalle de entidad |
| PUT | `/entities/{entityId}` | Editar entidad (con detección de conflictos) |
| DELETE | `/entities/{entityId}` | Eliminar entidad (DM o co-DM) |
| GET | `/entities/{entityId}/permissions` | Ver permisos por rol |
| PUT | `/entities/{entityId}/permissions` | Establecer permisos por rol |

**Tipos de entidad**
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/entity-types` | Listar tipos con sus campos |
| POST | `/entity-types` | Crear tipo personalizado (DM) |
| PUT | `/entity-types/{typeId}` | Editar tipo (DM) |
| DELETE | `/entity-types/{typeId}` | Eliminar tipo (DM) |
| POST | `/entity-types/{typeId}/fields` | Agregar campo |
| PUT | `/entity-types/{typeId}/fields/{fieldId}` | Editar campo |
| DELETE | `/entity-types/{typeId}/fields/{fieldId}` | Eliminar campo |

**Relaciones y grafo**
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/relationship-types` | Listar tipos de relación |
| POST | `/relationship-types` | Crear tipo de relación |
| PUT | `/relationship-types/{typeId}` | Editar tipo |
| DELETE | `/relationship-types/{typeId}` | Eliminar tipo |
| GET | `/entities/{entityId}/relationships` | Relaciones de una entidad |
| POST | `/entities/{entityId}/relationships` | Crear relación |
| DELETE | `/entities/{entityId}/relationships/{id}` | Eliminar relación |
| GET | `/graph` | Grafo completo (nodos + aristas) |
| GET | `/entities/search?q=` | Búsqueda de entidades |

### Export — `/api/campaigns/{campaignId}/export`
Exporta la campaña completa en JSON.

### Suscripciones — `/api/subscriptions`
Integración con Stripe Checkout y Customer Portal.

## SignalR Hub

**URL:** `/hubs/campaign`
Requiere JWT en la conexión.

| Evento (servidor → cliente) | Descripción |
|-----------------------------|-------------|
| `EntityCreated` | Nueva entidad creada en la campaña |
| `EntityUpdated` | Entidad actualizada |
| `EntityDeleted` | Entidad eliminada (envía el `entityId`) |
| `PermissionsChanged` | Permisos de una entidad modificados |
| `PresenceUpdated` | Lista actualizada de usuarios conectados |

| Método (cliente → servidor) | Descripción |
|-----------------------------|-------------|
| `JoinCampaign(campaignId)` | Unirse al grupo de la campaña |
| `LeaveCampaign(campaignId)` | Salir del grupo |

## Roles del sistema

| Rol | Permisos |
|-----|----------|
| `DM` | Propietario — acceso total |
| `Co-DM` | Crear/editar/eliminar entidades y relaciones |
| `Player` | Ver entidades con permiso de visibilidad |
| `Spectator` | Solo lectura (sin crear entidades) |

## CI/CD

GitHub Actions ejecuta `dotnet build` en cada push/PR a `main`.
Ver `.github/workflows/ci.yml`.
