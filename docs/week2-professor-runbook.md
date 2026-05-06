# Week 2 Professor Runbook (C1→C5)

## 1) Environment Setup

```bash
export Security__Check1KeyBase64="AQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQE="
export Security__Check1KeyVersion="v1"
```

## 2) Gate Execution Order (No Skip)

```bash
mkdir -p docs/logs
dotnet test --filter "FullyQualifiedName~Check1_" | tee docs/logs/check1.log
dotnet test --filter "FullyQualifiedName~Check2_" | tee docs/logs/check2.log
dotnet test --filter "FullyQualifiedName~Check3_" | tee docs/logs/check3.log
dotnet test --filter "FullyQualifiedName~Check4_" | tee docs/logs/check4.log
dotnet test --filter "FullyQualifiedName~Check5_" | tee docs/logs/check5.log
```

## 3) Final Evidence Screen

Open:
- `docs/week2-traceability.md`
- `docs/week2-check5-stress-report.md`
- `docs/logs/check1.log` ... `docs/logs/check5.log`

## 4) Stabilization Checklist

- [ ] C1→C5 logs generated in same run window.
- [ ] `docs/week2-check5-stress-report.md` exists and shows only PASS.
- [ ] No ad-hoc code edits after log generation.
