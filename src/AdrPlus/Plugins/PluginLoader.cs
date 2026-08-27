// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Abstractions;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.Formatting;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Reason a candidate plugin subfolder was rejected during structural load validation.
    /// </summary>
    internal enum PluginRejectionReason
    {
        ManifestInvalid,
        EntryAssemblyPathTraversal,
        NotInAllowlist,
        DuplicateName,
        EntryTypeIncompatible,
        AbstractionsVersionIncompatible
    }

    /// <summary>
    /// A candidate plugin subfolder that failed structural validation; never derails the host (fail-soft by design).
    /// </summary>
    /// <param name="FolderPath">The plugin's subfolder under <c>./plugins</c>.</param>
    /// <param name="Reason">The category of rejection.</param>
    /// <param name="Message">A localized, human-readable explanation.</param>
    internal sealed record PluginRejection(string FolderPath, PluginRejectionReason Reason, string Message);

    /// <summary>
    /// A plugin that passed structural load validation, ready for dispatch.
    /// </summary>
    /// <param name="Instance">The loaded plugin instance.</param>
    /// <param name="Manifest">The plugin's parsed <c>plugin.json</c> manifest.</param>
    /// <param name="FolderPath">The plugin's subfolder under <c>./plugins</c>.</param>
    /// <param name="LoadContext">
    /// The isolated <see cref="AssemblyLoadContext"/> this plugin's assembly was loaded into, so it can
    /// be unloaded on graceful shutdown. <see langword="null"/> for plugins seeded directly in tests rather than
    /// loaded via <see cref="PluginLoader.LoadAssembly"/> — there is no real ALC to unload in that case.
    /// </param>
    internal sealed record LoadedPlugin(IAdrPlugin Instance, PluginManifest Manifest, string FolderPath, AssemblyLoadContext? LoadContext = null);

    /// <summary>
    /// The outcome of validating and loading a single plugin subfolder: either a <see cref="LoadedPlugin"/>
    /// or a <see cref="PluginRejection"/>, never both.
    /// </summary>
    internal sealed class PluginLoadOutcome
    {
        public LoadedPlugin? Loaded { get; }
        public PluginRejection? Rejection { get; }

        private PluginLoadOutcome(LoadedPlugin? loaded, PluginRejection? rejection)
        {
            Loaded = loaded;
            Rejection = rejection;
        }

        public static PluginLoadOutcome Success(LoadedPlugin loaded) => new(loaded, null);

        public static PluginLoadOutcome Failure(PluginRejection rejection) => new(null, rejection);
    }

    /// <summary>
    /// Outcome of validating a candidate plugin's <c>plugin.json</c> manifest (schema, path-traversal guard,
    /// allowlist) — the checks that can be decided for one candidate in isolation, before duplicate names across
    /// candidates are known.
    /// </summary>
    internal sealed class ManifestValidationOutcome
    {
        public PluginManifest? Manifest { get; }
        public PluginRejection? Rejection { get; }

        private ManifestValidationOutcome(PluginManifest? manifest, PluginRejection? rejection)
        {
            Manifest = manifest;
            Rejection = rejection;
        }

        public static ManifestValidationOutcome Success(PluginManifest manifest) => new(manifest, null);

        public static ManifestValidationOutcome Failure(PluginRejection rejection) => new(null, rejection);
    }

    /// <summary>
    /// Validates and loads a single plugin subfolder, applying structural checks in two stages:
    /// <see cref="ValidateManifestAsync"/> covers manifest schema, <c>entryAssembly</c> path-traversal guard,
    /// and allowlist — everything decidable for one candidate alone. Duplicate-name detection across candidates
    /// (both are rejected, never just the second one found) is the caller's responsibility, run
    /// after every candidate has been through <see cref="ValidateManifestAsync"/>; only names that turn out unique
    /// should proceed to <see cref="LoadAssembly"/>, which validates <c>entryType</c>/<c>Name</c>/<c>Version</c>/
    /// <c>abstractionsVersion</c> (requiring the actual assembly to be loaded).
    /// </summary>
    internal sealed class PluginLoader(IFileSystemService fileSystem)
    {
        private const string ManifestFileName = "plugin.json";

        private readonly IFileSystemService _fileSystem = fileSystem;

        /// <summary>
        /// Validates the manifest in <paramref name="folderPath"/>: schema, <c>entryAssembly</c> path-traversal
        /// guard, and allowlist. Does not detect duplicate names (the caller compares across all candidates'
        /// manifests first) and does not load the assembly.
        /// </summary>
        /// <param name="folderPath">The plugin's subfolder path.</param>
        /// <param name="allowlist">
        /// The configured plugin allowlist (<see cref="AdrPlusConfig.PluginAllowlist"/>), or <see langword="null"/>
        /// if the allowlist is disabled (all plugins load).
        /// </param>
        /// <param name="onHashNotEnforcedWarning">
        /// Invoked with the plugin name when the matching allowlist entry has a non-empty <c>hash</c> — accepted,
        /// but the host does not enforce it yet (v1).
        /// </param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        public async Task<ManifestValidationOutcome> ValidateManifestAsync(
            string folderPath,
            List<PluginAllowlistEntry>? allowlist,
            Action<string> onHashNotEnforcedWarning,
            CancellationToken cancellationToken = default)
        {
            var manifestPath = Path.Combine(folderPath, ManifestFileName);

            PluginManifest? manifest;
            try
            {
                var manifestJson = await _fileSystem.ReadAllTextAsync(manifestPath, cancellationToken);
                manifest = JsonSerializer.Deserialize<PluginManifest>(manifestJson, PluginManifest.SerializerOptions);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                return ManifestValidationOutcome.Failure(RejectManifestInvalid(folderPath, ex.Message));
            }

            if (manifest is null || !HasRequiredFields(manifest))
            {
                return ManifestValidationOutcome.Failure(RejectManifestInvalid(folderPath, "manifest is missing one or more required fields (name, version, entryAssembly, entryType, abstractionsVersion, subscribedEvents)"));
            }

            if (ContainsPathTraversal(manifest.EntryAssembly!))
            {
                var message = string.Format(null, FormatMessages.PluginRejectedEntryAssemblyPathTraversal, folderPath, manifest.EntryAssembly!);
                return ManifestValidationOutcome.Failure(new PluginRejection(folderPath, PluginRejectionReason.EntryAssemblyPathTraversal, message));
            }

            if (allowlist != null)
            {
                var allowlistEntry = allowlist.Find(entry => string.Equals(entry.Name, manifest.Name, StringComparison.OrdinalIgnoreCase));
                if (allowlistEntry is null)
                {
                    var message = string.Format(null, FormatMessages.PluginRejectedNotInAllowlist, manifest.Name!);
                    return ManifestValidationOutcome.Failure(new PluginRejection(folderPath, PluginRejectionReason.NotInAllowlist, message));
                }

                if (!string.IsNullOrWhiteSpace(allowlistEntry.Hash))
                {
                    onHashNotEnforcedWarning(manifest.Name!);
                }
            }

            return ManifestValidationOutcome.Success(manifest);
        }

        /// <summary>
        /// Rejects the plugin in <paramref name="folderPath"/> as a duplicate: another candidate declared the
        /// same <see cref="PluginManifest.Name"/> (case-insensitive). Every plugin sharing a duplicated
        /// name is rejected — never just the second one found.
        /// </summary>
        public static PluginRejection RejectDuplicateName(string folderPath, string pluginName)
        {
            var message = string.Format(null, FormatMessages.PluginRejectedDuplicateName, pluginName, folderPath);
            return new PluginRejection(folderPath, PluginRejectionReason.DuplicateName, message);
        }

        /// <summary>
        /// Loads the plugin's assembly and validates <c>entryType</c>/<c>Name</c>/<c>Version</c>/
        /// <c>abstractionsVersion</c> against <paramref name="manifest"/>. Only call this once
        /// <paramref name="manifest"/>'s name is known not to collide with any other candidate's.
        /// </summary>
        /// <param name="folderPath">The plugin's subfolder path.</param>
        /// <param name="manifest">The already-validated manifest for this folder.</param>
        public static PluginLoadOutcome LoadAssembly(string folderPath, PluginManifest manifest)
        {
            var assemblyPath = Path.GetFullPath(Path.Combine(folderPath, manifest.EntryAssembly!));

            // The physical assembly load is inherent to AssemblyLoadContext/AssemblyDependencyResolver and
            // bypasses IFileSystemService — this is the one step that cannot go through that abstraction.
            if (!File.Exists(assemblyPath))
            {
                return PluginLoadOutcome.Failure(RejectEntryTypeIncompatible(folderPath, manifest.Name!, manifest.EntryType!));
            }

            IAdrPlugin instance;
            PluginAssemblyLoadContext loadContext;
            try
            {
                loadContext = new PluginAssemblyLoadContext(assemblyPath);
                var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                var entryType = assembly.GetType(manifest.EntryType!, throwOnError: false);

                if (entryType is null || !typeof(IAdrPlugin).IsAssignableFrom(entryType))
                {
                    return PluginLoadOutcome.Failure(RejectEntryTypeIncompatible(folderPath, manifest.Name!, manifest.EntryType!));
                }

                instance = (IAdrPlugin)Activator.CreateInstance(entryType)!;

                if (!string.Equals(instance.Name, manifest.Name, StringComparison.OrdinalIgnoreCase)
                    || instance.Version != manifest.Version)
                {
                    return PluginLoadOutcome.Failure(RejectEntryTypeIncompatible(folderPath, manifest.Name!, manifest.EntryType!));
                }

                if (!IsAbstractionsVersionCompatible(manifest.AbstractionsVersion!))
                {
                    return PluginLoadOutcome.Failure(RejectAbstractionsVersionIncompatible(folderPath, manifest.Name!, manifest.AbstractionsVersion!));
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or BadImageFormatException or ReflectionTypeLoadException or TargetInvocationException or MissingMethodException)
            {
                return PluginLoadOutcome.Failure(RejectEntryTypeIncompatible(folderPath, manifest.Name!, manifest.EntryType!));
            }

            return PluginLoadOutcome.Success(new LoadedPlugin(instance, manifest, folderPath, loadContext));
        }

        private static bool HasRequiredFields(PluginManifest manifest) =>
            !string.IsNullOrWhiteSpace(manifest.Name)
            && !string.IsNullOrWhiteSpace(manifest.Version)
            && !string.IsNullOrWhiteSpace(manifest.EntryAssembly)
            && !string.IsNullOrWhiteSpace(manifest.EntryType)
            && !string.IsNullOrWhiteSpace(manifest.AbstractionsVersion)
            && manifest.SubscribedEvents != null;

        private static bool ContainsPathTraversal(string entryAssembly) =>
            entryAssembly.Contains('/') || entryAssembly.Contains('\\') || entryAssembly.Contains("..", StringComparison.Ordinal);

        private static bool IsAbstractionsVersionCompatible(string abstractionsVersion)
        {
            if (!Version.TryParse(abstractionsVersion, out var pluginVersion))
            {
                return false;
            }

            var hostVersion = typeof(IAdrPlugin).Assembly.GetName().Version;
            return hostVersion != null && pluginVersion.Major == hostVersion.Major;
        }

        private static PluginRejection RejectManifestInvalid(string folderPath, string reasonDetail)
        {
            var message = string.Format(null, FormatMessages.PluginRejectedManifestInvalid, folderPath, reasonDetail);
            return new PluginRejection(folderPath, PluginRejectionReason.ManifestInvalid, message);
        }

        private static PluginRejection RejectEntryTypeIncompatible(string folderPath, string pluginName, string entryType)
        {
            var message = string.Format(null, FormatMessages.PluginRejectedEntryTypeIncompatible, pluginName, entryType);
            return new PluginRejection(folderPath, PluginRejectionReason.EntryTypeIncompatible, message);
        }

        private static PluginRejection RejectAbstractionsVersionIncompatible(string folderPath, string pluginName, string abstractionsVersion)
        {
            var hostMajor = typeof(IAdrPlugin).Assembly.GetName().Version?.Major ?? 0;
            var message = string.Format(null, FormatMessages.PluginRejectedAbstractionsVersionIncompatible, pluginName, abstractionsVersion, hostMajor);
            return new PluginRejection(folderPath, PluginRejectionReason.AbstractionsVersionIncompatible, message);
        }
    }

    /// <summary>
    /// One isolated <see cref="AssemblyLoadContext"/> per plugin subfolder, with private dependencies
    /// resolved via <see cref="AssemblyDependencyResolver"/> over the plugin's <c>.deps.json</c>.
    /// <see cref="AdrPlus.Abstractions"/> types are resolved by the host's default context.
    /// </summary>
    internal sealed class PluginAssemblyLoadContext(string pluginAssemblyPath) : AssemblyLoadContext(isCollectible: true)
    {
        private readonly AssemblyDependencyResolver _resolver = new(pluginAssemblyPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }
    }
}
