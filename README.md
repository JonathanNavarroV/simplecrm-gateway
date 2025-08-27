# SimpleCRM Gateway

**SimpleCRM Gateway** es el **punto de entrada** del ecosistema **SimpleCRM**, encargado de centralizar el acceso a los microservicios internos.  
Construido con **.NET 9** y **YARP (Yet Another Reverse Proxy)**, este proyecto proporciona autenticación, autorización y enrutamiento de solicitudes hacia los distintos servicios del sistema.

---

## 🚀 Funcionalidades principales

- Validación de **tokens JWT** emitidos por **Microsoft Entra ID** (Azure AD).
- Enrutamiento y balanceo de tráfico a los microservicios internos:
  - `simplecrm-crm-service` (gestión de clientes, contactos, etc.)
  - `simplecrm-auth-service` (servicio de autenticación y emisión de tokens)
- Transformaciones de encabezados HTTP (`X-Forwarded-*`) para auditoría.
- Integración opcional con **Redis** para cacheo de tokens o datos temporales.
- Endpoint de salud y prueba (`/_me`).

---

## 📂 Estructura del proyecto

```text
simplecrm-gateway/
├─ ApiGateway/            # Código fuente del Gateway (ASP.NET Core)
│  ├─ Program.cs
│  ├─ appsettings.json
│  └─ ...
├─ Dockerfile             # Imagen de Docker para despliegue
└─ README.md
```

---

## ⚙️ Requisitos previos

- .NET SDK 9.0
- Redis
  (opcional, solo si se desea usar cache)
- Cuenta y App registrada en Microsoft Entra ID para validar tokens.

---

## ▶️ Ejecución en desarrollo

### 1. Clona este repositorio

```bash
git clone git@github.com:JonathanNavarroV/simplecrm-gateway.git
cd simplecrm-gateway/src
```

### 2. Configura los valores de autenticación en `appsettings.json` o usando `dotnet user-secrets`:

```json
"Authentication": {
  "EntraId": {
    "Authority": "https://login.microsoftonline.com/<tenant-id>/v2.0",
    "ValidAudiences": [ "api://simplecrm-gateway" ]
  }
}
```

### 3. Ejecuta el proyecto:

```bash
dotnet run
```

### 4. Prueba el endpoint de salud:

```
curl http://localhost:5000/_me -H "Authorization: Bearer <token>"
```

---

## 🐳 Ejecución con Docker

### Construir y ejecutar la imagen:

```bash
docker build -t simplecrm-gateway .
docker run -p 5000:8080 simplecrm-gateway
```

---

## 🔗 Repositorios relacionados

- [simplecrm-frontend](https://github.com/JonathanNavarroV/simplecrm-frontend)
- [simplecrm-auth-service](https://github.com/JonathanNavarroV/simplecrm-auth-service)
- [simplecrm-crm-service](https://github.com/JonathanNavarroV/simplecrm-crm-service)

---

## ✨ Autor

[Jonathan Navarro](https://github.com/JonathanNavarroV)
