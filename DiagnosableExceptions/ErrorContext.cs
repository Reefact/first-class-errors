namespace Reefact.DiagnosableExceptions {

    /// <summary>
    ///     Encapsulates contextual information about an error, including
    ///     its version and associated content, to aid in error diagnosis
    ///     and resolution.
    /// </summary>
    /// <remarks>
    ///     The <see cref="ErrorContext" /> class is primarily used to provide
    ///     structured details about an error. The <see cref="Version" /> property
    ///     represents the version of the <see cref="Content" />, which is particularly
    ///     useful for scenarios such as deserialization or versioned error handling.
    /// </remarks>
    public class ErrorContext {

        #region Constructors declarations

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorContext" /> class with the specified version and content.
        /// </summary>
        /// <param name="version">
        ///     The version of the error context. Must be a positive, non-zero integer.
        /// </param>
        /// <param name="content">
        ///     The content associated with the error context. Cannot be <c>null</c>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when <paramref name="version" /> is less than or equal to zero.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="content" /> is <c>null</c>.
        /// </exception>
        public ErrorContext(int version, object content) {
            if (version < 1) { throw new ArgumentOutOfRangeException(nameof(version)); }
            if (content is null) { throw new ArgumentNullException(nameof(content)); }

            Version = version;
            Content = content;
        }

        #endregion

        /// <summary>
        ///     Gets the version of the error context.
        /// </summary>
        /// <remarks>
        ///     The version is a positive, non-zero integer that represents the version
        ///     of the <see cref="Content" />. This property is particularly useful in
        ///     scenarios involving versioned error handling or deserialization, where
        ///     the version helps to interpret the associated content correctly.
        /// </remarks>
        public int Version { get; }
        /// <summary>
        ///     Gets the content associated with the error context.
        /// </summary>
        /// <remarks>
        ///     The content provides additional contextual information about the error.
        ///     It is guaranteed to be non-<c>null</c> and can be used to store any
        ///     relevant data that aids in diagnosing or resolving the error.
        /// </remarks>
        public object Content { get; }

    }

}