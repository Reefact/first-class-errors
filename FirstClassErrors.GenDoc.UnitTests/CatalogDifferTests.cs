#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace FirstClassErrors.GenDoc.Versioning.UnitTests;

[TestSubject(typeof(CatalogDiffer))]
public sealed class CatalogDifferTests {

    private static CatalogSnapshot Snapshot(params CatalogSnapshotEntry[] entries) {
        return new CatalogSnapshot { Errors = entries };
    }

    private static CatalogSnapshotEntry Entry(string code, string? title = null, string? source = null, params CatalogSnapshotContextKey[] context) {
        return new CatalogSnapshotEntry { Code = code, Title = title, Source = source, Context = context };
    }

    private static CatalogSnapshotContextKey Key(string key, string valueType) {
        return new CatalogSnapshotContextKey { Key = key, ValueType = valueType };
    }

    [Fact(DisplayName = "Two identical snapshots produce an empty diff.")]
    public void TwoIdenticalSnapshotsProduceAnEmptyDiff() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("PAYMENT.DECLINED", "Payment declined", "Payment", Key("DealId", "System.String")));
        CatalogSnapshot current  = Snapshot(Entry("PAYMENT.DECLINED", "Payment declined", "Payment", Key("DealId", "System.String")));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        Check.That(diff.IsEmpty).IsTrue();
        Check.That(diff.HasChangesAtOrAbove(CatalogChangeImpact.Informational)).IsFalse();
    }

    [Fact(DisplayName = "A removed error code is a breaking change.")]
    public void ARemovedErrorCodeIsABreakingChange() {
        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(Snapshot(Entry("PAYMENT.DECLINED")), Snapshot());

        // Verify
        Check.That(diff.BreakingChanges).HasSize(1);
        CatalogChange change = diff.BreakingChanges[0];
        Check.That(change.Kind).IsEqualTo(CatalogChangeKind.ErrorRemoved);
        Check.That(change.Code).IsEqualTo("PAYMENT.DECLINED");
    }

    [Fact(DisplayName = "A removal whose title matches a single added error carries a 'possibly renamed' hint.")]
    public void ARemovalMatchingASingleAdditionCarriesARenameHint() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("PAYMENT.DECLINED", "Payment declined"));
        CatalogSnapshot current  = Snapshot(Entry("PAYMENT.REFUSED", "Payment declined"));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify: the removal stays breaking — consumers know the old code — but the hint points at the new one.
        CatalogChange removal = diff.Changes.Single(change => change.Kind == CatalogChangeKind.ErrorRemoved);
        Check.That(removal.Impact).IsEqualTo(CatalogChangeImpact.Breaking);
        Check.That(removal.Description).Contains("possibly renamed to 'PAYMENT.REFUSED'");
    }

    [Fact(DisplayName = "No rename hint is emitted when several added errors share the removed error's title.")]
    public void NoRenameHintIsEmittedWhenSeveralAdditionsShareTheTitle() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("OLD", "Same title"));
        CatalogSnapshot current  = Snapshot(Entry("NEW_1", "Same title"), Entry("NEW_2", "Same title"));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        CatalogChange removal = diff.Changes.Single(change => change.Kind == CatalogChangeKind.ErrorRemoved);
        Check.That(removal.Description).Not.Contains("possibly renamed");
    }

    [Fact(DisplayName = "A new error code is a compatible change.")]
    public void ANewErrorCodeIsACompatibleChange() {
        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(Snapshot(), Snapshot(Entry("INVENTORY.OUT_OF_STOCK", "Out of stock", "Inventory")));

        // Verify
        Check.That(diff.CompatibleChanges).HasSize(1);
        CatalogChange change = diff.CompatibleChanges[0];
        Check.That(change.Kind).IsEqualTo(CatalogChangeKind.ErrorAdded);
        Check.That(change.Description).Contains("Out of stock").And.Contains("Inventory");
    }

    [Fact(DisplayName = "A removed context key is a breaking change.")]
    public void ARemovedContextKeyIsABreakingChange() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("SOME_CODE", null, null, Key("DealId", "System.String")));
        CatalogSnapshot current  = Snapshot(Entry("SOME_CODE"));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        CatalogChange change = diff.Changes.Single();
        Check.That(change.Kind).IsEqualTo(CatalogChangeKind.ContextKeyRemoved);
        Check.That(change.Impact).IsEqualTo(CatalogChangeImpact.Breaking);
        Check.That(change.Description).Contains("DealId");
    }

    [Fact(DisplayName = "A context key changing its value type is a breaking change.")]
    public void AContextKeyChangingItsValueTypeIsABreakingChange() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("SOME_CODE", null, null, Key("DealId", "System.String")));
        CatalogSnapshot current  = Snapshot(Entry("SOME_CODE", null, null, Key("DealId", "System.Guid")));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        CatalogChange change = diff.Changes.Single();
        Check.That(change.Kind).IsEqualTo(CatalogChangeKind.ContextKeyValueTypeChanged);
        Check.That(change.Impact).IsEqualTo(CatalogChangeImpact.Breaking);
        Check.That(change.Description).Contains("System.String").And.Contains("System.Guid");
    }

    [Fact(DisplayName = "A new context key is a compatible change.")]
    public void ANewContextKeyIsACompatibleChange() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("SOME_CODE"));
        CatalogSnapshot current  = Snapshot(Entry("SOME_CODE", null, null, Key("DealId", "System.String")));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        CatalogChange change = diff.Changes.Single();
        Check.That(change.Kind).IsEqualTo(CatalogChangeKind.ContextKeyAdded);
        Check.That(change.Impact).IsEqualTo(CatalogChangeImpact.Compatible);
    }

    [Fact(DisplayName = "Title and source changes are informational.")]
    public void TitleAndSourceChangesAreInformational() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("SOME_CODE", "Old title", "OldSource"));
        CatalogSnapshot current  = Snapshot(Entry("SOME_CODE", "New title", "NewSource"));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        Check.That(diff.InformationalChanges).HasSize(2);
        Check.That(diff.Changes.Select(change => change.Kind))
             .Contains(CatalogChangeKind.TitleChanged, CatalogChangeKind.SourceChanged);
        Check.That(diff.HasChangesAtOrAbove(CatalogChangeImpact.Breaking)).IsFalse();
        Check.That(diff.HasChangesAtOrAbove(CatalogChangeImpact.Informational)).IsTrue();
    }

    [Fact(DisplayName = "The impact threshold treats compatible changes as above informational and below breaking.")]
    public void TheImpactThresholdRanksCompatibleBetweenInformationalAndBreaking() {
        // Setup: one compatible change only.
        CatalogDiff diff = CatalogDiffer.Diff(Snapshot(), Snapshot(Entry("SOME_CODE")));

        // Exercise & verify
        Check.That(diff.HasChangesAtOrAbove(CatalogChangeImpact.Breaking)).IsFalse();
        Check.That(diff.HasChangesAtOrAbove(CatalogChangeImpact.Compatible)).IsTrue();
        Check.That(diff.HasChangesAtOrAbove(CatalogChangeImpact.Informational)).IsTrue();
    }

    [Fact(DisplayName = "Changes are ordered by error code, whatever the snapshot order.")]
    public void ChangesAreOrderedByErrorCode() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("Z_CODE"), Entry("A_CODE"));
        CatalogSnapshot current  = Snapshot();

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        Check.That(diff.Changes.Select(change => change.Code)).ContainsExactly("A_CODE", "Z_CODE");
    }

    [Fact(DisplayName = "Identities and titles are compared normalized: whitespace-only differences produce no change.")]
    public void WhitespaceOnlyDifferencesProduceNoChange() {
        // Setup: same contract, but every identity and the title differ only by surrounding whitespace.
        CatalogSnapshot baseline = Snapshot(Entry("PAYMENT.DECLINED", "Payment declined", "Payment", Key("DealId", "System.String")));
        CatalogSnapshot current  = Snapshot(Entry("  PAYMENT.DECLINED  ", "  Payment declined  ", "  Payment  ", Key("  DealId  ", "System.String")));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        Check.That(diff.IsEmpty).IsTrue();
    }

    [Fact(DisplayName = "The rename hint matches titles that differ only by whitespace.")]
    public void TheRenameHintMatchesTitlesThatDifferOnlyByWhitespace() {
        // Setup
        CatalogSnapshot baseline = Snapshot(Entry("PAYMENT.DECLINED", "Payment declined"));
        CatalogSnapshot current  = Snapshot(Entry("PAYMENT.REFUSED", "  Payment declined  "));

        // Exercise
        CatalogDiff diff = CatalogDiffer.Diff(baseline, current);

        // Verify
        CatalogChange removal = diff.Changes.Single(change => change.Kind == CatalogChangeKind.ErrorRemoved);
        Check.That(removal.Description).Contains("possibly renamed to 'PAYMENT.REFUSED'");
    }

    [Fact(DisplayName = "Null snapshots are rejected.")]
    public void NullSnapshotsAreRejected() {
        // Exercise & verify
        Check.ThatCode(() => CatalogDiffer.Diff(null!, Snapshot())).Throws<ArgumentNullException>();
        Check.ThatCode(() => CatalogDiffer.Diff(Snapshot(), null!)).Throws<ArgumentNullException>();
    }

}
