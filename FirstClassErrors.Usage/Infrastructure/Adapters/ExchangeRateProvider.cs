using System.Diagnostics.CodeAnalysis;

namespace FirstClassErrors.Usage.Infrastructure.Adapters;

/// <summary>
///     The outgoing (secondary-port) adapter that calls an external exchange-rate provider to convert amounts.
/// </summary>
/// <remarks>
///     A minimal marker used only to anchor the exchange-rate error examples.
/// </remarks>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
                 Justification = "An intentional minimal marker: it only anchors the exchange-rate error examples through [ProvidesErrorsFor(nameof(...))] and carries no behaviour of its own.")]
public sealed class ExchangeRateProvider { }
