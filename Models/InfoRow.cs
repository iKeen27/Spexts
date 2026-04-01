using System.Windows;
using System.Windows.Input;
using Spexts.Helpers;
using Spexts.ViewModels;

namespace Spexts.Models;

public class InfoRow : BaseViewModel
{
    private string _label = string.Empty;
    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    private string _value = string.Empty;
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    private string? _valueColor;
    /// <summary>
    /// Hex color string for the value text (e.g. "#3FB950" for green).
    /// Null means use default theme color (#E6EDF3).
    /// </summary>
    public string? ValueColor
    {
        get => _valueColor;
        set => SetProperty(ref _valueColor, value);
    }

    public ICommand CopyCommand { get; }
    public ICommand CopyLabelCommand { get; }

    public InfoRow(string label = "", string value = "", string? valueColor = null)
    {
        _label = label;
        _value = value;
        _valueColor = valueColor;

        CopyCommand = new RelayCommand(_ =>
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                    Clipboard.SetText(Value ?? string.Empty));
            }
            catch { }
        });

        CopyLabelCommand = new RelayCommand(_ =>
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                    Clipboard.SetText(Label ?? string.Empty));
            }
            catch { }
        });
    }
}
