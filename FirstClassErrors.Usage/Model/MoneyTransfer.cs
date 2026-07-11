using System.Diagnostics.CodeAnalysis;

namespace FirstClassErrors.Usage.Model;

/// <summary>
///     Represents a money transfer between two accounts.
/// </summary>
/// <remarks>
///     A minimal, intentionally simplified domain concept used only to anchor the money-transfer error examples.
/// </remarks>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
                 Justification = "An intentional minimal marker: it only anchors the money-transfer error examples through [ProvidesErrorsFor(nameof(...))] and carries no behaviour of its own.")]
public sealed class MoneyTransfer { }
