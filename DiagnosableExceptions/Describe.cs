#region Usings declarations

using System.Diagnostics;
using System.Reflection;

#endregion

namespace Reefact.DiagnosableExceptions;

public static class Describe {

    #region Static members

    public static IErrorTitleStage Error(string factoryMethodName) {
        if (factoryMethodName is null) { throw new ArgumentNullException(nameof(factoryMethodName)); }

        Type exceptionType = GetExceptionType();

        return new ErrorDocumentationBuilder(new ErrorDocumentation(exceptionType, factoryMethodName));
    }

    private static Type GetExceptionType() {
        StackFrame  stackFrame = new(2);
        MethodBase? method     = stackFrame.GetMethod();

        return method!.DeclaringType!;
    }

    #endregion

}