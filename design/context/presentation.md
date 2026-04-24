# Amuse DA1 Presentation Content (Revised)

## Slide 1 - Title and Positioning
Title:
Amuse DA1: Building a Fairer, Cloud-Native Music Streaming Platform

Key message:
This project is not only about streaming audio. It is about combining consumer convenience with creator-side fairness and platform-level operational control.

Why this framing:
- Mainstream platforms optimize user convenience very well.
- Creator economics and direct artist distribution remain major pain points.
- DA1 is scoped to prove a realistic, technically sound baseline.

---

## Slide 2 - Agenda
1. Industry context and why this project exists
2. Current market solutions and remaining gaps
3. Project definition, users, and B2B2C logic
4. Core requirements and business rules
5. Architecture decisions with rationale and trade-offs
6. Security, CDN, and scalability strategy
7. DA1 scope, limitations, and out-of-scope boundaries
8. PoC and research progress: current state versus DA1 target
9. Risks, execution priorities, and advisor feedback

---

## Slide 3 - Industry Context: How We Got Here
Historical shift:
- Since around 2010, music consumption moved from physical media to internet-based digital delivery.
- Early digital distribution was heavily shaped by file sharing and piracy-era behavior.
- Legal on-demand streaming became dominant due to convenience and instant access.

What changed for users and artists:
- Users gained access convenience but lost durable ownership of purchased-like media.
- Artists gained exposure channels but often face weak bargaining power and thin payouts.

Why it matters for this topic:
The technical platform design directly affects ecosystem fairness, not only buffering and latency.

---

## Slide 4 - Current Solutions and Their Gaps
Observed strengths:
- Spotify and Apple Music: strong personalized discovery and smooth streaming UX.
- YouTube Music: large UGC ecosystem and strong distribution reach.
- Bandcamp: relatively fair direct artist-to-fan commerce model.

Observed gaps:
- Streaming-first services can be label-heavy in value distribution.
- UGC-heavy services do not inherently solve ownership and fair artist monetization.
- Commerce-friendly services may have weaker mainstream streaming engagement.

Problem opportunity:
Bridge the streaming experience and creator-first economics in one platform model.

---

## Slide 5 - Problem Statement
Core problem:
How can a DA1-scale system deliver secure, high-quality on-demand streaming while also supporting direct creator upload and a clearer monetization workflow?

Engineering problem:
- Avoid fake scalability claims from simple file serving.
- Avoid architecture overreach that kills delivery.
- Preserve tenant isolation and payout correctness from day one.

Success criterion:
Deliver an end-to-end B2B2C vertical slice where playback, governance, and settlement logic are coherent and testable.

---

## Slide 6 - Project Definition
What Amuse DA1 is:
- A multi-tenant B2B2C music platform.
- Two frontends: consumer listening and business operations.
- One core backend with async workers.

What it explicitly includes:
- Adaptive streaming path.
- Catalog/upload pipeline.
- Role and organization workflows.
- Mock billing and settlement.

Why this definition:
It maps to real product responsibilities while remaining feasible within DA1 constraints.

---

## Slide 7 - Target Users and Role Model
User classes:
- Listener: discover, stream, build playlists, optionally buy.
- Artist: upload releases, track performance.
- Unverified artist: limited visibility and restricted privileges.
- Label or group: manage multiple artists under one business account.
- Platform admin: approval, moderation, and policy enforcement.

Why this role model was chosen:
It captures real platform stakeholders and creates a practical basis for permission logic, discovery rules, and payout governance.

---

## Slide 8 - Why B2B2C Is Required
If built as B2C only, critical workflows are missing:
- No robust business-side ingestion governance.
- No member permissions for organizations.
- No accountable payout statements for creators.

Why B2B2C solves this:
- B2C surface optimizes listener experience.
- B2B surface handles content operations, rights workflow, and finance transparency.

What this enables technically:
- Organization-scoped claims and data boundaries.
- Clean separation of UX concerns without splitting the core domain too early.

---

## Slide 9 - Functional Goals and Business Value
Listener-side goals:
- Search, playback, playlist, recommendation, and reporting flows.

Creator-side goals:
- Direct upload without mandatory third-party distributor dependency.
- Verified versus unverified progression path to reduce low-quality catalog spam.

Business-value goals:
- Traceable revenue and payout logic.
- Operational controls for moderation and visibility.

Why this matters:
The product is valuable only when listening quality and creator economics improve together.

---

## Slide 10 - Core Business Rules (With Purpose)
Rule 1: Verified-first discovery sections
- Purpose: protect listener quality and reduce noise.

Rule 2: Unverified handling with explicit policy behavior
- Purpose: keep onboarding open while controlling ranking impact.

Rule 3: Auto-hide moderation threshold
- Purpose: fast abuse containment before manual review completes.

Rule 4: Promotion eligibility plus manual approval
- Purpose: data-informed quality gate, still supervised by platform governance.

Rule 5: Valid stream threshold for settlement
- Purpose: reduce payout distortion from accidental or low-intent plays.

Rule 6: Balanced ledger postings
- Purpose: prevent silent financial inconsistency in settlement outputs.

---

## Slide 11 - Streaming Channel Security
Decision:
Enforce authorization at media edge, not only in API controller logic.

Why this was chosen:
- Segment and manifest URLs are the true asset delivery surface.
- API-only checks are insufficient once media URLs leak.

How it works:
- Playback endpoint issues scoped playback token.
- Edge worker validates token claims per request.
- Fail-closed behavior denies access when checks fail.

What this protects:
- Track, organization, bitrate entitlement, and expiration boundaries.

---

## Slide 12 - CDN and Scalability Strategy
Decision:
Cloudflare-first delivery with R2 storage, short-lived manifest caching, and long-lived segment caching.

Why this was chosen:
- Streaming traffic should not overload core API services.
- Caching behavior differs between mutable manifests and immutable segments.

Operational controls:
- Purge triggers on moderation, republish, entitlement changes, and artifact replacement.
- Token checks remain mandatory for every media request.

Outcome:
Scalable playback path with policy enforcement and cache invalidation control.

---

## Slide 13 - Architecture Decisions: What and Why
Decision A: Monolith-first in DA1
- Why: faster delivery and lower coordination overhead.
- What for: complete core flows before domain decomposition.
- Trade-off: less independent service scaling in early phase.

Decision B: Single DB with tenant row isolation
- Why: operational simplicity and faster iteration.
- What for: enforce org-scoped boundaries consistently.
- Trade-off: stronger discipline needed in query and middleware guards.

Decision C: DASH primary, HLS optional
- Why: adaptive streaming baseline with compatibility path.
- What for: stable initial protocol focus and future extensibility.
- Trade-off: feature-flag and packaging complexity to manage.

Decision D: Mock payment in DA1
- Why: legal onboarding and production payment compliance exceed DA1 constraints.
- What for: validate monetization and ledger flow correctness.
- Trade-off: no real money path in DA1 demo.

Decision E: K3s development, AKS final demo
- Why: cost control and iteration speed during build phase.
- What for: preserve cloud-native deployment proof at final stage.
- Trade-off: environment parity validation required near release.

---

## Slide 14 - Technology and Implementation Method
Technology stack:
- Frontend: Next.js and TypeScript.
- Backend: .NET Web API.
- Data: PostgreSQL and Redis.
- Streaming: FFmpeg-based packaging and CDN delivery.
- Observability: OTLP stack.

Method:
- Requirement analysis from user behavior and market solutions.
- Object, data, UI, and system design.
- Incremental implementation and testing.
- Milestone-driven delivery and benchmark validation.

Why this method:
It balances research depth with build execution under DA1 deadlines.

---

## Slide 15 - DA1 Scope Baseline
Must-deliver DA1 capabilities:
- Identity, multi-org claims, and tenant authorization.
- B2B upload and catalog operations.
- Adaptive playback pipeline.
- B2C discovery and playlist basics.
- Mock billing, ledger, and monthly settlement statements.
- Moderation and promotion workflow.
- CI/CD and demo deployment readiness.

Why this scope is deliberate:
It captures the minimum viable set that proves product, architecture, and business logic coherence.

---

## Slide 16 - DA1 Limitations and Out-of-Scope
Out of scope in DA1:
- **Listener personal music uploads and custom library** — DA1 focuses on platform-curated and creator-uploaded content only, not listener-contributed uploads.
- **Lyrics display and time-synced lyrics/karaoke features** — Lyric services and sync infrastructure require separate content partnership agreements.
- **Miscellaneous personal library features** — Playlists from personal uploads, custom tagging systems, and other user-generated library organization.
- Full microservice decomposition.
- Real payment provider legal integration.
- Advanced social and listen-together ecosystem.
- Full legal and regional compliance implementation depth.
- Advanced recommendation stack beyond DA1 baseline.

Why this boundary is necessary:
Without explicit scope fences, infrastructure and feature creep will compromise core delivery quality. Personal upload and lyrics services are defer-to-DA2 decisions to maintain focus on creator-sourced, moderated content delivery.

---

## Slide 17 - PoC and Research Progress (Current)
Research completed:
- Comparative protocol and delivery-path analysis.
- B2B2C and hybrid creator ecosystem framing.
- Security direction for token-gated media path.

PoC implemented today:
- Backend skeleton with auth, playback, health, and event ingestion routes.
- Mock JWT flow with organization and tier claims parsing.
- Organization scope guard behavior in playback flow.
- Local prepared DASH artifact serving.
- Multi-service local orchestration via compose.

Progress interpretation:
This is a structural validation PoC, not a complete DA1 implementation.

---

## Slide 18 - Gap to DA1 Target
Still required for DA1 completion:
- Full schema implementation and DB-backed domain persistence.
- Redis blacklist and caching strategy implementation.
- Real queue-driven transcode and publish lifecycle.
- Full B2B portal operations and B2C recommendation baseline.
- Settlement engine completeness and reconciliation evidence.
- Edge token validation deployment and purge automation.
- NFR benchmark evidence and AKS release readiness.

Why this gap section matters:
It makes progress honest and gives a concrete execution path for the remaining timeline.

---

## Slide 19 - Risks, Controls, and Execution Priorities
Major risks:
- DA2-level scope creep entering DA1.
- Infra work consuming feature delivery time.
- Transcode backlog and playback instability.
- Tenant or finance integrity defects.

Controls:
- Milestone gates with explicit exit criteria.
- End-to-end user flow completion before infra expansion.
- Idempotent jobs and financial invariants.
- Security and purge integration tests.

Immediate priorities:
1. Data and auth foundations.
2. Upload-to-playable pipeline.
3. Discovery and payout core.
4. Benchmark and release hardening.

---

## Slide 20 - Advisor Feedback Requested
Review points:
- Is the DA1 scope cut appropriate for evaluation quality?
- Is DASH-first with optional HLS still the best delivery strategy under current timeline?
- Is the recommendation baseline level acceptable for DA1 expectations?
- Are the milestone priorities aligned with how progress will be assessed?

Closing:
The project strategy is to deliver a secure and measurable core first, then expand breadth in DA2 once the foundation is proven.
