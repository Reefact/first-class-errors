#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.Versioning.UnitTests;

[TestSubject(typeof(CatalogSnapshot))]
public sealed class CatalogSnapshotTests {

    [Fact(DisplayName = "The snapshot projects the code, title, source and context keys of every documented error.")]
    public void TheSnapshotProjectsTheContractOfEveryDocumentedError() {
        // Setup
        ErrorDocumentation documentation = new() {
            Code   = "TEMPERATURE_BELOW_ABSOLUTE_ZERO",
            Title  = "Temperature below absolute zero",
            Source = "Temperature",
            Context = new[] {
                new ErrorContextEntryDocumentation { Key = "AttemptedValue", ValueType = "System.Double" }
            }
        };

        // Exercise
        CatalogSnapshot snapshot = CatalogSnapshot.FromCatalog([documentation]);

        // Verify
        Check.That(snapshot.Schema).IsEqualTo(CatalogSnapshot.CurrentSchema);
        Check.That(snapshot.Errors).HasSize(1);
        CatalogSnapshotEntry entry = snapshot.Errors[0];
        Check.That(entry.Code).IsEqualTo("TEMPERATURE_BELOW_ABSOLUTE_ZERO");
        Check.That(entry.Title).IsEqualTo("Temperature below absolute zero");
        Check.That(entry.Source).IsEqualTo("Temperature");
        Check.That(entry.Context).HasSize(1);
        Check.That(entry.Context[0].Key).IsEqualTo("AttemptedValue");
        Check.That(entry.Context[0].ValueType).IsEqualTo("System.Double");
    }

    [Fact(DisplayName = "The snapshot orders errors by code and context keys by name, whatever the input order.")]
    public void TheSnapshotOrdersErrorsAndContextKeysDeterministically() {
        // Setup
        ErrorDocumentation second = new() { Code = "B_CODE" };
        ErrorDocumentation first = new() {
            Code = "A_CODE",
            Context = new[] {
                new ErrorContextEntryDocumentation { Key = "Zulu",  ValueType = "System.String" },
                new ErrorContextEntryDocumentation { Key = "Alpha", ValueType = "System.Int32" }
            }
        };

        // Exercise
        CatalogSnapshot snapshot = CatalogSnapshot.FromCatalog([second, first]);

        // Verify
        Check.That(snapshot.Errors.Select(entry => entry.Code)).ContainsExactly("A_CODE", "B_CODE");
        Check.That(snapshot.Errors[0].Context.Select(key => key.Key)).ContainsExactly("Alpha", "Zulu");
    }

    [Fact(DisplayName = "An entry without a code is skipped: it has no identity to track.")]
    public void AnEntryWithoutACodeIsSkipped() {
        // Setup
        ErrorDocumentation uncoded = new() { Title = "Orphan documentation" };
        ErrorDocumentation coded   = new() { Code  = "SOME_CODE" };

        // Exercise
        CatalogSnapshot snapshot = CatalogSnapshot.FromCatalog([uncoded, coded]);

        // Verify
        Check.That(snapshot.Errors).HasSize(1);
        Check.That(snapshot.Errors[0].Code).IsEqualTo("SOME_CODE");
    }

    [Fact(DisplayName = "Duplicate context key names collapse to a single tracked key.")]
    public void DuplicateContextKeyNamesCollapseToASingleTrackedKey() {
        // Setup
        ErrorDocumentation documentation = new() {
            Code = "SOME_CODE",
            Context = new[] {
                new ErrorContextEntryDocumentation { Key = "DealId", ValueType = "System.String" },
                new ErrorContextEntryDocumentation { Key = "DealId", ValueType = "System.Guid" }
            }
        };

        // Exercise
        CatalogSnapshot snapshot = CatalogSnapshot.FromCatalog([documentation]);

        // Verify: the first declaration wins, deterministically.
        Check.That(snapshot.Errors[0].Context).HasSize(1);
        Check.That(snapshot.Errors[0].Context[0].ValueType).IsEqualTo("System.String");
    }

    [Fact(DisplayName = "Two entries sharing a code collapse to one, keeping the first in code order.")]
    public void TwoEntriesSharingACodeCollapseToTheFirstInCodeOrder() {
        // Setup
        ErrorDocumentation first  = new() { Code = "SHARED", Title = "First", Source = "A" };
        ErrorDocumentation second = new() { Code = "SHARED", Title = "Second", Source = "B" };

        // Exercise
        CatalogSnapshot snapshot = CatalogSnapshot.FromCatalog([second, first]);

        // Verify: a single entry survives, deterministically the first one.
        Check.That(snapshot.Errors).HasSize(1);
        Check.That(snapshot.Errors[0].Title).IsEqualTo("First");
        Check.That(snapshot.Errors[0].Source).IsEqualTo("A");
    }

    [Fact(DisplayName = "A whitespace-padded code is trimmed in the snapshot.")]
    public void AWhitespacePaddedCodeIsTrimmed() {
        // Exercise
        CatalogSnapshot snapshot = CatalogSnapshot.FromCatalog([new ErrorDocumentation { Code = "  PADDED_CODE  " }]);

        // Verify
        Check.That(snapshot.Errors[0].Code).IsEqualTo("PADDED_CODE");
    }

    [Fact(DisplayName = "A null catalog is rejected.")]
    public void ANullCatalogIsRejected() {
        // Exercise & verify
        Check.ThatCode(() => CatalogSnapshot.FromCatalog(null!)).Throws<ArgumentNullException>();
    }

}

[TestSubject(typeof(CatalogSnapshotSerializer))]
public sealed class CatalogSnapshotSerializerTests {

    private static CatalogSnapshot SampleSnapshot() {
        return CatalogSnapshot.FromCatalog([
            new ErrorDocumentation {
                Code   = "AMOUNT_CURRENCY_MISMATCH",
                Title  = "Amount currency mismatch",
                Source = "Amount",
                Context = new[] {
                    new ErrorContextEntryDocumentation { Key = "AttemptedValue", ValueType = "System.Decimal" }
                }
            }
        ]);
    }

    [Fact(DisplayName = "A snapshot serializes to camelCase JSON with \\n line endings and a single trailing newline.")]
    public void ASnapshotSerializesToItsCanonicalForm() {
        // Exercise
        string json = CatalogSnapshotSerializer.Serialize(SampleSnapshot());

        // Verify
        Check.That(json).Contains("\"schema\": 1");
        Check.That(json).Contains("\"code\": \"AMOUNT_CURRENCY_MISMATCH\"");
        Check.That(json).Not.Contains("\r");
        Check.That(json.EndsWith("}\n", StringComparison.Ordinal)).IsTrue();
    }

    [Fact(DisplayName = "Serializing the same catalog twice produces byte-identical text.")]
    public void SerializingTheSameCatalogTwiceProducesIdenticalText() {
        // Exercise & verify
        Check.That(CatalogSnapshotSerializer.Serialize(SampleSnapshot())).IsEqualTo(CatalogSnapshotSerializer.Serialize(SampleSnapshot()));
    }

    [Fact(DisplayName = "A snapshot round-trips through its JSON form, preserving every tracked field.")]
    public void ASnapshotRoundTripsThroughItsJsonForm() {
        // Setup
        CatalogSnapshot original = SampleSnapshot();

        // Exercise
        CatalogSnapshot parsed = CatalogSnapshotSerializer.Deserialize(CatalogSnapshotSerializer.Serialize(original));

        // Verify
        Check.That(parsed.Schema).IsEqualTo(original.Schema);
        Check.That(parsed.Errors).HasSize(1);
        CatalogSnapshotEntry entry = parsed.Errors[0];
        Check.That(entry.Code).IsEqualTo("AMOUNT_CURRENCY_MISMATCH");
        Check.That(entry.Title).IsEqualTo("Amount currency mismatch");
        Check.That(entry.Source).IsEqualTo("Amount");
        Check.That(entry.Context).HasSize(1);
        Check.That(entry.Context[0].Key).IsEqualTo("AttemptedValue");
        Check.That(entry.Context[0].ValueType).IsEqualTo("System.Decimal");
    }

    [Fact(DisplayName = "A document that omits 'schema' entirely is rejected, not silently accepted.")]
    public void ADocumentThatOmitsSchemaIsRejected() {
        // Exercise & verify
        Check.ThatCode(() => CatalogSnapshotSerializer.Deserialize("""{ "errors": [] }"""))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "Deserialize trims padded codes and context-key names.")]
    public void DeserializeTrimsPaddedCodesAndContextKeyNames() {
        // Exercise
        CatalogSnapshot parsed = CatalogSnapshotSerializer.Deserialize(
            """{ "schema": 1, "errors": [ { "code": "  PADDED  ", "context": [ { "key": "  DealId  ", "valueType": "System.String" } ] } ] }""");

        // Verify
        Check.That(parsed.Errors[0].Code).IsEqualTo("PADDED");
        Check.That(parsed.Errors[0].Context[0].Key).IsEqualTo("DealId");
    }

    [Fact(DisplayName = "Null arguments are rejected by both Serialize and Deserialize.")]
    public void NullArgumentsAreRejected() {
        // Exercise & verify
        Check.ThatCode(() => CatalogSnapshotSerializer.Serialize(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => CatalogSnapshotSerializer.Deserialize(null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A snapshot declaring a newer schema is rejected as a distinct CatalogSchemaTooNewException.")]
    public void ASnapshotDeclaringANewerSchemaIsRejected() {
        // Exercise
        CatalogSchemaTooNewException? caught = null;
        try {
            CatalogSnapshotSerializer.Deserialize("""{ "schema": 999, "errors": [] }""");
        } catch (CatalogSchemaTooNewException exception) {
            caught = exception;
        }

        // Verify: a distinct, catchable type carrying the versions — yet still an InvalidOperationException.
        Check.That(caught).IsNotNull();
        Check.That(caught!.DeclaredSchema).IsEqualTo(999);
        Check.That(caught.SupportedSchema).IsEqualTo(CatalogSnapshot.CurrentSchema);
        Check.That(caught is InvalidOperationException).IsTrue();
    }

    [Fact(DisplayName = "A snapshot without a valid schema version is rejected.")]
    public void ASnapshotWithoutAValidSchemaVersionIsRejected() {
        // Exercise & verify
        Check.ThatCode(() => CatalogSnapshotSerializer.Deserialize("""{ "schema": 0, "errors": [] }"""))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "An entry without a code is rejected at parse time.")]
    public void AnEntryWithoutACodeIsRejectedAtParseTime() {
        // Exercise & verify
        Check.ThatCode(() => CatalogSnapshotSerializer.Deserialize("""{ "schema": 1, "errors": [ { "title": "No code" } ] }"""))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "Invalid JSON is reported as a clear error.")]
    public void InvalidJsonIsReportedAsAClearError() {
        // Exercise & verify
        Check.ThatCode(() => CatalogSnapshotSerializer.Deserialize("not json"))
             .Throws<InvalidOperationException>();
    }

    [Fact(DisplayName = "A hand-edited null 'errors' list is normalized to an empty one.")]
    public void ANullErrorsListIsNormalizedToAnEmptyOne() {
        // Exercise
        CatalogSnapshot parsed = CatalogSnapshotSerializer.Deserialize("""{ "schema": 1, "errors": null }""");

        // Verify
        Check.That(parsed.Errors).IsNotNull();
        Check.That(parsed.Errors).IsEmpty();
    }

}
