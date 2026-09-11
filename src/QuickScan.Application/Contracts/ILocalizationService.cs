using System;

namespace QuickScan.Application.Contracts;

/// <summary>
/// Contract for managing application localization.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current language code (e.g. 'en' or 'de').
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Switches the active application language.
    /// </summary>
    /// <param name="languageCode">The two-letter ISO language code.</param>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Gets a localized string for the specified key.
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Occurs when the active language changes.
    /// </summary>
    event Action? LanguageChanged;
}