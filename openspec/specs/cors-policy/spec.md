# cors-policy Specification

## Purpose

Controls which browser origins may call this API. No CORS policy existed before `facturas-listado` needed one for the JuntaNacional frontend to reach the API from a browser; this capability keeps that access explicit and configuration-driven rather than open to any origin.

## Requirements

### Requirement: Configurable CORS allow-list
The system SHALL allow cross-origin browser requests only from origins listed in configuration (`Cors:AllowedOrigins`), and SHALL NOT allow requests from unlisted origins.

#### Scenario: Allowed origin
- **WHEN** a browser sends a request with an `Origin` header matching an entry in `Cors:AllowedOrigins`
- **THEN** the system includes the appropriate CORS headers so the browser accepts the response

#### Scenario: Origin not in the allow-list
- **WHEN** a browser sends a request with an `Origin` header that is not in `Cors:AllowedOrigins`
- **THEN** the system does not include CORS headers permitting that origin, so the browser blocks the response from being read by the page

#### Scenario: No wildcard origin
- **WHEN** the CORS policy is evaluated for any request
- **THEN** the system never responds with an `Access-Control-Allow-Origin: *` header

### Requirement: Development configuration ships with local frontend origins
The system SHALL ship `appsettings.Development.json` with the JuntaNacional frontend's common local dev server origins pre-listed in `Cors:AllowedOrigins`.

#### Scenario: Local development request
- **WHEN** the API runs in the Development environment and a request arrives from one of the pre-listed local origins (e.g. `http://localhost:5173`)
- **THEN** the request is treated as an allowed origin under the Requirement above without additional configuration
