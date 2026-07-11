using System.Diagnostics.CodeAnalysis;

namespace FirstClassErrors.Usage.Infrastructure.Adapters;

/// <summary>
///     Validates bank transaction files to ensure compliance with predefined business rules and data integrity
///     requirements.
/// </summary>
/// <remarks>
///     This class is designed to detect and report issues in bank transaction files, such as transactions occurring
///     outside the statement period.
/// </remarks>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
                 Justification = "An intentional minimal marker: it only anchors the bank-transaction-file error examples through [ProvidesErrorsFor(nameof(...))] and carries no behaviour of its own.")]
public sealed class BankTransactionFileValidator { }