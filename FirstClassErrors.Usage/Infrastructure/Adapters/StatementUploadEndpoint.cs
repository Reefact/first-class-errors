using System.Diagnostics.CodeAnalysis;

namespace FirstClassErrors.Usage.Infrastructure.Adapters;

/// <summary>
///     The HTTP endpoint that ingests uploaded bank statements — an incoming (primary-port) adapter.
/// </summary>
/// <remarks>
///     A minimal marker used only to anchor the statement-upload error examples.
/// </remarks>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
                 Justification = "An intentional minimal marker: it only anchors the statement-upload error examples through [ProvidesErrorsFor(nameof(...))] and carries no behaviour of its own.")]
public sealed class StatementUploadEndpoint { }
