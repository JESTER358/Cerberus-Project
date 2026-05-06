# 🛡️ Cerberus Project - Almacenamiento Seguro Multi-Cloud

Este proyecto implementa una arquitectura de software para el almacenamiento, fragmentación, encriptación y recuperación segura de archivos distribuidos a través de múltiples nubes (AWS S3 y Azure Blob Storage).

## 📌 Arquitectura y Características
- **Criptografía Simétrica**: Encriptación y desencriptación de archivos usando **AES-256** con llave maestra.
- **Fragmentación Multi-Cloud**: División de binarios (50/50) y almacenamiento distribuido en MinIO (simulador AWS S3) y Azurite (simulador Azure).
- **Integridad de Datos**: Verificación criptográfica mediante Hashes **SHA-256** guardados en base de datos.
- **Concurrencia**: Descarga paralela asíncrona usando `Task.WhenAll`.
- **Persistencia**: Base de datos SQLite gestionada a través de **Entity Framework Core**.

---

## ⚙️ Requisitos Previos

Para ejecutar este proyecto desde cero en cualquier computadora, necesitas instalar lo siguiente:

1. **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (Para compilar y correr el backend).
2. **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** (Para correr los simuladores de la nube).
3. **[Git](https://git-scm.com/downloads)** (Para clonar el repositorio).

---

## 🚀 Instalación y Configuración Paso a Paso

### 1. Clonar el Repositorio
Abre tu terminal y clona el código fuente:
```bash
git clone https://github.com/JESTER358/Cerberus-Project.git
cd Cerberus-Project
```

### 2. Levantar la Infraestructura Cloud (Docker)
Inicia los simuladores de MinIO y Azurite en segundo plano:
```bash
docker-compose up -d
```
*(Espera unos segundos a que los contenedores estén completamente activos).*

### 3. Configurar la Base de Datos (Entity Framework)
Instala las herramientas de EF Core (si no las tienes) y crea la base de datos local a partir de las migraciones:
```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"  # (En Windows no es necesario este export)
dotnet ef database update
```

### 4. Ejecutar la Aplicación
Inicia el servidor backend en entorno de desarrollo:
```bash
dotnet run --project ProyectoInnovador.csproj --environment Development
```
El servidor debería comenzar a escuchar en `http://localhost:5232` (verifica tu puerto en la consola).

---

## 🧪 Guía de Uso: Demo de Punta a Punta

Una vez que el servidor esté corriendo, abre **otra pestaña en tu terminal** para probar el flujo completo de seguridad.

### FASE 1: Subida Segura (Upload)
Envía un archivo al sistema. El backend lo encriptará con AES-256, lo partirá en dos, lo subirá a AWS y Azure, y guardará los hashes en SQLite.

```bash
# Reemplaza la ruta del archivo por la de un archivo real en tu PC
curl -v -X POST "http://localhost:5232/security/upload-multicloud" \
     -H "Content-Type: multipart/form-data" \
     -F "archivo=@ruta/a/tu/archivo.png"
```
**Importante:** La consola te devolverá una respuesta JSON. Anota el número que aparece en `"archivoId"` (ej. `"archivoId": 1`).

### FASE 2: Recuperación y Verificación
Descarga el archivo usando el ID obtenido en el paso anterior. El sistema descargará ambas mitades en paralelo, las reensamblará, las desencriptará y verificará su integridad (SHA-256).

```bash
# Reemplaza el '1' por el ID de tu archivo
curl -v -X GET "http://localhost:5232/files/download/1" -o archivo_recuperado.png
```
**Resultado:** Revisa la carpeta desde donde ejecutaste el comando. Tendrás un archivo llamado `archivo_recuperado.png` en perfectas condiciones, demostrando que el ciclo de encriptación y reensamblaje fue exitoso.

---
*Desarrollado para la materia de Desarrollo de Apps Innovadoras.*