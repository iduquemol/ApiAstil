# facturas-listado Specification

## Purpose

Exposes a read endpoint (`GET /api/facturas`) that lists facturas within a date range for the JuntaNacional frontend's facturas grid. It wraps the `usr_sp_itq_consulta_fe` stored procedure - which returns its result as a single JSON-text column rather than a relational rowset, with inconsistent internal key casing and untrimmed/numeric values - and normalizes it into a typed, camelCase JSON contract the frontend can rely on.

## Requirements

### Requirement: List facturas by date range
The system SHALL provide `GET /api/facturas` returning the facturas from the `usr_sp_itq_consulta_fe` stored procedure for a required `fechaIni`/`fechaFin` date range.

#### Scenario: Successful query
- **WHEN** a client sends `GET /api/facturas?fechaIni=2026-01-01&fechaFin=2026-01-31`
- **THEN** the system returns `200 OK` with a JSON array of facturas from `usr_sp_itq_consulta_fe` for that range, each with `marca`, `fecha`, `tipo`, `numero`, `nitCliente`, `nombreCliente`, `valor`, and `estado`

#### Scenario: No facturas in range
- **WHEN** the stored procedure's `venta` result is `NULL` for the given date range (its convention for "no matches")
- **THEN** the system returns `200 OK` with an empty JSON array, not an error

#### Scenario: Missing or unparseable date parameters
- **WHEN** `fechaIni` or `fechaFin` is missing or not a valid date
- **THEN** the system returns `400 Bad Request` without calling the stored procedure

#### Scenario: Inverted date range
- **WHEN** `fechaIni` is later than `fechaFin`
- **THEN** the system returns `400 Bad Request` with a message indicating the range is invalid, without calling the stored procedure

### Requirement: Typed response shape
The system SHALL serialize each factura as a JSON object with camelCase field names, independent of the stored procedure's actual result shape (a single JSON-text column, not a relational rowset) or its inconsistent internal key casing.

#### Scenario: Field mapping and casing
- **WHEN** the system returns a factura in the response array
- **THEN** its JSON fields are exactly `marca` (boolean), `fecha` (date, `yyyy-MM-dd`), `tipo` (string, trimmed of padding), `numero` (number), `nitCliente` (string), `nombreCliente` (string), `valor` (number), and `estado` (number, a raw status code)

#### Scenario: `marca` defaults to unselected
- **WHEN** the system returns a factura
- **THEN** `marca` is always `false`, since the stored procedure does not track a persisted selection state

#### Scenario: `tipo` is trimmed
- **WHEN** the stored procedure's underlying data contains a padded `tipo` value (e.g. `"20 "`)
- **THEN** the system returns `tipo` with trailing whitespace removed (e.g. `"20"`)
