# Repository Guidelines

## Proyecto y objetivo

**Smart Energy API** es una ASP.NET Core Web API en .NET 10 y C#. Usa Controllers, Entity Framework Core, PostgreSQL mediante Npgsql y JWT para autenticación. La plataforma registra usuarios, hogares, dispositivos ESP32 y mediciones de consumo eléctrico. La aplicación cliente se comunicará con dispositivos ESP32 y enviará sus mediciones al backend.

## Modelo de dominio

El modelo principal es `User <-> HomeMember <-> Home -> Space -> Device -> EnergyReading`; además, `Home` mantiene un historial de `EnergyTariff`. Las entidades de dominio son `User`, `Home`, `HomeMember`, `Space`, `Device`, `EnergyReading` y `EnergyTariff`. Sus identificadores son `Guid`, salvo `EnergyReading`, que utiliza `long`.

- Un usuario puede pertenecer a varios hogares y un hogar tener varios usuarios. Modelar esta relación mediante `HomeMember`, permitiendo roles o permisos.
- Un hogar contiene múltiples espacios y cada `Space` pertenece a un único `Home`.
- Cada `Device` pertenece a un `Space`, nunca directamente a un hogar ni a un usuario. El hogar se determina mediante `Device -> Space -> Home`.
- Cada dispositivo físico se identifica mediante un `SerialNumber` globalmente único y permanente.
- Un hogar puede conservar un historial de tarifas eléctricas. El precio se expresa por kWh y permanece en `EnergyTariff`, no directamente en `Home`.
- Cada `EnergyReading` pertenece al dispositivo que la produjo.
- No almacenar por ahora costos calculados ni tarifas en `EnergyReading`; el consumo y el costo se calcularán posteriormente a partir de diferencias de `EnergyTotalKwh` y la tarifa aplicable.
- Resolver acceso a dispositivos y mediciones a través de la membresía del usuario en el hogar.
- Nunca usar únicamente IDs enviados por el cliente como prueba de autorización.

Inicialmente, `EnergyReading` puede almacenar `Voltage`, `Current`, `Power`, `EnergyTotalKwh` y `RecordedAt`. Usar `DateTimeOffset` en UTC para fechas importantes y no añadir campos especulativos.

- `Voltage` representa volts.
- `Current` representa amperes.
- `Power` representa watts instantáneos.
- `EnergyTotalKwh` representa el total acumulado de energía registrado por el dispositivo, expresado en kWh.
- El contador `EnergyTotalKwh` debe ser conceptualmente monotónicamente creciente durante el funcionamiento normal.
- El firmware debe intentar preservar el contador acumulado entre reinicios.
- Los cálculos futuros de consumo utilizarán diferencias entre lecturas acumuladas.
- No implementar todavía analytics ni cálculos de costos.

Para las consultas de `Consumption`:

- `EnergyTotalKwh` es acumulativo y el consumo se calcula mediante deltas entre lecturas consecutivas de cada dispositivo.
- Utilizar como baseline la lectura inmediatamente anterior al inicio del rango cuando exista.
- Si el contador disminuye, interpretar inicialmente que ocurrió un reset y utilizar `delta = current.EnergyTotalKwh`.
- Nunca permitir deltas negativos ni persistir los deltas calculados.
- El costo es calculado y no se persiste.
- Cada delta utiliza la `EnergyTariff` aplicable según `current.RecordedAt`.
- Si una tarifa cambia entre dos lecturas, asignar el delta completo a la tarifa vigente en la lectura actual; esta es una aproximación y no implica interpolación.
- `Power` es instantáneo y nunca debe sumarse a través del tiempo.
- La potencia de un Home o Space se obtiene sumando la última potencia relevante de cada Device activo.

La futura creación automática de un `Device` por `SerialNumber` solo podrá ocurrir dentro de una operación autenticada y autorizada. Antes de crearlo se deberá validar el acceso del usuario al hogar y al espacio; si ya existe, se deberá comprobar que pertenece al espacio y hogar correctos. Nunca crear dispositivos desde peticiones anónimas, permitir apropiación por conocer el número de serie ni reasignar un número de serie perteneciente a otro hogar o espacio incompatible. La seguridad de pairing/claim se diseñará posteriormente. Si todavía no existe un nombre amigable, se podrá usar temporalmente `SerialNumber` como nombre.

## Organización feature-first

Agrupar cada funcionalidad en `Features/<FeatureName>/`; no crear carpetas globales `Controllers/`, `Services/` o `DTOs/`.

```text
Features/
  Auth/              AuthController.cs, AuthService.cs, Dtos/
  Users/             UsersController.cs, UserService.cs, Dtos/
  Homes/             HomesController.cs, HomeService.cs, Dtos/
  Devices/           DevicesController.cs, DeviceService.cs, Dtos/
  EnergyReadings/    EnergyReadingsController.cs, EnergyReadingService.cs, Dtos/
Domain/Entities/     User.cs, Home.cs, HomeMember.cs, Space.cs, Device.cs, EnergyReading.cs, EnergyTariff.cs
Infrastructure/
  Persistence/       AppDbContext.cs, Configurations/
  Authentication/
Common/              Exceptions/, Middleware/, Extensions/
Migrations/
Program.cs
appsettings.json
SmartEnergy.Api.csproj
```

Cada feature puede incluir controller, service, DTOs, interfaces específicas, validators, mappers y componentes exclusivos. Mantener los cambios localizados en su feature.

## Responsabilidades por capa

Los Controllers reciben solicitudes HTTP, realizan validación básica, obtienen la identidad autenticada, llaman al Service y producen respuestas HTTP. No deben contener lógica de negocio compleja.

Los Services contienen validaciones de negocio, autorización sobre recursos, coordinación con EF Core, creación o modificación de entidades y transformación a DTOs. Usar directamente `AppDbContext` desde Services cuando sea apropiado.

`Domain/Entities` contiene las entidades centrales y no depende de Controllers, DTOs ni infraestructura HTTP. `Infrastructure` concentra EF Core, PostgreSQL/Npgsql, `AppDbContext`, configuraciones `IEntityTypeConfiguration<T>`, JWT y otros detalles técnicos externos al dominio.

`Common` se reserva para comportamiento transversal reutilizado por múltiples features, como middleware, excepciones globales y extensiones. No usarlo como destino para código sin ubicación clara. Mantener las migraciones de EF Core en `Migrations/`.

## Reglas de desarrollo

- Usar nombres en inglés para clases, propiedades, métodos, variables y archivos.
- Aplicar cuatro espacios, PascalCase para tipos y miembros públicos, camelCase para variables y parámetros, y namespaces con ámbito de archivo.
- Mantener nullable reference types habilitado.
- Usar inyección de dependencias y `async`/`await` para I/O; nombrar métodos asincrónicos con el sufijo `Async` cuando corresponda.
- Usar DTOs como contratos de entrada y salida. No exponer entidades de EF Core desde endpoints.
- Mantener el código simple, legible y sin abstracciones innecesarias.
- No crear una interfaz para cada clase automáticamente; hacerlo solo cuando aporte una ventaja concreta.
- No introducir CQRS, MediatR, Repository Pattern ni patrones adicionales sin una necesidad concreta y autorización previa.

## Entity Framework Core

Usar EF Core con PostgreSQL mediante Npgsql. Mantener `AppDbContext` en `Infrastructure/Persistence` y preferir `IEntityTypeConfiguration<T>` para configuraciones importantes. Definir explícitamente las relaciones relevantes y modificar el esquema mediante migraciones. No ejecutar migraciones destructivas ni cambios destructivos sobre la base de datos sin consultar primero.

## Seguridad y autenticación

Usar JWT para autenticar usuarios y proteger todos los endpoints privados. Las contraseñas deben almacenarse con hashing seguro, nunca en texto plano. No incluir secretos JWT, contraseñas ni connection strings sensibles en el código o archivos versionados; obtenerlos de variables de entorno, user secrets u otra configuración segura.

En cada operación privada, comprobar que el usuario autenticado pertenece al hogar solicitado. Para espacios, dispositivos y mediciones, validar además la ruta de pertenencia `Device -> Space -> Home` y que el hogar sea accesible para ese usuario.

## Desarrollo, compilación y pruebas

- `dotnet restore`: restaura dependencias NuGet existentes.
- `dotnet build`: compila el proyecto; ejecutarlo después de cambios importantes.
- `dotnet run`: inicia la API localmente.
- `dotnet watch run`: inicia la API con hot reload.
- `dotnet test`: ejecuta los proyectos de pruebas disponibles.
- `dotnet format --verify-no-changes`: comprueba el formato sin modificar archivos.

El proyecto debe compilar antes de considerar una tarea terminada. Ejecutar también los tests relacionados cuando existan. No ignorar errores ni warnings nuevos sin explicar su causa. Crear pruebas en `tests/SmartEnergy.Api.Tests/`, nombrando archivos según el componente, por ejemplo `HomesServiceTests.cs`, y métodos por comportamiento esperado.

## Paquetes NuGet

No agregar paquetes automáticamente. Antes de instalar uno: explicar cuál es, para qué se necesita, si puede resolverse con las capacidades actuales de .NET y esperar autorización cuando implique una decisión arquitectónica relevante.

## Cambios arquitectónicos

Consultar antes de introducir CQRS, MediatR, Repository Pattern, Clean Architecture completa, múltiples proyectos, otro sistema de autenticación o base de datos, message brokers, Redis u otra infraestructura. Explicar el problema, beneficios, desventajas y por qué Smart Energy lo necesita. Priorizar una arquitectura limpia, pragmática y mantenible.

## Flujo de trabajo y contribuciones

Antes de una funcionalidad considerable: analizar el código relacionado, explicar brevemente el cambio, implementarlo dentro de su feature, ejecutar `dotnet build` y los tests pertinentes, y reportar archivos modificados junto con decisiones técnicas. No cambiar código ajeno a la tarea sin una razón concreta.

Como no hay historial Git disponible, usar commits breves e imperativos, por ejemplo `Add device registration`. Cada pull request debe describir motivación y comportamiento, enlazar issues, indicar comandos de verificación y destacar cambios de configuración, base de datos o contratos HTTP.

No versionar `bin/`, `obj/`, `.vs/` ni secretos. Mantener valores seguros en `appsettings*.json` y documentar toda nueva configuración requerida.
