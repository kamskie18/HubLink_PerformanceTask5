using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HubLink.Application.Configuration;
using HubLink.Application.Services;
using HubLink.Desktop.Services;
using HubLink.Domain.Models;
using Microsoft.Extensions.Options;

namespace HubLink.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IHubLinkApiClient _apiClient;
    private readonly ICsvExportService _csvExportService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly HubLinkOptions _options;

    private List<Shipment> _allRetrievedShipments = new();
    private CancellationTokenSource? _fetchCts;
    private bool _hasLoadedOnce = false;

    [ObservableProperty]
    private string _baseAddress = "http://127.0.0.1:5080";

    [ObservableProperty]
    private string _branchCode = "BR-001";

    [ObservableProperty]
    private bool _useTestEndpoint = false;

    [ObservableProperty]
    private string _selectedTestMode = "normal";

    [ObservableProperty]
    private string _selectedFilter = "All";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _isLoading = false;

    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready. Click 'Fetch Shipments' to retrieve branch records.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _isError = false;

    [ObservableProperty]
    private bool _hasStaleData = false;

    [ObservableProperty]
    private string _staleDataNotice = string.Empty;

    [ObservableProperty]
    private string _lastSyncedText = "Not synced yet";

    [ObservableProperty]
    private string _emptyStateMessage = "No shipments loaded yet. Click 'Fetch Shipments' to connect to the central service.";

    [ObservableProperty]
    private ShipmentSummary _summary = new();

    public bool ShowEmptyState => !IsLoading && DisplayedShipments.Count == 0;

    public ObservableCollection<Shipment> DisplayedShipments { get; } = new();

    public List<string> FilterOptions { get; } = new()
    {
        "All",
        "Pending",
        "InTransit",
        "Delivered",
        "Exception"
    };

    public List<string> TestModes { get; } = new()
    {
        "normal",
        "empty",
        "bad-request",
        "server-error",
        "invalid-json",
        "delay"
    };

    public MainWindowViewModel(
        IHubLinkApiClient apiClient,
        ICsvExportService csvExportService,
        IOptions<HubLinkOptions> options,
        IFileDialogService? fileDialogService = null)
    {
        _apiClient = apiClient;
        _csvExportService = csvExportService;
        _fileDialogService = fileDialogService;
        _options = options.Value;

        if (!string.IsNullOrWhiteSpace(_options.BaseAddress))
            BaseAddress = _options.BaseAddress;
        if (!string.IsNullOrWhiteSpace(_options.BranchCode))
            BranchCode = _options.BranchCode;
        UseTestEndpoint = _options.UseTestEndpoint;
        if (!string.IsNullOrWhiteSpace(_options.TestMode))
            SelectedTestMode = _options.TestMode;
    }

    partial void OnSelectedFilterChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    public async Task FetchShipmentsAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        IsError = false;
        StatusMessage = UseTestEndpoint
            ? $"Connecting to {BaseAddress} (Test Mode: {SelectedTestMode}) for branch {BranchCode}..."
            : $"Connecting to {BaseAddress} for branch {BranchCode}...";

        _fetchCts?.Dispose();
        _fetchCts = new CancellationTokenSource();

        try
        {
            var result = await _apiClient.GetShipmentsAsync(
                branchCode: BranchCode,
                useTestEndpoint: UseTestEndpoint,
                testMode: SelectedTestMode,
                baseAddress: BaseAddress,
                cancellationToken: _fetchCts.Token);

            if (result.IsSuccess && result.Data != null)
            {
                _hasLoadedOnce = true;
                _allRetrievedShipments = result.Data.Shipments ?? new List<Shipment>();
                HasStaleData = false;
                StaleDataNotice = string.Empty;
                LastSyncedText = $"Last synced: {DateTime.Now:HH:mm:ss}";

                Summary = ShipmentSummary.FromShipments(_allRetrievedShipments);
                ApplyFilter();

                if (result.IsEmpty)
                {
                    StatusMessage = $"Connected to branch {result.Data.BranchCode}. Valid empty response: 0 shipments found.";
                    EmptyStateMessage = $"Branch {result.Data.BranchCode} currently has 0 shipments on record.";
                }
                else
                {
                    StatusMessage = $"Retrieved {_allRetrievedShipments.Count} shipment(s) for branch {result.Data.BranchCode}.";
                }
            }
            else
            {
                IsError = true;
                StatusMessage = $"Retrieval Failed: {result.ErrorMessage}";

                if (_allRetrievedShipments.Count > 0)
                {
                    HasStaleData = true;
                    StaleDataNotice = $"Showing previously loaded records ({LastSyncedText}). Latest refresh failed.";
                }
                else
                {
                    EmptyStateMessage = "Unable to retrieve shipments. Check the error banner above and click Retry.";
                }
            }
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"Unexpected error: {ex.Message}";
            if (_allRetrievedShipments.Count > 0)
            {
                HasStaleData = true;
                StaleDataNotice = $"Showing previously loaded records ({LastSyncedText}). Latest refresh failed.";
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    [RelayCommand]
    public void CancelFetch()
    {
        if (IsLoading && _fetchCts != null && !_fetchCts.IsCancellationRequested)
        {
            _fetchCts.Cancel();
            StatusMessage = "Cancelling active request...";
        }
    }

    private void ApplyFilter()
    {
        DisplayedShipments.Clear();

        IEnumerable<Shipment> filtered = _allRetrievedShipments;
        if (!string.Equals(SelectedFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(s => string.Equals(s.Status, SelectedFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered)
        {
            DisplayedShipments.Add(item);
        }

        if (_hasLoadedOnce && _allRetrievedShipments.Count > 0 && DisplayedShipments.Count == 0)
        {
            EmptyStateMessage = $"No shipments match the '{SelectedFilter}' status filter. Select 'All' to view all {_allRetrievedShipments.Count} record(s).";
        }
        else if (_hasLoadedOnce && _allRetrievedShipments.Count == 0)
        {
            EmptyStateMessage = $"Branch {BranchCode} currently has 0 shipments on record.";
        }

        OnPropertyChanged(nameof(ShowEmptyState));
    }

    [RelayCommand]
    public async Task ExportCsvAsync(string? targetFilePath = null)
    {
        if (DisplayedShipments.Count == 0)
        {
            IsError = true;
            StatusMessage = "Export aborted: No shipment records are currently displayed under the active filter.";
            return;
        }

        try
        {
            var suggestedName = $"HubLink_{BranchCode}_{SelectedFilter}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var destination = targetFilePath;

            if (string.IsNullOrWhiteSpace(destination))
            {
                if (_fileDialogService != null)
                {
                    destination = await _fileDialogService.PickCsvSavePathAsync(suggestedName);
                    if (string.IsNullOrWhiteSpace(destination))
                    {
                        IsError = false;
                        StatusMessage = "CSV export cancelled.";
                        return;
                    }
                }
                else
                {
                    var exportDir = Path.Combine(AppContext.BaseDirectory, "Exports");
                    destination = Path.Combine(exportDir, suggestedName);
                }
            }

            await _csvExportService.ExportToFileAsync(DisplayedShipments, destination);

            IsError = false;
            StatusMessage = $"CSV exported successfully ({DisplayedShipments.Count} record(s)) to {Path.GetFileName(destination)}.";
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"CSV Export failed: {ex.Message}";
        }
    }
}
