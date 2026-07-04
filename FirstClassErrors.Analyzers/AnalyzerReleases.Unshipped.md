; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
<<<<<<< b57606bb883403c1b1a6d14247e5e03a167aa892
<<<<<<< c001b978e3d2604b56a4606cebebae66a2042953
=======
>>>>>>> 8229f61d518a97b78334466dffa438b4a7426c3d

### New Rules

Rule ID | Category | Severity | Notes
<<<<<<< b99d792aab6c528239b0e92feef667084d9dea0a
<<<<<<< b57606bb883403c1b1a6d14247e5e03a167aa892
--------|--------------------------------------|----------|-------------------------------------
FCE001  | FirstClassErrors.ErrorCodes          | Error    | DuplicateErrorCodeAnalyzer
FCE002  | FirstClassErrors.ErrorCodes          | Error    | EmptyErrorCodeAnalyzer
FCE003  | FirstClassErrors.ErrorCodes          | Info     | NonLiteralErrorCodeAnalyzer (disabled by default)
FCE006  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByTargetNotFoundAnalyzer
FCE007  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByInvalidSignatureAnalyzer
<<<<<<< 993934e7f8182457844cd4b01cded521601592e6
<<<<<<< 76f96d9b547b96b7d8a14048a2f6036c69baf88d
FCE008  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByWithoutProvidesErrorsForAnalyzer
<<<<<<< 5494378d3d4e079a3405b6b07d3c49987a45b79c
<<<<<<< 2cd7e88849d2856333ec71c2146d75e038de3f35
=======
>>>>>>> db7c3cb49e9b8480f5aa6d958fabbebeed5d58ac
=======
--------|-----------------------------|----------|-------------------------
FCE002  | FirstClassErrors.ErrorCodes | Error    | EmptyErrorCodeAnalyzer
>>>>>>> 8229f61d518a97b78334466dffa438b4a7426c3d
=======
--------|--------------------------------------|----------|-------------------------------------
FCE002  | FirstClassErrors.ErrorCodes          | Error    | EmptyErrorCodeAnalyzer
FCE006  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByTargetNotFoundAnalyzer
>>>>>>> 25f33ab91f85a53fce3220cf3b67a341780288de
=======
>>>>>>> 42551f455f05a68042840626428e601821a47626
=======
FCE008  | FirstClassErrors.DocumentationWiring | Error    | DocumentedByWithoutProvidesErrorsForAnalyzer
>>>>>>> 1110bd90366a90aca8cc009aea4091673f8c9dee
=======
=======
FCE009  | FirstClassErrors.DocumentationWiring | Warning  | ErrorFactoryNotDocumentedAnalyzer
<<<<<<< 906c991f34df57e7390a0e7bf1e8bfc00c89a922
>>>>>>> e8ea977457cba6404369a7eb5c5530147d4ee52d
=======
FCE010  | FirstClassErrors.DocumentationWiring | Warning  | MultipleFactoriesShareDocumentationAnalyzer
<<<<<<< 919b0936d2ff932e1a282dde842a3dcb52e58a3b
<<<<<<< b2642695b7e235070c2ef66b55d1aedc4dc5159e
>>>>>>> ee4e6faead546d60fd6995f2a279ccc7c2bf7934
=======
=======
FCE011  | FirstClassErrors.DocumentationContent| Error    | DuplicateDocumentedCodeAnalyzer
>>>>>>> fe13baf310cee0ef8a50260c9618a900c9c052c8
FCE012  | FirstClassErrors.DocumentationContent| Warning  | EmptyExamplesAnalyzer
<<<<<<< 8b1cd7af60fadf279bc7a6db4401d132a5b4cda7
>>>>>>> fa43e3873d390a51a58a51b6e9e5119f10aae02a
=======
FCE013  | FirstClassErrors.DocumentationContent| Warning  | ExampleDoesNotCallDocumentedFactoryAnalyzer
>>>>>>> 6b9bac398957700cc935135cb9decc007d4cdf60
FCE016  | FirstClassErrors.Usage               | Warning  | UnusedToExceptionResultAnalyzer
>>>>>>> e82f3b87253ce42f9e0497e6d8a41054cd608b17
