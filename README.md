# Log Aggregator & Analyzer — DRÄXLMAIER Technical Test

A WPF desktop application (.NET 8) that monitors a folder for log files, parses them across multiple formats, aggregates entries by service and severity, and produces a sortable summary report.

---

## Architecture

```
LogAggregator.sln
├── LogAggregator.Core        → Domain models + Infrastructure (parsers, services)
├── LogAggregator.WPF         → WPF UI using MVVM pattern
└── LogAggregator.Plugins     → Example plugin (CSV parser)
```

**Clean Architecture — 4 layers:**

```
Domain (contracts + models)
    ↓
Infrastructure (parsers + services)
    ↓
Application (ParserRegistry, orchestration)
    ↓
WPF Presentation (MVVM, zero business logic in code-behind)
```

---

## Setup

**Prerequisites:** .NET 8 SDK, Visual Studio 2022

```
git clone https://github.com/arbihoussem/WassimSaibi.git
cd WassimSaibi/LogAggregator
dotnet build
```

Or open `LogAggregator.sln` in Visual Studio 2022 and press `Ctrl+Shift+B`.

---

## Usage

1. Run `LogAggregator.WPF` (set as startup project)
2. Click **Browse** → select a folder containing log files
3. Click **Start** → the app scans recursively and displays results in real-time
4. Use the **Filter** box to search by service name
5. Click **Skipped Entries** tab to review corrupted or unrecognized entries
6. Click **Export Report** to save a `.log` summary file

---

## Supported Formats

| Format | Extension | Field for severity |
|--------|-----------|-------------------|
| Plain text | `.txt`, `.log` | positional (3rd token) |
| JSON | `.json` | `"severity"` field |
| XML | `.xml` | `<severity>` element |
| CSV (plugin) | `.csv` | column index 1 |

### Text format — two patterns supported

```
2025-10-16 08:00:00 DEBUG PaymentService - Sample message
[2025-10-16 08:10:00] WARNING AuthService - Sample message
```

### JSON format

```json
[
  { "timestamp": "2025-10-16T08:00:00", "severity": "ERROR", "service": "AuthService", "message": "..." }
]
```

### XML format

```xml
<logs>
  <log>
    <timestamp>2025-10-16T08:00:00</timestamp>
    <severity>INFO</severity>
    <service>OrderService</service>
    <message>...</message>
  </log>
</logs>
```

---

## Timestamp Handling

| Format | Example |
|--------|---------|
| `yyyy-MM-ddTHH:mm:ss` | `2025-10-16T08:00:00` |
| `yyyy-MM-dd HH:mm:ss` | `2025-10-16 08:00:00` |
| `dd/MM/yyyy HH:mm:ss` | `16/10/2025 08:00:00` |
| `MM/dd/yyyy HH:mm:ss` | `10/16/2025 08:00:00` |

**Ambiguous timestamps** (e.g. `01/02/2025`) default to `dd/MM/yyyy` — European convention, appropriate for a German company.

**Unparseable timestamps** (e.g. `"invalid-timestamp"`, `"bad-timestamp"`) → `Timestamp = null`. The entry is still aggregated — the message is still valuable.

---

## Severity Mapping

| Raw value | Mapped to |
|-----------|-----------|
| `ERROR` | Error |
| `CRITICAL`, `FATAL` | Error (same bucket) |
| `WARNING`, `WARN` | Warning |
| `INFO`, `INFORMATION` | Info |
| `DEBUG` | Debug |
| `INVALID`, `UNKNOWN` | Unknown |

---

## Error Handling Strategy

> **Philosophy: Never crash. Always explain.**

| Situation | Decision |
|-----------|----------|
| Missing timestamp | Keep entry, `Timestamp = null` |
| Unparseable timestamp | Keep entry, `Timestamp = null`, flag in report |
| Missing service field | `ServiceName = "Unknown"` |
| Missing message | `Message = ""` |
| Unreadable line | Skip, log reason + raw content |
| Invalid JSON root | Skip file, log reason |
| Malformed XML | Skip file, log reason |
| No parser for extension | Skip file, log reason |
| Plugin DLL load failure | Skip plugin, log warning |

All skipped entries are visible in the **Skipped Entries** tab and exported report.

---

## Plugin System

Drop a DLL into the `/plugins` folder next to the exe — it's auto-discovered at startup.

**To create a plugin:**

1. Create a class library targeting `.NET 8`
2. Reference `LogAggregator.Core.dll`
3. Implement `ILogParser`:

```csharp
public class MyParser : ILogParser
{
    public string ParserName => "My Custom Parser (.dat)";
    public bool CanParse(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() == ".dat";
    public IEnumerable<LogEntry> Parse(string filePath) { ... }
}
```

4. Build → copy DLL to `/plugins` → restart the app

No changes to the main application are needed.

---

## Manual Validation Report

Five entries were randomly selected (seed=42) from the parsed dataset for manual review.

### Entry 1 — Accepted
**Source:** `logs_1.txt`
**Raw:** `2025-10-16 08:00:00 DEBUG PaymentService - Sample message 0`
**Decision:** ✅ ACCEPTED
**Reasoning:** Matches Pattern A (no brackets). Timestamp parses cleanly as `yyyy-MM-dd HH:mm:ss`. Level=DEBUG, Service=PaymentService. All fields present and valid.

### Entry 2 — Accepted (assumption)
**Source:** `log_0.json`
**Raw:** `{ "timestamp": "2025-10-16T08:00:00", "severity": "CRITICAL", "service": "AuthService", "message": "Critical failure" }`
**Decision:** ✅ ACCEPTED with assumption
**Reasoning:** `CRITICAL` is not a standard level in the spec but is semantically equivalent to `ERROR`. Mapped to `LogLevel.Error` to ensure it appears in the error count. Decision documented in `LevelParser.cs`.

### Entry 3 — Accepted (null timestamp)
**Source:** `logs_3.json`
**Raw:** `{ "timestamp": "invalid-timestamp", "severity": "WARNING", "service": "OrderService", "message": "Queue delay" }`
**Decision:** ✅ ACCEPTED with null timestamp
**Reasoning:** The timestamp is unparseable but the severity and message are valid. Discarding the entry would lose real operational data. `Timestamp = null` is stored — the entry contributes to the WARNING count.

### Entry 4 — Accepted (missing service)
**Source:** `log_2.txt`
**Raw:** `[2025-10-16 09:30:00] INVALID - Sample message 9`
**Decision:** ✅ ACCEPTED as Unknown service
**Reasoning:** The service field is absent (the `-` appears immediately after the level). `ServiceName = "Unknown"`. Level `INVALID` maps to `LogLevel.Unknown`. Entry is counted under the Unknown service row.

### Entry 5 — Skipped
**Source:** `logs_4.xml`
**Raw:** `<log><timestamp>bad-timestamp</timestamp><service>InventoryService</service><message>Missing severity</message></log>`
**Decision:** ⛔ SKIPPED
**Reasoning:** The `<severity>` element is completely absent. Without a severity level the entry cannot be meaningfully aggregated into any bucket. It is recorded in the Skipped Entries tab with reason "missing required `<severity>` element" so it remains visible for human review.

---

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| Clean Architecture | Parsers testable without UI. UI replaceable without touching business logic. |
| MVVM | Zero business logic in code-behind. DataContext = ViewModel only. |
| `ConcurrentDictionary` | FileSystemWatcher fires on ThreadPool threads. Aggregation must be thread-safe. |
| `yield return` | Lazy parsing — large files never fully loaded into RAM. |
| Fixed `Random(42)` seed | Manual validation entries are reproducible across runs. |
| `dd/MM/yyyy` default | Ambiguous dates default to European format — appropriate for a German company. |
| Plugin via reflection | `Assembly.LoadFrom` + `IsAssignableFrom` — drop DLL, zero main app changes. |

---

*Candidate: Wassim Saibi — DRÄXLMAIER .NET Developer Technical Test*
