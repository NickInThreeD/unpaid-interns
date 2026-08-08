# 103 — Build Versioning & Mismatch Rejection

**Source:** [`core_components.md`](../core_components.md) §13 — Onboarding, Performance & Long Tail
**Status:** ❌ No version stamp and no handshake check exist
**Depends on:** [Client/Server Build Parity](95_client_server_build_parity.md)
**Blocks:** every mismatched-build bug being diagnosable

## Summary

Stamping builds with a version, and refusing to connect when they disagree.

`core_components.md` gives the reason and it is the same one that drives [`95_client_server_build_parity.md`](95_client_server_build_parity.md): ghost serialisation is layout-sensitive, so a client and server on different revisions **fail in confusing ways**. The instruction is to stamp builds and reject mismatched connections at handshake with a clear message.

**Scope boundary, because the two components overlap deliberately:** component 95 owns *what must match* — it enumerates the four parity surfaces (code revision, content catalogue, baked subscenes, data registries) and collapses them into one composite stamp. **This component owns the mechanism**: how the stamp is generated, embedded, transmitted, compared, and reported. They should be built together, and 95 defines this one's input.

The value is not really the rejection. It is that a mismatch becomes **a named failure instead of a mystery**. Without it, a stale client produces a player standing in a wall, health reading as ammo, or an item id resolving to a different item — symptoms that look like a dozen different gameplay bugs and get filed as such. One handshake check converts all of them into "your build is out of date".

## How to Build

**Generate the stamp at build time, from the four surfaces**

- Hash together the git commit hash, the Addressables catalogue version, a hash of the registered subscene set, and the data registry versions ([`87_data_driven_configuration.md`](87_data_driven_configuration.md) requires version-stamped registries; [`93_addressables_content_build.md`](93_addressables_content_build.md) requires the catalogue version to be part of the stamp).
- One composite value. Four separate checks would drift; a single hash cannot.
- Generate it in a **pre-build step** and write it into a generated source file or a build-time asset, so it is baked rather than computed at runtime. A stamp computed at runtime from mutable state is not a build identity.
- Fail the build if the working tree is dirty in a release configuration, or include a dirty marker. A stamp that says "commit abc123" for three different local builds is worse than useless.
- Include a **human-readable version** alongside the hash — a build number and date — because the hash is for comparison and the readable form is what goes in a bug report.

**Check it at handshake, before anything else**

- The exchange must happen **before any gameplay state is replicated**. A client that receives one snapshot of mismatched ghost data has already deserialised garbage.
- Netcode for Entities supports a protocol version check; use it if it fits, and otherwise send the stamp as the first RPC on connect and disconnect on mismatch.
- The check is **symmetric**. The server rejects stale clients, and it reports its own version in the rejection, so a stale *server* is diagnosable rather than presenting as "everyone's client is broken" — which is what happens when four people update and the dedicated server does not ([`95_client_server_build_parity.md`](95_client_server_build_parity.md) requires this specifically).
- No partial compatibility. There is no safe degraded mode for a layout mismatch, and offering one guarantees someone will use it.

**Make the message actionable**

- Name the problem and both versions: *"Build mismatch — this client is build 412, the host is build 418. Update to join."* A timeout or a generic connection error sends the player to the wrong diagnosis entirely.
- Route it through `ConnectionStatusScreen`, alongside the other distinguishable failure reasons required by [`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md) and [`91_join_by_code.md`](91_join_by_code.md) — service unavailable, session not found, session full, session deployed, and now version mismatch. One screen, one taxonomy.
- Log both stamps server-side on every rejection. In a group where one person cannot join, the host's log is where the answer is.

**Surface the version where a player can read it**

- In the main menu, in the pause menu, and in the session info panel. `SessionInfo.cs` already displays and copies the session code ([`91_join_by_code.md`](91_join_by_code.md)); the version belongs beside it and should be copyable for the same reason.
- Every bug report should be able to include it without the reporter knowing what it is. This is the single cheapest improvement to bug report quality available.
- Include it in telemetry records ([`101_analytics_and_balance_telemetry.md`](101_analytics_and_balance_telemetry.md), which requires the stamp so data from different builds can be separated) and in crash reports ([`104_crash_and_error_reporting.md`](104_crash_and_error_reporting.md)).

**Guard the ghost layout directly as well**

- The version stamp catches a mismatch between two builds. It does not catch a developer adding a field to `ControllerState` and not realising why the network broke.
- [`95_client_server_build_parity.md`](95_client_server_build_parity.md) requires an automated test that hashes every ghost-serialised struct layout and fails when it changes without a version bump. That test is the *authoring-time* guard; this handshake is the *runtime* guard. Both are needed and neither replaces the other.
- `FirstPersonController.ControllerState`'s warning comment — repeated at the top and bottom of the struct — becomes redundant once the test exists, which is the point.

**Test the rejection, because untested rejection paths are usually broken**

- Build two clients from deliberately different revisions and confirm refusal with the correct message ([`97_build_verification_pass.md`](97_build_verification_pass.md) includes this in the standing checklist).
- Test a **content-only** mismatch — same code, one extra item in the registry — and confirm the stamp differs and the connection is refused.
- Test a stale **dedicated server** against current clients, since that is the most likely real-world occurrence.
- These need real builds; the Editor is a single revision and cannot produce a mismatch ([`96_editor_vs_build_test_paths.md`](96_editor_vs_build_test_paths.md) Tier 3).

## Acceptance Criteria

- [ ] A composite version stamp is generated at build time from the code revision, content catalogue, subscene set, and registry versions.
- [ ] The stamp is baked into the build, not computed at runtime.
- [ ] A dirty working tree either fails a release build or is marked in the stamp.
- [ ] A human-readable build number and date accompany the hash.
- [ ] Every build profile embeds the stamp, including the dedicated server.
- [ ] Versions are exchanged and compared before any gameplay state is replicated.
- [ ] A mismatched client is disconnected at handshake.
- [ ] The rejection names both versions and states that a build mismatch is the cause.
- [ ] The check is symmetric; a stale server is diagnosable from the client's message.
- [ ] No partial or degraded compatibility path exists.
- [ ] Rejections route through `ConnectionStatusScreen` within the shared failure taxonomy.
- [ ] Both stamps are logged server-side on every rejection.
- [ ] The version is visible and copyable in the main menu, pause menu, and session info panel.
- [ ] The version appears in telemetry records and crash reports.
- [ ] An automated test guards ghost struct layouts at authoring time, independently of the handshake.
- [ ] A code-revision mismatch, a content-only mismatch, and a stale dedicated server are each verified with real builds to be refused correctly.
