using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using QuickScan.Application.Contracts;

namespace QuickScan.UI.Services;

/// <summary>
/// Implements dynamic Avalonia localization with German and English resource dictionaries.
/// </summary>
public sealed class LocalizationService : ILocalizationService
{
    private static readonly Uri EnUri = new("avares://QuickScan.UI/Resources/Strings.en.axaml");
    private static readonly Uri DeUri = new("avares://QuickScan.UI/Resources/Strings.de.axaml");

    private ResourceDictionary? _currentDictionary;

    /// <inheritdoc/>
    public string CurrentLanguage { get; private set; } = "en";

    /// <inheritdoc/>
    public event Action? LanguageChanged;

    /// <summary>
    /// Initializes localization with system language or English default.
    /// </summary>
    public LocalizationService()
    {
        var initialLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase)
            ? "de"
            : "en";

        SetLanguage(initialLang);
    }

    /// <inheritdoc/>
    public void SetLanguage(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            languageCode = "en";
        }

        languageCode = languageCode.ToLowerInvariant().StartsWith("de") ? "de" : "en";
        CurrentLanguage = languageCode;

        var targetUri = languageCode == "de" ? DeUri : EnUri;

        if (Avalonia.Application.Current is { } app)
        {
            try
            {
                var newDict = (ResourceDictionary)AvaloniaXamlLoader.Load(targetUri);

                if (_currentDictionary != null)
                {
                    app.Resources.MergedDictionaries.Remove(_currentDictionary);
                }

                app.Resources.MergedDictionaries.Add(newDict);
                _currentDictionary = newDict;
            }
            catch
            {
                // Fallback or preview design time handling
            }
        }

        LanguageChanged?.Invoke();
    }

    /// <inheritdoc/>
    public string GetString(string key)
    {
        if (Avalonia.Application.Current != null &&
            Avalonia.Application.Current.TryGetResource(key, null, out var val) &&
            val is string str)
        {
            return str;
        }

        return key;
    }
}