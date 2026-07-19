namespace FirstClassErrors.RequestBinder;

/// <summary>
///     Builds the indexed argument path of one list element (<c>Tags[2]</c>) on demand. Implemented by the list
///     converters and passed (as <c>this</c> — no allocation) into the shared element iteration and into each
///     element's nested binding, so an element path — and the list path it is built from — only materializes when
///     a path is first needed: a failing element, or, inside a complex element, a nested prefix or an
///     <c>Argument</c> name. A list of simple properties whose every element binds never builds a single path
///     string.
/// </summary>
internal interface IElementPathSource {

    /// <summary>The full indexed path of the element at <paramref name="index" /> (list path + <c>[index]</c>).</summary>
    /// <param name="index">The element's zero-based position.</param>
    /// <returns>The element's argument path.</returns>
    string ElementPathAt(int index);

}
