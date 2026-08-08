# 95 — Client/Server Build Parity

**Source:** [`core_components.md`](../core_components.md) §12 — Build & Release Readiness
**Status:** ⚠️ No parity guarantee of any kind exists
**Depends on:** [Data-Driven Configuration](87_data_driven_configuration.md), [Addressables Content Build](93_addressables_content_build.md), [Entity Subscene Baking](94_entity_subscene_baking.md)
**Blocks:** trusting any multiplayer bug report

## Summary

Two builds from different revisions failing to communicate, usually without saying so.

`core_components.md` puts it starkly: ghost serialisation is layout-sensitive, and **a client and server built from different code revisions will fail to communicate, often subtly.** The word doing the work is *subtly*. A protocol mismatch that throws is a good day. What actually happens is that a `[GhostField]` struct whose layout changed deserialises into the wrong fields, and a client sees a player at the wrong position, health reading as ammo, or an item id that resolves to a different item.

The project has warnings but no guarantees. `FirstPersonController.ControllerState` carries an explicit comment — *"Adding more members to this struct might break network serialisation, speak to Claire/Andy B"* — repeated at both the top and bottom of the struct, which is a strong signal that someone has been bitten. But a comment is not a check, and nothing anywhere prevents a mismatched pair from connecting.

Parity is broader than code. **Four things must match** between a client and a server: the code revision, the content catalogue, the baked subscenes, and the data registries. Each has its own way of going wrong and each is invisible in the Editor, where there is only ever one of everything.

**Scope boundary:** this component owns *what must match and how mismatch is detected*. The stamping and handshake mechanism is [`103_build_versioning_and_mismatch_rejection.md`](103_build_versioning_and_mismatch_rejection.md); the two should be built together and this one defines its inputs.

## How to Build

**Enumerate the four parity surfaces**

- **Code revision** — ghost component layouts, RPC struct definitions, and command data. The most dangerous and the hardest to detect after the fact. A git commit hash is the natural stamp.
- **Content catalogue** — Addressables. A prefab present in one build and not the other resolves to null on one side only ([`93_addressables_content_build.md`](93_addressables_content_build.md) requires the catalogue version to be part of the stamp).
- **Baked subscenes** — a subscene missing from one build profile produces an empty entity world on that side with no error ([`94_entity_subscene_baking.md`](94_entity_subscene_baking.md)).
- **Data registries** — items, monsters, locations, weather, upgrades. Only ids cross the wire, so a registry mismatch means an id resolving to a different thing or to nothing. [`87_data_driven_configuration.md`](87_data_driven_configuration.md) requires version-stamped registries checked at connect, covering all of them with one stamp.

All four collapse into **one composite version stamp** compared at handshake. Four separate checks would drift; one hash of all four cannot.

**Make the ghost layout risk explicit rather than remembered**

- `ControllerState`'s warning comment is the current mitigation and it is not enough. Several plans deliberately route around it — [`11_stamina.md`](11_stamina.md), [`10_crouch.md`](10_crouch.md), and [`17_climbing_and_verticality.md`](17_climbing_and_verticality.md) all prefer `PredictedPlayerGhost` over `ControllerState` specifically to avoid touching it.
- That preference is correct and should be written down as a **rule**: new per-player gameplay state goes on `PredictedPlayerGhost`; `ControllerState` is closed to additions without deliberate review.
- Better still, replace the comment with a check. A test that hashes the layout of every ghost-serialised struct and fails when it changes without the version being bumped turns an invisible hazard into a failing build. That is cheap and it is exactly the kind of pure-logic test [`89_automated_tests.md`](89_automated_tests.md) is scoped for.

**Reject mismatches at the handshake, loudly**

- A mismatched client must be refused **before** any gameplay state is exchanged, with a message naming the problem: "this build is out of date" is actionable; a timeout is not.
- [`90_relay_and_lobby_service_enablement.md`](90_relay_and_lobby_service_enablement.md) and [`91_join_by_code.md`](91_join_by_code.md) both require distinguishable connection-failure reasons; version mismatch is another one and should route through the same `ConnectionStatusScreen` path.
- Do not attempt graceful degradation. There is no safe partial compatibility with a layout mismatch.

**Include the dedicated server**

- `FPS2 Windows Server` is a separate build profile booting `ServerScene`, and it is the easiest build to forget to rebuild. A server running yesterday's revision against today's clients is the exact failure this component exists to catch.
- The stamp check must be symmetric: the server rejects stale clients and reports its own version, so a stale *server* is diagnosable rather than presenting as "everyone's client is broken".

**Test the failure, not just the success**

- Build two clients from deliberately different revisions and confirm the connection is refused with the right message. An untested rejection path is usually a broken one.
- Verify with a **real content mismatch** too: build one client with an extra item in the registry and confirm the stamp differs.
- Add both to the build verification pass ([`97_build_verification_pass.md`](97_build_verification_pass.md)), which is where two-build scenarios get exercised.

**Keep the Editor honest**

- §12 notes that Multiplayer Play Mode and thin clients are `#if UNITY_EDITOR` only, and **Editor multiplayer testing does not prove a build works** — every networking change needs verification with two real builds, or a build against an Editor host.
- The Editor is a single revision by construction, so it can never surface a parity problem. That makes it the wrong place to gain confidence about this component specifically.
- A build-against-Editor-host test is the cheap middle ground and catches most content and subscene mismatches.

## Acceptance Criteria

- [ ] Code revision, content catalogue, baked subscenes, and data registries are combined into one composite version stamp.
- [ ] The stamp is computed at build time and embedded in every build profile, including the dedicated server.
- [ ] A mismatched client is rejected at handshake before any gameplay state is exchanged.
- [ ] The rejection message names version mismatch specifically and routes through `ConnectionStatusScreen`.
- [ ] The server reports its own version, so a stale server is diagnosable.
- [ ] No partial or degraded compatibility path exists.
- [ ] A rule is documented that new per-player state goes on `PredictedPlayerGhost`, and `ControllerState` is closed to additions without review.
- [ ] An automated test hashes every ghost-serialised struct layout and fails when it changes without a version bump.
- [ ] Two builds from different code revisions are refused with the correct message, verified by actually building both.
- [ ] A content-only mismatch produces a different stamp and is likewise refused.
- [ ] Both failure cases are part of the build verification pass.
- [ ] Editor-only multiplayer testing is documented as insufficient for parity verification.
- [ ] A standalone build connecting to an Editor host is part of the routine test loop.
