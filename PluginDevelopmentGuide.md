![icon](https://raw.githubusercontent.com/FRACerqueira/AdrPlus/main/icon.png)
# Plugin Development Guide

This guide is for developers writing a **plugin** for AdrPlus — code that reacts to ADR lifecycle events (created, approved, rejected, superseded, ...) to sync decisions into an external system (Confluence, Jira, Teams, a search index, whatever you need).

If you just want to *use* AdrPlus to manage ADRs, see the [Step-by-Step Guide](StepByStepGuide.md) instead. If you want a working example to read alongside this guide, every new repository already ships with one: `AdrPlus.Plugins.AdrIndexer` (bundled with the tool, auto-installed by `adrplus init` into `./plugins/adr-indexer/`) — its source lives at `src/AdrPlus.Plugins.AdrIndexer/AdrIndexerPlugin.cs` in the AdrPlus repository.

---

## Table of Contents

1. [How the plugin system works](#how-the-plugin-system-works)
2. [Project setup](#project-setup)
3. [The `IAdrPlugin` contract](#the-iadrplugin-contract)
4. [The `plugin.json` manifest](#the-pluginjson-manifest)
5. [Choosing which events to subscribe to](#choosing-which-events-to-subscribe-to)
6. [`adrKey` vs. `Adr.Number`: picking a stable external identity](#adrkey-vs-adrnumber-picking-a-stable-external-identity)
7. [Retryable vs. permanent failures](#retryable-vs-permanent-failures)
8. [Deployment model: this runs on each developer's machine](#deployment-model-this-runs-on-each-developers-machine)
9. [Secrets: never put them in `plugin.json`](#secrets-never-put-them-in-pluginjson)
10. [Installing and testing your plugin](#installing-and-testing-your-plugin)

---

## How the plugin system works

- AdrPlus discovers plugins under `./plugins/<name>/` in the repository — one subfolder per plugin, each loaded into its own isolated `AssemblyLoadContext`.
- Whenever a command settles an ADR's status (`approve`, `reject`, `undo`, `supersede`, `migrate`, and the metadata-only `new`/`version`/`revise`), the host dispatches **one event** to every loaded plugin that subscribes to it.
- Dispatch in the foreground is a **single, non-retried attempt**, bounded by a short timeout (`foregroundTimeoutMs`, default 5000ms) — this is what keeps `adrplus approve` fast even if your external system is slow or down. If that attempt doesn't succeed, the host queues the event in a per-plugin pending file and returns control to the user immediately.
- `adrplus sync` (no flags) re-drives whatever is sitting in that pending file, retrying with backoff — safe to run on a schedule (cron/CI), since it's a no-op once everything has synced.
- `adrplus sync --backfill` sweeps every existing ADR and re-emits its *current settled* event — the only way a plugin installed on a repo that already has ADRs ever sees the history. Never wire `--backfill` into a scheduler; it's a deliberate, manual operation.
- `adrplus plugins --list` / `--validate` are your diagnostics: confirm your plugin loaded, see its pending count, or check why it didn't load.

None of this requires you to write any retry, timeout, or scheduling logic yourself — the host handles all of it. Your plugin only needs to answer, for one event at a time: *do I care about this, and did my reaction succeed?*

---

## Project setup

Create a class library targeting `net10.0` (or anything `>= net10.0`) and reference `AdrPlus.Abstractions` — the only assembly your plugin depends on from AdrPlus itself. It's published as its own [NuGet package](https://www.nuget.org/packages/AdrPlus.Abstractions), versioned and released independently of the `adrplus` CLI tool:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AdrPlus.Abstractions" Version="1.0.0-beta6" Private="false" />
  </ItemGroup>
</Project>
```

`Private="false"` matters: it stops the build from copying `AdrPlus.Abstractions.dll` into your plugin's own output folder — see the warning below about why that copy breaks loading. If you're working inside a clone of the AdrPlus repository itself (e.g. contributing the plugin back), a `ProjectReference` to `src/AdrPlus.Abstractions/AdrPlus.Abstractions.csproj` works the same way, as the bundled `AdrIndexer` example does.

Your build output — the plugin's `.dll`, its own dependencies (resolved from its `.deps.json`), and `plugin.json` — goes into its own folder under the target repository's `./plugins/<name>/`. **Do not** copy `AdrPlus.Abstractions.dll` itself into that folder: the host resolves that assembly once and expects your plugin to share the exact same type identity. A second copy alongside your plugin gives it a *different* `IAdrPlugin` type as far as the CLR is concerned, and the host will reject your plugin as not implementing the contract.

---

## The `IAdrPlugin` contract

Every plugin implements exactly one interface:

```csharp
public interface IAdrPlugin : IAsyncDisposable
{
    string Name { get; }       // must match plugin.json's "name"
    string Version { get; }    // must match plugin.json's "version"

    Task InitializeAsync(IPluginContext context, IPluginConfiguration config, CancellationToken ct);
    bool ShouldHandle(AdrEventContext context);
    Task<PluginResult> OnAdrEventAsync(AdrEventContext context, CancellationToken ct);
}
```

- **One singleton instance is held per plugin** for the lifetime of the process — `OnAdrEventAsync` must be reentrant (don't rely on mutable instance state across concurrent calls unless you protect it yourself).
- **`InitializeAsync` runs lazily**: only the first time, in this process, that an event you actually subscribe to is about to be dispatched to you. A developer running `adrplus new` never triggers your `InitializeAsync` if you don't subscribe to `Created` — so don't resolve credentials or open connections in a constructor; do it here instead, where a failure (e.g. a missing API key) can be reported against the one thing that needed it.
- **If `InitializeAsync` throws**, the host treats it as a permanent failure: your plugin is skipped for the rest of this run, with one prominent warning, and nothing is queued for retry (see [Retryable vs. permanent failures](#retryable-vs-permanent-failures)).
- **`ShouldHandle` is a cheap, synchronous pre-filter**, evaluated *in addition to* `plugin.json`'s `subscribedEvents` — use it for anything `subscribedEvents` can't express (e.g. "only ADRs in the `security` scope").
- **`OnAdrEventAsync` must never throw for control flow.** Return `PluginResultStatus.Failed` instead. It must also treat any `AdrEventType` value it doesn't recognize as `Skipped` — the host may add new event types later, and your plugin must not break on one it's never seen.

### Implementing `IAdrPlugin`

Implement the interface directly — no base class required. The host already calls `ShouldHandle`
for you and only invokes `OnAdrEventAsync` when it returned `true` (see `PluginManager.DispatchAsync`),
so your `OnAdrEventAsync` only needs to react and shield its own exceptions into `Failed`:

```csharp
using AdrPlus.Abstractions;

public sealed class MyPlugin : IAdrPlugin
{
    private string _apiKey = "";

    public string Name => "MyPlugin";
    public string Version => "1.0.0";

    public Task InitializeAsync(IPluginContext context, IPluginConfiguration config, CancellationToken ct)
    {
        _apiKey = Environment.GetEnvironmentVariable("MYPLUGIN_API_KEY")
            ?? throw new InvalidOperationException("MYPLUGIN_API_KEY is not set.");
        return Task.CompletedTask;
    }

    public bool ShouldHandle(AdrEventContext context) => true;

    public async Task<PluginResult> OnAdrEventAsync(AdrEventContext context, CancellationToken ct)
    {
        try
        {
            var pageId = await UpsertToMySystemAsync(context, ct);
            return new PluginResult { Status = PluginResultStatus.Success, ExternalKey = pageId };
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Won't self-heal on retry.
            return new PluginResult { Status = PluginResultStatus.Failed, Message = "Invalid API key.", IsRetryable = false };
        }
        catch (HttpRequestException ex)
        {
            // Transient — retryable by default.
            return new PluginResult { Status = PluginResultStatus.Failed, Message = ex.Message };
        }
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
```

`OnAdrEventAsync` must never throw for control flow — catch what you expect and return `Failed`
instead. It must also treat any `AdrEventType` it doesn't recognize as `Skipped`, since the host may
add new event types later.

### `AdrPluginBase`: an optional convenience

Repeating the exception-shielding above in every plugin gets old — `AdrPluginBase` can help: it
implements `IAdrPlugin` for you, turns `OnAdrEventAsync` into a template method that catches
exceptions into a retryable `Fail(...)` automatically, and exposes `Success()`/`Skip()`/
`Fail(message, isRetryable:)` helpers so you never construct `PluginResult` by hand:

```csharp
using AdrPlus.Abstractions;

public sealed class MyPlugin : AdrPluginBase
{
    public override string Name => "MyPlugin";
    public override string Version => "1.0.0";

    private string _apiKey = "";

    public override Task InitializeAsync(IPluginContext context, IPluginConfiguration config, CancellationToken ct)
    {
        _apiKey = Environment.GetEnvironmentVariable("MYPLUGIN_API_KEY")
            ?? throw new InvalidOperationException("MYPLUGIN_API_KEY is not set.");
        return Task.CompletedTask;
    }

    protected override async Task<PluginResult> HandleAsync(AdrEventContext context, CancellationToken ct)
    {
        try
        {
            var pageId = await UpsertToMySystemAsync(context, ct);
            return Success(externalKey: pageId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Fail("Invalid API key.", isRetryable: false);
        }
        catch (HttpRequestException ex)
        {
            return Fail(ex.Message);
        }
    }
}
```

It's entirely optional — the host has no special-cased knowledge of the base class either way; use
whichever fits your plugin's shape.

### What you receive: `AdrEventContext`

```csharp
public sealed record AdrEventContext
{
    public required AdrEventType EventType { get; init; }
    public required bool IsReplay { get; init; }                 // true only during `sync --backfill`
    public required AdrRecordSnapshot Adr { get; init; }
    public required string AdrFilePath { get; init; }
    public required Func<string> GetAdrRenderedContent { get; init; }  // lazy — call only if you need it
    public required RepoInfoSnapshot Repo { get; init; }
    public required string CorrelationId { get; init; }
}
```

`GetAdrRenderedContent` is a delegate, not a plain string, on purpose: rendering the full Markdown content is skipped entirely for events your plugin doesn't handle. Only call it inside `HandleAsync`/after `ShouldHandle` has already said yes — calling it unconditionally in `ShouldHandle` defeats the point.

---

## The `plugin.json` manifest

Every plugin folder needs one, alongside the compiled assembly:

```json
{
  "name": "confluence",
  "version": "1.0.0",
  "entryAssembly": "MyCompany.AdrPlus.Confluence.dll",
  "entryType": "MyCompany.AdrPlus.Confluence.ConfluencePlugin",
  "abstractionsVersion": "1.0.0-beta6",
  "subscribedEvents": [ "Approved", "Rejected", "Superseded", "StatusUndone", "Migrated" ],
  "foregroundTimeoutMs": 5000,
  "backgroundTimeoutMs": 30000,
  "retryPolicy": {
    "maxAttempts": 3,
    "delayMs": 2000,
    "backoff": "Exponential",
    "jitter": true
  },
  "settings": {
    "baseUrl": "https://mycompany.atlassian.net/wiki",
    "spaceKey": "ARCH"
  }
}
```

| Field | Meaning |
|---|---|
| `name` / `version` | Must match `IAdrPlugin.Name`/`Version` exactly — mismatched values reject the plugin at load. |
| `entryAssembly` / `entryType` | The DLL filename and fully-qualified class implementing `IAdrPlugin`. |
| `abstractionsVersion` | The `AdrPlus.Abstractions` version you built against. The host checks the SemVer **major** matches its own; a mismatch rejects the plugin with a warning rather than risking a binary-incompatible load. |
| `subscribedEvents` | Cheap, declarative filter — the host skips dispatch entirely for events not listed here, before your code runs at all. |
| `foregroundTimeoutMs` | How long the single, non-retried foreground attempt gets before the host abandons it and queues a retry. Keep this short — it adds directly to how long `adrplus approve`/etc. takes to return. |
| `backgroundTimeoutMs` / `retryPolicy` | Apply **only** to background re-drive (`adrplus sync`), never to the foreground path — see [How the plugin system works](#how-the-plugin-system-works). `backoff` is `"Fixed"` or `"Exponential"`; delay for attempt *n* is `delayMs` (Fixed) or `delayMs * 2^(n-1)` (Exponential), randomized by `jitter`. |
| `settings` | Your own typed configuration, read via `IPluginConfiguration.GetValue<T>(key)` in `InitializeAsync`. **Non-secret only** — see [Secrets](#secrets-never-put-them-in-pluginjson). |

---

## Choosing which events to subscribe to

`AdrEventType` has eight values: `Created`, `Versioned`, `Revised`, `Superseded`, `Approved`, `Rejected`, `StatusUndone`, `Migrated`.

**If your plugin syncs the decision itself to an external system** (the Confluence/Jira/Teams case), subscribe only to `Approved`, `Rejected`, `Superseded`, `StatusUndone`, and `Migrated`. `Created`, `Versioned`, and `Revised` fire on **metadata-only scaffolding** — `adrplus new`/`version`/`revise` capture title, domain, scope, and a date, but never a finished decision body (`revise` can even start from `--empty`). The user edits the actual content by hand afterward, outside AdrPlus, before running `approve`/`reject`.

This isn't just noise to filter — subscribing to `Revised`/`Versioned` for a content-sync plugin is actively risky. If your plugin upserts one external artifact per ADR (see the next section), a `Revised` event fires *after* your previously-synced, approved content, and reacting to it would overwrite a published decision with a blank draft. Check `context.Adr.StatusUpdate`/`StatusChange` if you need to confirm content is settled before acting.

**If your plugin doesn't do a per-ADR external upsert** — e.g. it regenerates a full index/listing from every ADR's current on-disk state, the way the bundled `AdrIndexer` example does — this risk doesn't apply, and subscribing to all eight events to keep the index current on every change is fine. The distinction is whether an event could cause you to overwrite *previously synced, settled* state with *not-yet-settled* state; a full-rebuild plugin never has that problem because it always reads the current file, not a cached one.

Either way: **treat any `AdrEventType` value you don't recognize as `Skipped`, not an error.** The host may add event types in future releases, and your plugin must degrade gracefully, not throw.

---

## `adrKey` vs. `Adr.Number`: picking a stable external identity

The host's own bookkeeping (deduplication, pending-state tracking) is keyed by an internal `adrKey` (e.g. `"0007-v1-r0"`) that identifies one specific **version+revision file** — it changes on every `Revised`/`Versioned` event. That's the right key *for the host's purposes*, but it is almost certainly the *wrong* key for yours.

If you want **one external artifact that persists across an ADR's whole lifetime** — one Confluence page, one Jira issue, one search-index document — derive your own external identity from `context.Adr.Number` (the stable sequence number, exposed in `AdrRecordSnapshot`), not from any per-revision key. `Number` does not change across `Revised`/`Versioned` events; a scoped key does.

Store whatever identifier your external system gives you back (e.g. a Confluence page id) in `PluginResult.ExternalKey` — the host doesn't interpret it, but round-tripping it through your own retry/replay logic gives you a natural idempotent-upsert path: "does an artifact for `Adr.Number == 7` already exist? If so, update it; otherwise, create it."

The host makes **no cross-revision continuity guarantee** beyond exposing `Number` — it's entirely on your plugin to use it consistently.

---

## Retryable vs. permanent failures

Not every failure is worth retrying. A missing or invalid credential fails identically on every attempt, no matter the backoff schedule; a network blip usually resolves itself. `PluginResult.IsRetryable` (default `true`) is how you tell the host which one just happened:

```csharp
return Fail("Invalid API key.", isRetryable: false);   // permanent — host will NOT queue this for retry
return Fail(ex.Message);                                // transient — retryable by default, queued and retried
```

- **Retryable failures** (the default) get written to the plugin's `./plugins/<name>/state/pending.json` and retried with backoff by `adrplus sync`.
- **`IsRetryable: false`** (and `InitializeAsync` throwing, treated the same way) is **never** written to pending state — there's nothing productive to retry automatically. Instead, the host emits one distinct, prominent warning telling the developer to fix configuration and then run `adrplus sync --backfill` once fixed, rather than silently retrying a failure that can't self-heal.

This split is entirely **protocol-agnostic**, by design: the host only ever sees your `PluginResult` — never an HTTP status code, a socket exception, or any other transport detail. Whether you're talking HTTP, gRPC, a message queue, or something else, classifying "this specific failure is permanent" is your plugin's job, the same way resolving credentials is (see the next two sections). Nothing about `AdrPlus.Abstractions` assumes HTTP.

---

## Deployment model: this runs on each developer's machine

There's no server-side component here. Dispatch happens on whichever developer's machine ran the state-changing command (`approve`, `reject`, etc.), using whatever credentials **that machine** has configured for your plugin. There's no central dispatcher.

Practically, this means:

- Your plugin needs a way to resolve its own credentials per-machine (environment variable, local secret store, whatever fits your team) — the host provides no credentials API at all (see the next section).
- Coverage of your external system is only as consistent as credential provisioning across the team. A developer without valid credentials configured locally simply won't sync — loudly, thanks to `IsRetryable: false`, but they won't sync — until someone with working credentials runs `adrplus sync --backfill`.
- If your team needs more centralized coverage, a CI job running `adrplus sync` (no flags, on a schedule, with one shared credential) re-drives whatever is already pending — safe to automate, since it's self-limiting. `--backfill` itself must stay a manual, occasional operation regardless of who runs it (see the previous section) — never wire it into a cron/CI trigger.
- The process exit code never reflects plugin outcomes (only the local ADR file write does) — scripts that need to know whether sync actually succeeded should check `adrplus plugins --list`'s pending counts or the file log, not the exit code.

---

## Secrets: never put them in `plugin.json`

`plugin.json` (and your plugin's compiled `.dll`) is meant to be **committed to the repository**, so the whole team gets the same plugin installed on clone — that's exactly what `adrplus init` does for the bundled `AdrIndexer` example. That also means anything you put in its `settings` object is checked into git, in plain text, for anyone with repo access to read.

- Put non-secret configuration in `settings` — base URLs, space keys, output filenames, anything that isn't a credential.
- Resolve actual credentials (API keys, tokens, passwords) yourself, from an environment variable or a local secret store — never from `settings`.
- `./plugins/<name>/state/pending.json` is local, per-machine runtime state, not something to commit — make sure your repo's `.gitignore` excludes `plugins/*/state/`.

An optional allowlist can restrict which plugin names are permitted to load at all, configured under `pluginallowlist` in the repo's `adrplus.json` (each entry has a `name`, matched case-insensitively, and an as-yet-unenforced `hash` field reserved for future use). This guards against an unexpected plugin folder being loaded — it does not replace the credential discipline above.

---

## Installing and testing your plugin

1. Build your plugin project and copy its output — DLL, dependencies, `plugin.json` — into `<repo>/plugins/<name>/`.
2. `adrplus plugins --validate --path <repo>` — re-runs structural load validation (manifest schema, `entryType` implements `IAdrPlugin`, `Name`/`Version` match, `abstractionsVersion` compatible) without dispatching any real event. Fix whatever it reports before moving on.
3. `adrplus plugins --list --path <repo>` — confirms your plugin is loaded, shows its subscribed events, allowlist status, and current pending-item count.
4. Trigger a real event (e.g. `adrplus approve` against a `Proposed` ADR) and check your plugin's side effect happened, or that `adrplus plugins --list` now shows a pending item if it didn't.
5. If you're onboarding the plugin onto a repo that already has ADRs, run `adrplus sync --backfill --path <repo>` once, by hand, to receive the existing history. Don't automate this call.
6. For ongoing background re-drive of anything that failed on its first foreground attempt, schedule `adrplus sync --path <repo>` (no flags) via cron/CI — it's self-limiting and safe to run repeatedly.
