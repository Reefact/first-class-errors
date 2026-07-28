namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Uri" /> generator drawing from the ambient random context.
    ///     Unconstrained, it yields any valid URI from the safe space — an absolute web (<c>http</c>/<c>https</c>),
    ///     WebSocket (<c>ws</c>/<c>wss</c>), FTP or mailto URI, or a relative reference. Narrow it to a family
    ///     (<c>Web()</c>, <c>WebSocket()</c>, <c>Ftp()</c>, <c>Mailto()</c>, <c>Relative()</c>) to reach that family's
    ///     component constraints; each narrowing returns a builder exposing only that family's valid components.
    /// </summary>
    /// <returns>A URI generator to narrow fluently.</returns>
    public static AnyUri Uri() {
        return new AnyUri(AmbientRandomSource.Instance, UriSpec.Unconstrained);
    }

}
