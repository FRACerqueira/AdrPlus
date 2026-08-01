// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using AdrPlus.Infrastructure.Logging;
using AdrPlus.Infrastructure.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Default <see cref="IPluginManager"/> implementation. Discovers plugin subfolders via
    /// <see cref="IFileSystemService.GetDirectories"/>, validates each with a <see cref="PluginLoader"/>, and
    /// reads the plugin allowlist from <see cref="AdrPlusConfig.PluginAllowlist"/>. Never throws for a
    /// rejected plugin — fail-soft, consistent with D30.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PluginManager"/> class.
    /// </remarks>
    /// <param name="fileSystem">The file system service used to discover plugin subfolders and read manifests.</param>
    /// <param name="config">The application configuration, providing the optional plugin allowlist.</param>
    /// <param name="logger">The logger for recording plugin rejections and warnings.</param>
    /// <param name="prompt">The console writer for surfacing plugin rejections and warnings to the user.</param>
    internal sealed class PluginManager(
        IFileSystemService fileSystem,
        IOptions<AdrPlusConfig> config,
        ILogger<PluginManager> logger,
        IConsoleWriter prompt) : IPluginManager
    {
        private readonly IFileSystemService _fileSystem = fileSystem;
        private readonly AdrPlusConfig _config = config.Value;
        private readonly ILogger<PluginManager> _logger = logger;
        private readonly IConsoleWriter _prompt = prompt;
        private readonly List<LoadedPlugin> _loadedPlugins = [];
        private readonly List<PluginRejection> _rejections = [];

        /// <inheritdoc/>
        public IReadOnlyList<LoadedPlugin> LoadedPlugins => _loadedPlugins;

        /// <inheritdoc/>
        public IReadOnlyList<PluginRejection> Rejections => _rejections;

        /// <inheritdoc/>
        public async Task LoadPluginsAsync(string pluginsRootPath, CancellationToken cancellationToken = default)
        {
            _loadedPlugins.Clear();
            _rejections.Clear();

            if (!_fileSystem.DirectoryExists(pluginsRootPath))
            {
                return;
            }

            var loader = new PluginLoader(_fileSystem);

            // Stage 1: validate every candidate's manifest in isolation (schema, path-traversal guard, allowlist).
            // Duplicate names can only be known once every candidate has reached this point.
            var candidates = new List<(string FolderPath, PluginManifest Manifest)>();

            foreach (var folderPath in _fileSystem.GetDirectories(pluginsRootPath).OrderBy(path => path, StringComparer.Ordinal))
            {
                var outcome = await loader.ValidateManifestAsync(
                    folderPath,
                    _config.PluginAllowlist,
                    pluginName => WriteWarning(string.Format(null, FormatMessages.PluginAllowlistHashNotEnforced, pluginName)),
                    cancellationToken);

                if (outcome.Manifest is { } manifest)
                {
                    candidates.Add((folderPath, manifest));
                }
                else if (outcome.Rejection is { } rejection)
                {
                    _rejections.Add(rejection);
                    WriteWarning(rejection.Message);
                }
            }

            // Stage 2: group by name (D22, case-insensitive) — every candidate in a group with more than one
            // member is rejected as a duplicate; only a uniquely-named candidate proceeds to load its assembly.
            foreach (var group in candidates.GroupBy(candidate => candidate.Manifest.Name!, StringComparer.OrdinalIgnoreCase))
            {
                if (group.Count() > 1)
                {
                    foreach (var (folderPath, manifest) in group)
                    {
                        var rejection = PluginLoader.RejectDuplicateName(folderPath, manifest.Name!);
                        _rejections.Add(rejection);
                        WriteWarning(rejection.Message);
                    }
                    continue;
                }

                var (soleFolderPath, soleManifest) = group.Single();
                var outcome = PluginLoader.LoadAssembly(soleFolderPath, soleManifest);

                if (outcome.Loaded is { } loaded)
                {
                    _loadedPlugins.Add(loaded);
                }
                else if (outcome.Rejection is { } rejection)
                {
                    _rejections.Add(rejection);
                    WriteWarning(rejection.Message);
                }
            }
        }

        private void WriteWarning(string message)
        {
            LogMessages.LogPluginWarning(_logger, message);
            _prompt.PromptWriteInfo(message);
        }
    }
}
