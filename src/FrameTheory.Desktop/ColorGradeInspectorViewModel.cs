using System.ComponentModel;
using System.Runtime.CompilerServices;
using FrameTheory.Color;
using FrameTheory.Photo;

namespace FrameTheory.Desktop;

public sealed class ColorGradeInspectorViewModel : INotifyPropertyChanged
{
    private PpmRaster? _source;
    private ColorGrade _grade = new();
    private CubeLut? _lut;
    private string _statusText = "Import local media to inspect a real before/after grade.";
    private string _beforeSummary = "Before: no media";
    private string _afterSummary = "After: no media";
    private string _scopeSummary = "Scopes: no media";

    public ColorGrade Grade => _grade;

    public byte[]? BeforePreviewPpm => _source?.ToPpm();

    public byte[]? AfterPreviewPpm => _source?.Apply(ColorGraph.ForSrgb(), _grade, _lut).ToPpm();

    public ColorScopeData? Scopes => _source is null ? null : ColorScopeAnalyzer.Analyze(_source.Width, _source.Height, _source.RgbBytes);

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

    public string BeforeSummary
    {
        get => _beforeSummary;
        private set
        {
            if (value == _beforeSummary)
            {
                return;
            }

            _beforeSummary = value;
            OnPropertyChanged();
        }
    }

    public string AfterSummary
    {
        get => _afterSummary;
        private set
        {
            if (value == _afterSummary)
            {
                return;
            }

            _afterSummary = value;
            OnPropertyChanged();
        }
    }

    public string ScopeSummary
    {
        get => _scopeSummary;
        private set
        {
            if (value == _scopeSummary)
            {
                return;
            }

            _scopeSummary = value;
            OnPropertyChanged();
        }
    }

    public float ExposureEV
    {
        get => _grade.ExposureEV;
        set => SetGrade(new ColorGrade(value, Contrast, Saturation, Temperature, Tint));
    }

    public float Contrast
    {
        get => _grade.Contrast;
        set => SetGrade(new ColorGrade(ExposureEV, value, Saturation, Temperature, Tint));
    }

    public float Saturation
    {
        get => _grade.Saturation;
        set => SetGrade(new ColorGrade(ExposureEV, Contrast, value, Temperature, Tint));
    }

    public float Temperature
    {
        get => _grade.Temperature;
        set => SetGrade(new ColorGrade(ExposureEV, Contrast, Saturation, value, Tint));
    }

    public float Tint
    {
        get => _grade.Tint;
        set => SetGrade(new ColorGrade(ExposureEV, Contrast, Saturation, Temperature, value));
    }

    public void SetSource(byte[]? previewPpm)
    {
        if (previewPpm is null)
        {
            _source = null;
            BeforeSummary = "Before: no image thumbnail";
            AfterSummary = "After: no image thumbnail";
            ScopeSummary = "Scopes: no image thumbnail";
            StatusText = "Imported media has no image preview.";
            NotifyPreviewChanged();
            return;
        }

        try
        {
            _source = PpmRaster.Parse(previewPpm);
            StatusText = $"Inspecting {_source.Width}x{_source.Height} sRGB pixels.";
            RecomputeSummaries();
            NotifyPreviewChanged();
        }
        catch (InvalidDataException exception)
        {
            _source = null;
            StatusText = $"Color preview unavailable: {exception.Message}";
            BeforeSummary = "Before: invalid preview";
            AfterSummary = "After: invalid preview";
            ScopeSummary = "Scopes: invalid preview";
            NotifyPreviewChanged();
        }
    }

    public void ResetGrade()
    {
        _lut = null;
        SetGrade(new ColorGrade());
    }

    public ColorPreset CreatePreset(string name) => new(name, _grade, _lut);

    public void ApplyPreset(ColorPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        _grade = preset.Grade;
        _lut = preset.Lut;
        StatusText = $"Applied style '{preset.Name}' to the local color preview.";
        RecomputeSummaries();
        NotifyPreviewChanged();
    }

    public void ApplyGrade(ColorGrade grade)
    {
        _lut = null;
        SetGrade(grade);
        StatusText = "Applied the reviewed reference grade to the local color preview.";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetGrade(ColorGrade grade)
    {
        _grade = grade;
        RecomputeSummaries();
        NotifyPreviewChanged();
    }

    private void RecomputeSummaries()
    {
        if (_source is null)
        {
            return;
        }

        BeforeSummary = FormatSummary("Before", _source);
        AfterSummary = FormatSummary("After", _source.Apply(ColorGraph.ForSrgb(), _grade, _lut));
        var scopes = Scopes!;
        ScopeSummary = $"Scopes: histogram peaks R {Peak(scopes.RedHistogram)}, G {Peak(scopes.GreenHistogram)}, B {Peak(scopes.BlueHistogram)}; waveform bins {scopes.Waveform.Count}; vectorscope bins {scopes.Vectorscope.Count}";
    }

    private static string FormatSummary(string label, PpmRaster raster)
    {
        var red = 0L;
        var green = 0L;
        var blue = 0L;
        var pixels = raster.RgbBytes.Length / 3;
        for (var index = 0; index < raster.RgbBytes.Length; index += 3)
        {
            red += raster.RgbBytes[index];
            green += raster.RgbBytes[index + 1];
            blue += raster.RgbBytes[index + 2];
        }

        return $"{label}: {raster.Width}x{raster.Height}; mean RGB {red / (double)pixels:0.0}, {green / (double)pixels:0.0}, {blue / (double)pixels:0.0}";
    }

    private void NotifyPreviewChanged()
    {
        OnPropertyChanged(nameof(Grade));
        OnPropertyChanged(nameof(ExposureEV));
        OnPropertyChanged(nameof(Contrast));
        OnPropertyChanged(nameof(Saturation));
        OnPropertyChanged(nameof(Temperature));
        OnPropertyChanged(nameof(Tint));
        OnPropertyChanged(nameof(BeforePreviewPpm));
        OnPropertyChanged(nameof(AfterPreviewPpm));
        OnPropertyChanged(nameof(Scopes));
    }

    private static int Peak(IReadOnlyList<int> histogram)
    {
        var peak = 0;
        for (var index = 1; index < histogram.Count; index++)
        {
            if (histogram[index] > histogram[peak])
            {
                peak = index;
            }
        }

        return peak;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
