# HubLink — Regional Logistics Data Exchange
**ITSD81 Desktop Application Development — Performance Task 5**

## 1. Project Overview
HubLink is an asynchronous desktop application built with C#, .NET 10, Avalonia UI, and the CommunityToolkit MVVM pattern. It connects partner branch operations staff to a centralized regional logistics shipment REST API.

The application allows operations staff to:
- Connect to the central provider service via non-blocking asynchronous HTTP requests (`HttpClient`).
- View and inspect shipments associated with their branch (`BR-001`, `BR-002`, `BR-003`).
- Filter retrieved shipments in-memory by status (`All`, `Pending`, `InTransit`, `Delivered`, `Exception`).
- Monitor live operational summary counters (Total, Pending, InTransit, Delivered, Exception) reflecting the entire retrieved branch dataset.
- Export filtered records to a standardized CSV report matching RFC 4180 escaping rules.
- Gracefully handle network timeouts, unreachable servers, invalid JSON, and simulated HTTP 400/500 errors.

---

## 2. Solution Architecture
```
HubLink.sln
├── HubLink.Domain
│   └── Models (Shipment, ShipmentStatus, BranchShipmentsResponse, ShipmentSummary)
├── HubLink.Application
│   ├── Configuration (HubLinkOptions)
│   ├── Models (ApiResponse<T>)
│   └── Services (IHubLinkApiClient, HubLinkApiClient, ICsvExportService, CsvExportService)
├── HubLink.Desktop (Avalonia UI client)
│   ├── ViewModels (MainWindowViewModel)
│   ├── Views (MainWindow.axaml, MainWindow.axaml.cs)
│   └── appsettings.json
└── HubLink.Tests (xUnit test suite)
    ├── ShipmentSummaryTests.cs
    ├── CsvExportServiceTests.cs
    └── HubLinkApiClientTests.cs
```

---

## 3. Configuration & Running the Application

### A. Start the HubLink Local Server
Ensure the instructor-provided test service is running on `http://127.0.0.1:5080`:
```cmd
cd "PT5\HubLink-PT5-Local-Server-Windows-x64"
Start-HubLink.cmd
```

### B. Configuration (`appsettings.json`)
The service URL and default branch are configured in `HubLink.Desktop/appsettings.json`:
```json
{
  "HubLink": {
    "BaseAddress": "http://127.0.0.1:5080",
    "BranchCode": "BR-001",
    "TimeoutSeconds": 5,
    "UseTestEndpoint": false,
    "TestMode": "normal"
  }
}
```
Both the service address and branch identifier can also be overridden directly in the desktop UI at runtime.

### C. Build & Run
```powershell
dotnet restore
dotnet build
dotnet run --project HubLink.Desktop
```

### D. Run Automated Tests
```powershell
dotnet test
```

---

## 4. Controlled Test Scenarios (AC-01 through AC-08)
HubLink supports toggling the controlled test endpoint directly from the interface:
- **`normal`**: Standard HTTP 200 retrieval with 5 shipments for BR-001.
- **`empty` / `BR-999`**: Valid empty branch response (0 shipments, treated as successful empty state per OR-07).
- **`bad-request`**: Simulates HTTP 400 bad request error.
- **`server-error`**: Simulates HTTP 500 internal server error.
- **`invalid-json`**: Simulates incomplete/malformed JSON response.
- **`delay`**: 10-second wait to exercise client-side cancellation and timeout handling.
