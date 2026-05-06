# Skill Registry

**Delegator use only.** Any agent that launches sub-agents reads this registry to resolve compact rules, then injects them directly into sub-agent prompts. Sub-agents do NOT read this registry or individual SKILL.md files.

See `_shared/skill-resolver.md` for the full resolution protocol.

## User Skills

| Trigger | Skill | Path |
|---------|-------|------|
| When creating a pull request, opening a PR, or preparing changes for review. | branch-pr | /home/omar/.claude/skills/branch-pr/SKILL.md |
| When creating a GitHub issue, reporting a bug, or requesting a feature. | issue-creation | /home/omar/.claude/skills/issue-creation/SKILL.md |
| When writing Go tests, using teatest, or adding test coverage. | go-testing | /home/omar/.claude/skills/go-testing/SKILL.md |
| When user says "judgment day", "judgment-day", "review adversarial", "dual review", "doble review", "juzgar", "que lo juzguen". | judgment-day | /home/omar/.claude/skills/judgment-day/SKILL.md |
| When user asks to create a new skill, add agent instructions, or document patterns for AI. | skill-creator | /home/omar/.claude/skills/skill-creator/SKILL.md |

## Compact Rules

Pre-digested rules per skill. Delegators copy matching blocks into sub-agent prompts as `## Project Standards (auto-resolved)`.

### branch-pr
- Cada PR DEBE linkear un issue aprobado (`status:approved`).
- Cada PR DEBE tener exactamente una etiqueta `type:*`.
- Respetar Conventional Commits (`type(scope): description` o `type: description`).
- Nombrar ramas con `type/description` y regex permitida.
- Correr validaciones (shellcheck/checks requeridos) antes de merge.
- Usar template de PR con resumen, tabla de cambios y test plan.

### issue-creation
- No crear issues vacíos: usar template de bug o feature request.
- Buscar duplicados antes de abrir un issue.
- Todo issue nuevo entra con `status:needs-review`; PR recién con `status:approved`.
- Preguntas van a Discussions, no a Issues.
- Completar todos los campos requeridos del template.
- Mantener título consistente con convención (`fix(...)`, `feat(...)`, etc.).

### go-testing
- Preferir tests table-driven para funciones y casos múltiples.
- Para Bubbletea: testear transiciones de estado vía `Update()`.
- Para flujos TUI completos: usar `teatest.NewTestModel()`.
- Para salida visual: golden files en `testdata/`.
- Mockear dependencias/sistema para aislar efectos secundarios.
- Usar comandos estándar `go test`, `-cover`, `-short` según contexto.

### judgment-day
- Lanzar DOS jueces en paralelo y ciegos entre sí (misma consigna).
- El orquestador sintetiza: Confirmed, Suspect A/B, Contradictions.
- Clasificar warnings en `real` vs `theoretical`; theoretical se reporta como INFO.
- Si hay issues confirmados, delegar Fix Agent y luego re-juzgar.
- No declarar APPROVED hasta cumplir criterios de convergencia.
- En rondas avanzadas, re-juzgar solo si persisten CRITICALs confirmados.

### skill-creator
- Crear skill solo para patrones reutilizables (no one-off).
- Seguir estructura `skills/{skill-name}/SKILL.md` con frontmatter completo.
- Incluir Trigger claro en `description`.
- Priorizar reglas críticas accionables y ejemplos mínimos.
- Evitar duplicar docs extensas; referenciar recursos locales.
- Registrar skill en AGENTS.md cuando aplique.

## Project Conventions

| File | Path | Notes |
|------|------|-------|
| — | — | No se detectaron archivos de convención a nivel proyecto (`AGENTS.md`, `agents.md`, `CLAUDE.md`, `.cursorrules`, `GEMINI.md`, `copilot-instructions.md`). |

Read the convention files listed above for project-specific patterns and rules. All referenced paths have been extracted — no need to read index files to discover more.
