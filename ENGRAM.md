# ENGRAM — Contexto operativo de Cerberus

## Proyecto
- **Nombre:** Cerberus
- **Objetivo:** Plataforma web de almacenamiento seguro con **file sharding**.
- **Estrategia de seguridad:** cifrar archivo, fragmentarlo y distribuirlo entre **AWS S3** y **Azure Blob Storage**.
- **Stack base:** ASP.NET Core MVC (.NET 8), C#, EF Core Code-First, SQLite.

## Restricciones y criterios técnicos
- **Confidencialidad:** AES-256.
- **Integridad:** SHA-256.
- **Disponibilidad/Redundancia:** distribución multi-cloud (AWS + Azure).

## Estado actual confirmado (Semana 1)
- Modelos creados: `Usuario`, `ArchivoOriginal`, `Fragmento`.
- Contexto EF creado: `Data/ApplicationDbContext`.
- Registro de SQLite en `Program.cs` con `Data Source=cerberus.db`.
- Dependencias EF Core/SQLite agregadas en `ProyectoInnovador.csproj`.

## Cronograma oficial (resumen)
- **S1 (15h):** setup repo, MVC, dependencias, DB local, modelos.
- **S2 (15h):** AES-256, gestión de llaves, cifrado/descifrado, SHA-256, pruebas binarias.
- **S3 (0h):** receso.
- **S4 (20h):** AWS S3 + IAM + subida asíncrona + ETag.
- **S5 (20h):** Azure Blob + SAS + sincronización + redundancia.
- **S6 (20h):** algoritmo de fragmentación + distribución + metadatos + paralelo.
- **S7 (15h):** negocio/perfiles + autenticación + suscripciones.
- **S8 (15h):** QA + heurísticas + refactor + E2E + documentación.
- **S9 (5h):** presentación final y cierre.
- **Total:** 125 horas-hombre.

## Enfoque SDD recomendado (macro)
1. Definir escenarios semanales (Given/When/Then).
2. Implementar caso de uso mínimo por semana.
3. Validar con pruebas de comportamiento antes de expandir alcance.
4. Mantener controllers delgados y lógica en servicios/casos de uso.
