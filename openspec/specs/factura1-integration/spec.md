# factura1-integration Specification

## Purpose

Integrates ApiAstil with Factura1 (electronic invoicing provider) to authenticate, generate a factura's XML from the database, and submit it for DIAN processing. All of this logic lives in `FacturasController` by deliberate choice - it is not extracted into a separate service class. Two diagnostic endpoints (`generar-xml`, `probar-token`) are kept alongside the real end-to-end submission endpoint (`enviar-xml`) to make QA/support troubleshooting easier.

## Requirements

### Requirement: Authenticate with Factura1
The system SHALL obtain an authentication token from Factura1 by calling `{Factura1:BaseUrl}{Factura1:AuthEndpoint}` with the configured `Factura1:Username`/`Factura1:Password` credentials.

#### Scenario: Successful authentication
- **WHEN** Factura1's auth endpoint responds successfully with a token
- **THEN** the system uses that token for the subsequent send-to-Factura1 call

#### Scenario: Authentication failure
- **WHEN** Factura1's auth endpoint does not respond successfully, or returns no token
- **THEN** the system does not attempt to send the factura and reports the authentication failure to the caller

### Requirement: Generate factura XML by folio
The system SHALL provide `GET /api/facturas/generar-xml/{folio}` returning the factura's XML generated from the database for a given folio, as a diagnostic/testing endpoint.

#### Scenario: XML found
- **WHEN** a folio with generatable XML is requested
- **THEN** the system returns `200 OK` with the XML content

#### Scenario: XML not found
- **WHEN** a folio has no generatable XML
- **THEN** the system returns `404 Not Found`

### Requirement: Test authentication independently
The system SHALL provide `GET /api/facturas/probar-token` returning the Factura1 authentication token, as a diagnostic/testing endpoint independent of sending a factura.

#### Scenario: Token retrieved
- **WHEN** Factura1 authentication succeeds
- **THEN** the system returns `200 OK` with the token

#### Scenario: Token retrieval fails
- **WHEN** Factura1 authentication fails
- **THEN** the system returns `400 Bad Request` describing the failure

### Requirement: Submit a factura to Factura1
The system SHALL provide `POST /api/facturas/enviar-xml/{folio}`, requiring a JSON body with `anoDoc`, `perDoc`, `tipo`, and `numero`, that generates the factura's XML, authenticates with Factura1, and submits the XML (base64-encoded) to `{Factura1:BaseUrl}/v2/factura`.

#### Scenario: Complete request payload
- **WHEN** the system submits a factura to Factura1
- **THEN** the request body includes `usuario` and `contrasena` (the same credentials used to authenticate), `sucursal` (from `Factura1:Sucursal`), and `base64doc` (the generated XML, base64-encoded)

#### Scenario: Authorization header
- **WHEN** the system submits a factura to Factura1
- **THEN** the request includes an `Authorization` header carrying the token obtained from authentication

#### Scenario: Folio has no XML
- **WHEN** the requested folio has no generatable XML
- **THEN** the system returns `404 Not Found` without attempting authentication or submission

#### Scenario: Authentication fails before submission
- **WHEN** authentication with Factura1 fails
- **THEN** the system returns an error response without attempting to submit the factura

#### Scenario: Successful submission with a typed response
- **WHEN** Factura1 responds successfully to the submission with a body matching its documented shape
- **THEN** the system returns `200 OK` with a typed response object exposing `qrdata`, `DIAN` (each with `mensaje`, `xml`, `valido`, `descripcion`, `statusCode`, and `respuesta` entries), `xml`, `clavtec`, `id`, `error`, and `cufe`

#### Scenario: Successful HTTP response with an unparseable body
- **WHEN** Factura1 responds with a successful HTTP status but a body that cannot be parsed into the expected shape
- **THEN** the system returns `200 OK` with the raw response body rather than failing with a server error

#### Scenario: Submission fails
- **WHEN** Factura1 responds with a non-successful HTTP status
- **THEN** the system returns that status to the caller with a message and the raw response detail

### Requirement: Persist Factura1 submission response
The system SHALL persist the outcome of a successful, successfully-parsed Factura1 submission by calling `usr_sp_itq_respuesta` with the document's identity, the resulting error/cufe values, and the XML that was sent.

#### Scenario: Successful submission is persisted
- **WHEN** Factura1's submission response is successfully parsed into the typed response object
- **THEN** the system calls `usr_sp_itq_respuesta` with `@ano_doc`/`@per_doc`/`@sub_tip`/`@num_doc` from the request body, `@codigoError` and `@valorError` both set to the response's `error` value, `@cufe` set to the response's `cufe` value, and `@doc_request` set to the plain (non-base64) XML that was submitted

#### Scenario: Persistence is skipped when there is no parsed outcome
- **WHEN** Factura1's submission fails (non-2xx), or succeeds but its body cannot be parsed into the typed response
- **THEN** the system does not call `usr_sp_itq_respuesta`

#### Scenario: Persistence failure does not hide a successful submission
- **WHEN** Factura1's submission is successfully parsed but the subsequent `usr_sp_itq_respuesta` call fails
- **THEN** the system still returns `200 OK` with Factura1's typed response, adding a warning indicating the internal save failed
