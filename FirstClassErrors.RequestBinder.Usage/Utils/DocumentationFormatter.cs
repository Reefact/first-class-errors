#region Usings declarations

using System.Globalization;

#endregion

namespace FirstClassErrors.RequestBinder.Usage.Utils;

/// <summary>
///     Formats the values interpolated into the sample's <b>diagnostic</b> (author-language) error messages so they
///     render culture-invariantly — an integer, a date or a decimal reads identically on every machine, and the
///     generated documentation stays reproducible. Mirrors the same helper in <c>FirstClassErrors.Usage</c>.
/// </summary>
internal static class DocumentationFormatter {

    #region Static members

    private static readonly CultureInfo DocumentationCulture = CultureInfo.InvariantCulture;

    public static string Format(string format, params object[]? arguments) {
        ArgumentNullException.ThrowIfNull(format);

        if (arguments is null || arguments.Length == 0) { return format; }

        string[] formattedArguments = arguments
                                     .Select(Format)
                                     .ToArray();

        return string.Format(CultureInfo.InvariantCulture, format, formattedArguments);
    }

    private static string Format(object? value) {
        if (value is null) { return DocumentationValue.Null; }

        return FormatDynamic((dynamic)value);
    }

    private static string FormatDynamic(string value) {
        return value;
    }

    private static string FormatDynamic(bool value) {
        return value ? DocumentationValue.True : DocumentationValue.False;
    }

    private static string FormatDynamic(int value) {
        return value.ToString(DocumentationCulture);
    }

    private static string FormatDynamic(long value) {
        return value.ToString(DocumentationCulture);
    }

    private static string FormatDynamic(decimal value) {
        return value.ToString(DocumentationCulture);
    }

    private static string FormatDynamic(DateOnly value) {
        return value.ToString(DocumentationFormat.DateOnly, DocumentationCulture);
    }

    private static string FormatDynamic(DateTime value) {
        return value.ToUniversalTime().ToString(DocumentationFormat.DateTime, DocumentationCulture);
    }

    private static string FormatDynamic(Guid value) {
        return value.ToString(DocumentationFormat.Guid);
    }

    private static string FormatDynamic(Enum value) {
        return value.ToString();
    }

    private static string FormatDynamic(object value) {
        return value.ToString() ?? value.GetType().Name;
    }

    #endregion

    #region Nested types

    private static class DocumentationFormat {

        public const string Guid     = "D";
        public const string DateTime = "yyyy-MM-ddTHH:mm:ssZ";
        public const string DateOnly = "yyyy-MM-dd";

    }

    private static class DocumentationValue {

        public const string Null  = "null";
        public const string True  = "true";
        public const string False = "false";

    }

    #endregion

}
