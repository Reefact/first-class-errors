#region Usings declarations

using System.Globalization;

using Reefact.DiagnosableExceptions.Usage.Model;

#endregion

namespace Reefact.DiagnosableExceptions.Usage;

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

    private static string FormatDynamic(short value) {
        return value.ToString(DocumentationCulture);
    }

    private static string FormatDynamic(byte value) {
        return value.ToString(DocumentationCulture);
    }

    private static string FormatDynamic(decimal value) {
        return value
              .ToString(DocumentationCulture)
              .TrimEnd('0')
              .TrimEnd('.');
    }

    private static string FormatDynamic(this TemperatureUnit unit) {
        return unit switch {
            TemperatureUnit.Kelvin  => "K",
            TemperatureUnit.Celsius => "°C",
            _                       => unit.ToString()
        };
    }

    private static string FormatDynamic(object value) {
        return value.ToString() ?? value.GetType().Name;
    }

    private static string FormatDynamic(double value) {
        if (double.IsNaN(value)) { return DocumentationValue.NaN; }
        if (double.IsPositiveInfinity(value)) { return DocumentationValue.PlusInfinity; }
        if (double.IsNegativeInfinity(value)) { return DocumentationValue.MinusInfinity; }

        return value.ToString(DocumentationFormat.Numeric, DocumentationCulture);
    }

    private static string FormatDynamic(float value) {
        if (float.IsNaN(value)) { return DocumentationValue.NaN; }
        if (float.IsPositiveInfinity(value)) { return DocumentationValue.PlusInfinity; }
        if (float.IsNegativeInfinity(value)) { return DocumentationValue.MinusInfinity; }

        return value.ToString(DocumentationFormat.Numeric, DocumentationCulture);
    }

    private static string FormatDynamic(Guid value) {
        return value.ToString(DocumentationFormat.Guid);
    }

    private static string FormatDynamic(DateTime value) {
        return value
              .ToUniversalTime()
              .ToString(DocumentationFormat.DateTime, DocumentationCulture);
    }

    private static string FormatDynamic(DateTimeOffset value) {
        return value
              .ToUniversalTime()
              .ToString(DocumentationFormat.DateTime, DocumentationCulture);
    }

    private static string FormatDynamic(DateOnly value) {
        return value.ToString(DocumentationFormat.DateOnly, DocumentationCulture);
    }

    private static string FormatDynamic(TimeSpan value) {
        return value.ToString(DocumentationFormat.TimeSpan, DocumentationCulture);
    }

    private static string FormatDynamic(Enum value) {
        return value.ToString();
    }

    #endregion

    #region Nested types

    private static class DocumentationFormat {

        public const string Guid     = "D";
        public const string DateTime = "yyyy-MM-ddTHH:mm:ssZ";
        public const string DateOnly = "yyyy-MM-dd";
        public const string TimeSpan = "c";
        public const string Numeric  = "G";

    }

    private static class DocumentationValue {

        public const string Null          = "null";
        public const string True          = "true";
        public const string False         = "false";
        public const string NaN           = "NaN";
        public const string PlusInfinity  = "+Infinity";
        public const string MinusInfinity = "-Infinity";

    }

    #endregion

}