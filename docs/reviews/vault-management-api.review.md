# Vault Management API Review

## Scope
- Artifact reviewed: `vault-management-api.json`
- Review method: manual inspection + `npx -y @redocly/cli@latest lint --max-problems 500 vault-management-api.json`
- Lint summary: 53 errors, 83 warnings (`security-defined`: 48, `operation-operationId`: 48, `no-invalid-media-type-examples`: 34)
- Spec compliance caveat: no implementation plan file was present in this repo, so compliance was evaluated against contract consistency and OpenAPI quality signals.

## Findings

### CRITICAL
1. Broken endpoint path template blocks correct routing/codegen for device approval
- File: `vault-management-api.json:2386`
- Issue: Path is declared as `/device-approval/{organizationId}/approve/{request-id}}` (extra closing brace).
- Why it matters: Generated clients and route matchers will target a malformed path; this can produce 404s or broken SDK methods for approve-by-request.
- Required fix: Change to `/device-approval/{organizationId}/approve/{request-id}` and re-run contract linting.

### MAJOR
1. `field.type` schema is internally contradictory (`string` type with integer enum values)
- File: `vault-management-api.json:3065-3072`
- Issue: `type` is declared as `"type": "string"` but enum values are `0..3` integers.
- Why it matters: Clients generated from this schema will apply wrong typing, causing request validation failures and runtime coercion bugs.
- Required fix: Use `"type": "integer"` (or convert enum values to strings) and align all examples.

2. Numerous examples do not conform to declared schemas (nullability/type drift)
- Files: `vault-management-api.json:30`, `vault-management-api.json:129-132`, `vault-management-api.json:161-162`, `vault-management-api.json:3234-3236`, `vault-management-api.json:2870-2899`
- Issue: Example payloads repeatedly use `null` or wrong shapes where schemas declare non-null strings/arrays.
- Why it matters: Examples are executable guidance; invalid examples lead consumers to send invalid requests and erode trust in docs.
- Required fix: Either (a) mark fields nullable / broaden schema intentionally, or (b) correct examples to match strict schema.

3. `/unlock` request body schema is underspecified (missing object type and required password)
- File: `vault-management-api.json:55-66`
- Issue: Schema contains `properties` but omits `"type": "object"` and `required: ["password"]`.
- Why it matters: Tooling can accept invalid non-object payloads or omit password without schema-level rejection.
- Required fix: Add `"type": "object"`, `required`, and `additionalProperties` policy as appropriate.

4. Authentication/security requirements are not explicitly declared in the contract
- Files: `vault-management-api.json:2-8`, `vault-management-api.json:10-14` (pattern repeated across operations)
- Issue: No root-level or operation-level `security` requirement is declared; lint reports 48 `security-defined` errors.
- Why it matters: Consumers cannot reliably understand auth expectations; security scanners and SDKs treat endpoints as potentially unauthenticated.
- Required fix: Add `components.securitySchemes` and root/operation `security` declarations (or explicit documented exceptions).

5. List endpoint contract is unbounded; no pagination controls on item listing
- File: `vault-management-api.json:547-602`
- Issue: `GET /list/object/items` returns all matching items by default and exposes filters/search only (no `limit`, `offset`, or cursor).
- Why it matters: Large vaults can trigger high latency, payload bloat, and avoidable client memory pressure.
- Required fix: Add pagination parameters and response metadata (e.g., total/next cursor).

6. No reviewable automated contract tests/checks are present in repo
- File(s): repository contents (`vault-management-api.json` only)
- Issue: No CI/test artifact exists to enforce spec integrity (path validity, schema/example parity, security declarations).
- Why it matters: Regressions like the malformed path and schema/example drift will recur unnoticed.
- Required fix: Add a CI lint/validation step and fail builds on schema-breaking drift.

### MINOR
1. Query parameter semantics and naming are inconsistent
- File: `vault-management-api.json:569`, `vault-management-api.json:587-592`
- Issue: `folderid` casing differs from surrounding params; `trash` is typed boolean but described as non-boolean behavior.
- Why it matters: Creates avoidable client confusion and inconsistent SDK surface.
- Suggested fix: Normalize naming (`folderId`) and align behavioral description with formal schema.

2. Missing `operationId` across operations reduces SDK quality
- File example: `vault-management-api.json:10-11` (48 occurrences)
- Issue: Operations do not define stable operation IDs.
- Why it matters: Generated client method names become unstable/opaque and harder to maintain.
- Suggested fix: Add deterministic `operationId` for each operation.

## Assessment
- Ready to merge: **No**
- Reasoning: At least one contract-breaking defect is present (malformed path), plus major schema and security-specification issues that will mislead integrators and generate unreliable client code. These should be fixed before merge.
