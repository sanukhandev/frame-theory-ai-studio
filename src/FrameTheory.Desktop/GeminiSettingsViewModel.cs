using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameTheory.AI;

namespace FrameTheory.Desktop;

public sealed class GeminiSettingsViewModel : INotifyPropertyChanged
{
    private readonly GeminiConfigurationService _configuration;
    private string _selectedModelId = GeminiModelCatalog.Supported[0].Id;
    private bool _hasStoredApiKey;
    private string _statusText = "Manual editing is offline-ready; no Gemini key is loaded.";

    public GeminiSettingsViewModel(GeminiConfigurationService configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Models = GeminiModelCatalog.Supported.Select(model => model.Id).ToArray();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> Models { get; }

    public string SelectedModelId
    {
        get => _selectedModelId;
        set
        {
            if (!GeminiModelCatalog.Contains(value) || value == _selectedModelId)
            {
                return;
            }

            _selectedModelId = value;
            OnPropertyChanged();
        }
    }

    public bool HasStoredApiKey
    {
        get => _hasStoredApiKey;
        private set
        {
            if (value == _hasStoredApiKey)
            {
                return;
            }

            _hasStoredApiKey = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (value == _statusText)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await _configuration.LoadAsync(cancellationToken);
            SelectedModelId = configuration.ModelId;
            HasStoredApiKey = configuration.HasApiKey;
            StatusText = HasStoredApiKey
                ? "Gemini key is stored in the operating-system credential store. Uploads still require consent."
                : "Manual editing is offline-ready; no Gemini key is stored.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Gemini settings load canceled; manual editing remains available.";
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or InvalidOperationException or UnauthorizedAccessException)
        {
            HasStoredApiKey = false;
            StatusText = $"Secure Gemini storage is unavailable: {exception.Message} Manual editing remains available.";
        }
    }

    public async Task SaveAsync(string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await _configuration.SaveAsync(SelectedModelId, apiKey, cancellationToken);
            HasStoredApiKey = configuration.HasApiKey;
            StatusText = "Gemini key stored in the operating-system credential store. Media upload remains opt-in.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Gemini key save canceled.";
        }
        catch (Exception exception) when (exception is ArgumentException or PlatformNotSupportedException or InvalidOperationException or UnauthorizedAccessException)
        {
            StatusText = $"Gemini key was not stored: {exception.Message}";
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _configuration.DeleteAsync(cancellationToken);
            HasStoredApiKey = false;
            StatusText = "Gemini key removed; manual editing remains available offline.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Gemini key removal canceled.";
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or InvalidOperationException or UnauthorizedAccessException)
        {
            StatusText = $"Gemini key was not removed: {exception.Message}";
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
