#region Usings declarations

using Microsoft.CodeAnalysis;

#endregion

namespace Reefact.DiagnosableExceptions.Analyzer {

    internal static class DiagnosticDescriptors {

        #region Statics members declarations

        public static readonly DiagnosticDescriptor FactoryMustBeCalledByOwner =
            new DiagnosticDescriptor(
                "RDEA001",
                "Exception factory must be called only by its owner type",
                "The exception factory '{0}' belongs to '{1}', but is called from '{2}'",
                "Architecture",
                DiagnosticSeverity.Error,
                true);

        #endregion

    }

}