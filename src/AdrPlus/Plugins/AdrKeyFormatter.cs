// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Plugins
{
    /// <summary>
    /// Formats the stable <c>adrKey</c> identity used across <c>pending.json</c> entries (spec §7):
    /// <c>"{Number:D4}-v{Version}-r{Revision??0}"</c> (e.g. <c>"0007-v1-r0"</c>). Shared by
    /// <see cref="PluginManager"/> (building the key from an <c>AdrRecordSnapshot</c>) and
    /// <c>SyncCommandHandler</c> (building the same key from an <c>AdrFileNameComponents</c>) so the
    /// format only exists in one place.
    /// </summary>
    internal static class AdrKeyFormatter
    {
        internal static string Format(int number, int version, int? revision) => $"{number:D4}-v{version}-r{revision ?? 0}";
    }
}
