# HubLink Verification Checklist (Performance Task 5)

**Course:** ITSD81 — Desktop Application Development  
**Application:** HubLink — Regional Logistics Data Exchange  
**Test Service:** `HubLink-PT5-Local-Server-Windows-x64` (`http://127.0.0.1:5080`)  

---

## Acceptance & Verification Scenarios (AC-01 to AC-08)

| ID | Scenario | Verification Method | Status | Observed Result |
|----|----------|---------------------|--------|-----------------|
| **AC-01** | Retrieve shipment information from the provided API for a configured branch (`BR-001`). | Automated test (`GetShipmentsAsync_NormalBranchBR001_Returns5Items`) + live HTTP GET to `/api/shipments?branchCode=BR-001`. | Verified | HTTP 200 OK; 5 shipments (`HUB-1001` to `HUB-1005`) deserialized and bound to the UI DataGrid. |
| **AC-02** | Apply different shipment status filters (`Pending`, `InTransit`, `Delivered`, `Exception`, `All`). | `MainWindowViewModel.ApplyFilter()` logic verified against `BR-001` dataset without additional HTTP calls. | Verified | Selecting a status filters `DisplayedShipments` in-memory; selecting `All` restores all 5 records. |
| **AC-03** | Examine the operational summary. | Automated test (`FromShipments_WithBR001Fixtures_CalculatesAccurateCounts`). | Verified | Summary cards accurately display Total: 5, Pending: 1, InTransit: 2, Delivered: 1, Exception: 1 across all retrieved branch records regardless of active filter. |
| **AC-04** | Refresh information from the service asynchronously. | `FetchShipmentsCommand` executes `async`/`await` HTTP request; `IsLoading` disables overlapping clicks, displays progress bar, and enables Cancel button. | Verified | UI remains responsive during retrieval; loading indicator and `Last synced` timestamp update cleanly upon completion. |
| **AC-05** | Encounter unavailable service, request timeout, or unsuccessful HTTP response. | Automated tests (`bad-request`, `server-error`, `delay` timeout, unreachable port `59999`) + Error Alert Banner with Retry button in UI. | Verified | HTTP 400/500, timeout (408), and connection failure (503) display clear error feedback, retain prior data with a stale-data notice if available, and provide a Retry button. |
| **AC-06** | Receive an empty or invalid response. | Automated tests (`BR-999` valid empty branch + `mode=invalid-json`). | Verified | `BR-999` returns HTTP 200 with 0 items and shows a valid empty-branch state (`IsSuccess = true, IsEmpty = true`); `invalid-json` returns an error message without crashing. |
| **AC-07** | Export shipment information while a status filter is active. | Automated test (`GenerateCsv_WithValidShipments_ProducesCorrectHeaderAndEscapedData`) + generated [Sample_Export_BR-001.csv](Sample_Export_BR-001.csv). | Verified | Exports exact column order (`TrackingNumber,Origin,Destination,Status,LastUpdatedUtc`) with RFC 4180 escaping for commas and quotes (e.g., `"Butuan, Agusan del Norte"`, `"Depot ""North"""`). |
| **AC-08** | Attempt an export that cannot be completed. | Automated test (`GenerateCsv_WithEmptyCollection_ThrowsInvalidOperationException`) + UI empty-export guard and exception handler. | Verified | Attempting to export when 0 records are displayed aborts with an understandable warning; file I/O errors report failure without claiming success. |

---

## Mandatory Operational Rules Compliance

- **OR-01 / OR-02:** All shipment data originates from real HTTP GET requests using the configured `BaseAddress` and `BranchCode`.
- **OR-03 / OR-04:** JSON is deserialized into typed `BranchShipmentsResponse` / `Shipment` models; summary reflects full branch data while filter controls visible rows.
- **OR-05 / OR-07 / OR-10:** HTTP errors, timeouts, and malformed JSON are never treated as success; valid empty responses (`BR-999` / `mode=empty`) are distinguished from failures.
- **OR-06:** Network I/O and file export use `async`/`await` without blocking the Avalonia UI thread.
- **OR-08 / OR-09:** CSV exports reflect the active filter and follow RFC 4180 structure.
