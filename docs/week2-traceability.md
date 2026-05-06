# Week 2 Security Traceability (C1→C5)

## Gate Status

| Gate | Requirement | Evidence Command | Evidence File | Status |
|---|---|---|---|---|
| C1 | AES-256 roundtrip + invalid/missing key | `dotnet test "Cerberus-Project.sln" --filter "FullyQualifiedName~Check1_"` | `docs/logs/check1.log` | ✅ PASSED |
| C2 | Key version tag + rotation | `dotnet test "Cerberus-Project.sln" --filter "FullyQualifiedName~Check2_"` | `docs/logs/check2.log` | ✅ PASSED |
| C3 | File encrypt/decrypt roundtrip | `dotnet test "Cerberus-Project.sln" --filter "FullyQualifiedName~Check3_"` | `docs/logs/check3.log` | ✅ PASSED |
| C4 | SHA-256 integrity match + tamper fail | `dotnet test "Cerberus-Project.sln" --filter "FullyQualifiedName~Check4_"` | `docs/logs/check4.log` | ✅ PASSED |
| C5 | Stress matrix with 100% correctness | `dotnet test "Cerberus-Project.sln" --filter "FullyQualifiedName~Check5_"` | `docs/logs/check5.log`, `docs/week2-check5-stress-report.md` | ✅ PASSED |

## Implemented Artifacts

- `Security/Contracts/*`: contracts for AES, key provider, file crypto, integrity, orchestrator.
- `Security/Services/*`: AES CBC service, config key provider, file crypto flow, SHA-256 service, check orchestrator.
- `Security/Models/*`: `AesPayload`, `FileRoundtripResult`, `StressRunResult`.
- `Security/Exceptions/Check1KeyException.cs`: explicit key errors.
- `Controllers/SecurityController.cs`: thin demo endpoint.
- `tests/Cerberus.Security.Tests/*`: filterable `Check1_` ... `Check5_` test evidence.

## Professor Demo Checklist

1. Show DI registrations in `Program.cs`.
2. Show C1/C2 core in `ConfigurationCheck1KeyProvider` + `AesCbcRoundtripService`.
3. Show C3/C4 flow in `FileCryptoService`, `SecurityCheckOrchestrator`, `Sha256IntegrityHashService`.
4. Run gates in order and open corresponding logs in `docs/logs/`.
5. Open stress table: `docs/week2-check5-stress-report.md` (generated once C5 runs on .NET 8 runtime).
