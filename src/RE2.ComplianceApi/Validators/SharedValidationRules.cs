using FluentValidation;

namespace RE2.ComplianceApi.Validators;

/// <summary>
/// Shared OWASP-aligned validation rules for input sanitization.
/// T289: Prevent injection, XSS, and buffer abuse patterns.
/// </summary>
public static class SharedValidationRules
{
    /// <summary>Maximum length for general text fields to prevent buffer abuse.</summary>
    public const int MaxTextLength = 2000;

    /// <summary>Maximum length for code/ID fields.</summary>
    public const int MaxCodeLength = 100;

    /// <summary>Maximum length for short text fields (names, reasons).</summary>
    public const int MaxShortTextLength = 500;

    private static readonly char[] AllowedCodeChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_./".ToCharArray();

    /// <summary>
    /// Validates that text does not contain HTML/script tags (XSS prevention).
    /// </summary>
    public static IRuleBuilderOptions<T, string> NoHtmlTags<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || !ContainsHtmlTags(value))
            .WithMessage("'{PropertyName}' must not contain HTML or script tags.");
    }

    /// <summary>
    /// Validates that nullable text does not contain HTML/script tags.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> NoHtmlTagsNullable<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || !ContainsHtmlTags(value))
            .WithMessage("'{PropertyName}' must not contain HTML or script tags.");
    }

    /// <summary>
    /// Validates that a code/ID field contains only safe characters (alphanumeric + dash/underscore/dot/slash).
    /// </summary>
    public static IRuleBuilderOptions<T, string> SafeCode<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || value.All(c => AllowedCodeChars.Contains(c)))
            .WithMessage("'{PropertyName}' contains invalid characters. Only letters, digits, dashes, underscores, dots, and slashes are allowed.");
    }

    /// <summary>
    /// Validates that an ISO 3166-1 alpha-2 country code is properly formatted.
    /// </summary>
    public static IRuleBuilderOptions<T, string> IsoCountryCode<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Length(2)
            .Matches("^[A-Z]{2}$")
            .WithMessage("'{PropertyName}' must be a valid ISO 3166-1 alpha-2 country code (e.g., 'NL', 'DE').");
    }

    /// <summary>
    /// Validates that a nullable country code is properly formatted when present.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> IsoCountryCodeNullable<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => value == null || System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Z]{2}$"))
            .WithMessage("'{PropertyName}' must be a valid ISO 3166-1 alpha-2 country code (e.g., 'NL', 'DE').");
    }

    /// <summary>
    /// Applies standard text field validation: max length + no HTML tags.
    /// </summary>
    public static IRuleBuilderOptions<T, string> SafeText<T>(
        this IRuleBuilder<T, string> ruleBuilder, int maxLength = MaxShortTextLength)
    {
        return ruleBuilder
            .MaximumLength(maxLength)
            .NoHtmlTags();
    }

    /// <summary>
    /// Applies standard nullable text field validation: max length + no HTML tags.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> SafeTextNullable<T>(
        this IRuleBuilder<T, string?> ruleBuilder, int maxLength = MaxShortTextLength)
    {
        return ruleBuilder
            .MaximumLength(maxLength)
            .NoHtmlTagsNullable();
    }

    private static bool ContainsHtmlTags(string value)
    {
        // Check for common HTML/script injection patterns
        var lower = value.ToLowerInvariant();
        return lower.Contains('<') && lower.Contains('>') ||
               lower.Contains("javascript:") ||
               lower.Contains("onerror=") ||
               lower.Contains("onload=") ||
               lower.Contains("onclick=");
    }
}
