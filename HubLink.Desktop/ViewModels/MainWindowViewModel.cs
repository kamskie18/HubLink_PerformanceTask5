using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HubLink.Application.Configuration;
using HubLink.Application.Services;
using HubLink.Domain.Models;
using Microsoft.Extensions.Options;

namespace HubLink.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IHubLinkApiClient _apiClient;
    private readonly ICsvExportService _csvExportService;
    private readonly HubLinkOptions _options;

    private List<Shipment> _allRetrievedShipments = new();

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
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = "Ready. Click 'Fetch Shipments' to retrieve branch records.";

    [ObservableProperty]
    private bool _isError = false;

    [ObservableProperty]
    private bool _hasStaleData = false;

    [ObservableProperty]
    private string _staleDataNotice = string.Empty;

    [ObservableProperty]
    private ShipmentSummary _summary = new();

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
        IOptions<HubLinkOptions> options)
    {
        _apiClient = apiClient;
        _csvExportService = csvExportService;
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
        StatusMessage = $"Connecting to {BaseAddress} for branch {BranchCode}...";
        IsError = false;

        try
        {
            var result = await _apiClient.GetShipmentsAsync(
                BranchCode,
                UseTestEndpoint,
                SelectedTestMode);

            if (result.IsSuccess && result.Data != null)
            {
                _allRetrievedShipments = result.Data.Shipments ?? new List<Shipment>();
                HasStaleData = false;
                StaleDataNotice = string.Empty;

                Summary = ShipmentSummary.FromShipments(_allRetrievedShipments);
                ApplyFilter();

                if (result.IsEmpty)
                {
                    StatusMessage = $"Successfully retrieved branch {BranchCode}. Result: 0 shipments found (valid empty response).";
                }
                else
                {
                    StatusMessage = $"Successfully retrieved {_allRetrievedShipments.Count} shipment(s) for branch {BranchCode}.";
                }
            }
            else
            {
                IsError = true;
                StatusMessage = $"Retrieval Failed: {result.ErrorMessage}";

                // If previous data exists, keep it visible but indicate clearly that it is stale (per Section IV.E)
                if (_allRetrievedShipments.Count > 0)
                {
                    HasStaleData = true;
                    StaleDataNotice = "Note: Displaying previously retrieved records. The latest refresh attempt failed.";
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
                StaleDataNotice = "Note: Displaying previously retrieved records. The latest refresh attempt failed.";
            }
        }
        finally
        {
            IsLoading = false;
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
    }

    [RelayCommand]
    public async Task ExportCsvAsync(string? targetFilePath = null)
    {
        if (DisplayedShipments.Count == 0)
        {
            IsError = true;
            StatusMessage = "Export aborted: No shipment records are available under the active filter to export.";
            return;
        }

        try
        {
            var destination = targetFilePath;
            if (string.IsNullOrWhiteSpace(destination))
            {
                var exportDir = Path.Combine(AppContext.BaseDirectory, "Exports");
                destination = Path.Combine(exportDir, $"HubLink_{BranchCode}_{SelectedFilter}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            }

            await _csvExportService.ExportToFileAsync(DisplayedShipments, destination);

            IsError = false;
            StatusMessage = $"CSV Exported successfully ({DisplayedShipments.Count} records): {Path.GetFileName(destination)}";
        }
        catch (Exception ex)
        {
            IsError = true;
            StatusMessage = $"CSV Export failed: {ex.Message}";
        }
    }
}
