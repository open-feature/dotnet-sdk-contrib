using System;

namespace OpenFeature.Providers.Flagd.Resolver.File;

/// <summary>
/// Watches a single flag definition file for changes and raises <see cref="FileChanged"/>
/// when the underlying file content is modified.
/// </summary>
internal interface IFileWatcher : IDisposable
{
    /// <summary>
    /// Raised when a change to the watched file is detected.
    /// </summary>
    event EventHandler<FileChangedEventArgs> FileChanged;

    /// <summary>
    /// Starts watching the file.
    /// </summary>
    void Start();
}
