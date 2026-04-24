# Amuse DA1 - Visual-First Presentation Script

## How To Use This File
- Keep each slide text minimal (3 to 5 bullets max).
- Put the Mermaid diagram or illustration as the main visual.
- Use the Speaker Notes section as your talking script.

---

## Slide 1 - Title
On-slide text:
- Amuse DA1
- Building a Fairer Cloud-Native Music Streaming Platform
- B2B2C, Secure Streaming, Monetization Logic

Visual:
- Illustration Placeholder: Split-screen hero image
- Left: listener with headphones using mobile app
- Right: artist dashboard and payout chart
- Background motif: streaming waveform + cloud infra nodes

Speaker Notes:
This project is not only about low-latency playback. It aims to combine listener convenience with creator-side fairness and platform governance.

---

## Slide 2 - Agenda
On-slide text:
- Industry context and platform gap
- DA1 problem framing and product definition
- Architecture choices and trade-offs
- Scope boundaries: DA1 vs DA2
- PoC status, risks, and advisor feedback asks

Visual (Mermaid):
```mermaid
flowchart LR
  A[Industry Context] --> B[Problem and Product]
  B --> C[Architecture Decisions]
  C --> D[Scope Boundaries]
  D --> E[Progress and Risks]
  E --> F[Advisor Feedback]
```

Speaker Notes:
This agenda sets expectations for a full narrative arc: why this project matters now, what DA1 must prove, which architecture decisions are intentional, and where we are in execution.

---

## Slide 3 - Industry Evolution (Physical -> Piracy -> Streaming)
On-slide text:
- Consumption shifted from physical media to digital
- Piracy exposed distribution weaknesses
- Legal on-demand streaming won on convenience

Visual (Mermaid):
```mermaid
timeline
    title Music Distribution Evolution
    2010 : Physical media still common
    2010-2015 : MP3 sharing and piracy pressure
    2013-2020 : On-demand streaming becomes mainstream
    2020+ : Platform scale grows, creator fairness debate intensifies
```

Speaker Notes:
The project starts from this industry transition: convenience improved, but fairness and ownership concerns remain unresolved.

---

## Slide 4 - Current Platforms: Strengths vs Gaps
On-slide text:
- Spotify/Apple Music: strong UX and discovery
- YouTube Music: strong UGC reach
- Bandcamp: stronger direct artist commerce
- Gap: no single model balances all three

Visual:
- Illustration Placeholder: 3-column comparison cards
- Card 1 Spotify/Apple, Card 2 YouTube Music, Card 3 Bandcamp
- Each card has two sections: strengths and structural limitations

Speaker Notes:
The opportunity is not to copy one platform. It is to combine streaming convenience with creator-first economics in one coherent architecture.

---

## Slide 5 - Problem Statement
On-slide text:
- Need secure adaptive streaming
- Need direct creator upload and governance
- Need transparent payout logic
- Must be feasible within DA1 timeline

Visual (Mermaid):
```mermaid
flowchart LR
    A[Listener Convenience] --> E[Platform Design Goal]
    B[Creator Fairness] --> E
    C[Operational Governance] --> E
    D[DA1 Time Constraint] --> E
```

Speaker Notes:
Success means delivering a working vertical slice where playback, governance, and monetization are all internally consistent and testable.

---

## Slide 6 - Why B2B2C (Not B2C Only)
On-slide text:
- B2C only solves listening UX
- Missing business workflows without B2B
- B2B2C supports real content lifecycle

Visual (Mermaid):
```mermaid
flowchart LR
    L[Listeners - B2C Portal] --> P[Amuse Platform]
    A[Artists] --> B[Business Portal - B2B]
    G[Labels/Groups] --> B
    B --> P
    P --> C[CDN Streaming + Catalog + Settlement]
```

Speaker Notes:
B2B2C is required because upload governance, role permissions, moderation, and payouts are business-side concerns that B2C-only systems cannot model properly.

---

## Slide 7 - User and Role Model
On-slide text:
- Listener
- Artist
- Unverified Artist
- Label or Group
- Platform Admin

Visual:
- Illustration Placeholder: role map diagram
- Center node: Amuse Platform
- 5 role icons with labeled capabilities around center

Speaker Notes:
This role model is the basis for claims, permissions, discovery policy, moderation flow, and payout visibility.

---

## Slide 8 - Core Business Rules (Visual)
On-slide text:
- Verified-first search sections
- Unverified handling with policy toggle
- Auto-hide after report threshold
- Valid stream >= 30 seconds for settlement
- Balanced ledger entries only

Visual (Mermaid):
```mermaid
flowchart TD
    U[Track Uploaded] --> V{Artist Verified?}
    V -->|Yes| P[Public Discovery + RS Eligible]
    V -->|No| D[Unverified Section Behavior]
    D --> R{Reports >= 5 valid?}
    R -->|Yes| H[Auto Hide + Manual Review Queue]
    R -->|No| K[Remain Discoverable by Policy]
```

Speaker Notes:
These rules exist to balance openness for creators and quality control for listeners while preserving governance and business integrity.

---

## Slide 9 - Overall System Architecture
On-slide text:
- Two frontends, one platform core
- Async processing for heavy media tasks
- Cloud edge delivery for playback traffic
- Data and policy separation by responsibility

Visual (Mermaid):
```mermaid
flowchart TB
    subgraph UX[Experience Layer]
      C[Consumer Web]
      B[Business Web]
    end

    subgraph APP[Application Layer]
      API[API Core]
      AUTH[Auth and Tenant Guard]
      REC[Discovery and Recommendation]
      BILL[Billing and Settlement]
      MOD[Moderation and Promotion]
    end

    subgraph JOBS[Async Layer]
      Q[Queue]
      W[Transcode Worker]
    end

    subgraph DATA[Data Layer]
      PG[(PostgreSQL)]
      RE[(Redis)]
      R2[(Cloudflare R2)]
    end

    subgraph EDGE[Edge Delivery]
      CDN[Cloudflare CDN]
      CFW[Cloudflare Worker]
    end

    C --> API
    B --> API
    API --> AUTH
    API --> REC
    API --> BILL
    API --> MOD
    API --> PG
    API --> RE
    API --> Q
    Q --> W
    W --> R2
    C --> CDN
    CDN --> CFW
    CFW --> R2
```

Visual add-on:
- Illustration Placeholder: cloud icon architecture board
- Include simple icons for browser, API, queue, worker, DB, cache, object storage, CDN edge

Speaker Notes:
This diagram gives the full architecture at a glance: product surfaces, core domain services, asynchronous processing, and edge media delivery.

---

## Slide 10 - CDN and Scalability Model
On-slide text:
- R2 for artifact storage
- CDN for high-volume delivery
- Short TTL for manifests, long TTL for segments
- Purge on moderation and republish events

Visual:
- Illustration Placeholder: layered architecture graphic
- Layer 1 client players, Layer 2 edge worker/CDN, Layer 3 object storage, Layer 4 control APIs

Speaker Notes:
The goal is to keep playback load off the core API while still enforcing policy and entitlement checks.

---

## Slide 11 - Decision 1: Monolith-First in DA1
On-slide text:
- Choice: modular monolith for DA1 core services
- Why: faster integration and lower coordination overhead
- What for: deliver complete end-to-end flows early
- Trade-off: fewer independent deploy and scale boundaries

Visual (Mermaid):
```mermaid
flowchart LR
    A[Single Service Boundary] --> B[Fast feature integration]
    A --> C[Simpler local debugging]
    A --> D[Lower orchestration overhead]
    D --> E[Deliver DA1 milestones sooner]
```

Speaker Notes:
This is a sequencing decision, not a denial of microservices. We optimize for delivery reliability in DA1, then split domains in DA2 when boundaries are validated.

---

## Slide 12 - Decision 2: Single DB + Tenant Isolation
On-slide text:
- Choice: single PostgreSQL database in DA1
- Why: operational simplicity and faster migration cycles
- What for: predictable data consistency across domains
- Trade-off: strict tenant guards are mandatory

Visual (Mermaid):
```mermaid
flowchart LR
    T[Access Token org scope] --> G[Tenant Guard Middleware]
    G --> Q[Scoped Queries by organization_id]
    Q --> DB[(Single PostgreSQL)]
    DB --> I[Consistent transactional model]
```

Speaker Notes:
This keeps data operations simple during DA1, while enforcing isolation through claims and query guards rather than separate databases per tenant.

---

## Slide 13 - Decision 3: DASH Primary, HLS Optional
On-slide text:
- Choice: DASH as default delivery protocol
- Why: focused adaptive baseline and implementation control
- What for: stable playback path for DA1 acceptance
- Trade-off: HLS compatibility is feature-flagged, not default

Visual (Mermaid):
```mermaid
flowchart LR
    U[Uploaded Master File] --> X[FFmpeg Packaging]
    X --> D[DASH Artifacts default]
    X --> H[HLS Artifacts optional flag]
    D --> P[Primary Player Path]
    H --> F[Fallback Compatibility Path]
```

Speaker Notes:
The protocol choice is about delivery risk management. One strong default path is better than two partial paths that both fail quality targets.

---

## Slide 14 - Decision 4: Mock Payment in DA1
On-slide text:
- Choice: mock provider callbacks, real ledger logic
- Why: legal and compliance onboarding exceed DA1 timeline
- What for: validate revenue and payout business correctness
- Trade-off: no real-money transaction path in demo

Visual (Mermaid):
```mermaid
flowchart LR
    C[Mock Checkout] --> P[Mock Callback]
    P --> L[Double-entry Ledger]
    L --> S[Monthly Settlement]
    S --> R[Payout Statements]
```

Speaker Notes:
This preserves financial logic integrity while removing external legal blockers. DA1 proves correctness of accounting flow, not payment vendor contracts.

---

## Slide 15 - Decision 5: K3s Dev, AKS Demo
On-slide text:
- Choice: local-first K3s for daily build loops
- Why: cost control and faster iteration
- What for: reserve AKS for release-hardening and demo proof
- Trade-off: must validate environment parity before final release

Visual (Mermaid):
```mermaid
flowchart LR
    DEV[K3s Daily Development] --> TEST[Integration + Load Validation]
    TEST --> REL[AKS Release Candidate]
    REL --> DEMO[Final Demonstration]
```

Speaker Notes:
This strategy minimizes cloud burn during development while still proving cloud-native deployment in the final evaluation window.

---

## Slide 16 - Streaming Security: Why Edge Authorization
On-slide text:
- Media URLs are the real attack surface
- API-only auth is not enough
- Every manifest and segment must be token-validated
- Fail-closed: deny if invalid

Visual (Mermaid):
```mermaid
sequenceDiagram
    participant Client
    participant API as Playback API
    participant Edge as Cloudflare Worker
    participant R2 as R2 Storage

    Client->>API: Request playback manifest
    API-->>Client: manifest_url + playback_jwt
    Client->>Edge: GET manifest/segment + token
    Edge->>Edge: Validate claims/signature/expiry/path/bitrate
    Edge->>R2: Fetch object only if valid
    R2-->>Edge: Media object
    Edge-->>Client: Stream response
```

Speaker Notes:
This is chosen to protect assets at the delivery path itself, not just at API boundaries.

---

## Slide 17 - Scope Framework (How Scope Is Decided)
On-slide text:
- Scope axis 1: business value for DA1 grading
- Scope axis 2: delivery risk within timeline
- Scope axis 3: dependency criticality (blocks other work or not)
- Rule: DA1 keeps only high-value, dependency-critical items

Visual (Mermaid):
```mermaid
quadrantChart
    title DA1 Scope Framing
    x-axis Lower delivery risk --> Higher delivery risk
    y-axis Lower DA1 value --> Higher DA1 value
    quadrant-1 Build if feasible
    quadrant-2 Core DA1
    quadrant-3 Defer
    quadrant-4 Avoid
    Adaptive playback + Normalization: [0.2, 0.9]
    Upload-transcode-publish flow: [0.21, 0.83]
    Moderation + visibility rules: [0.4, 0.78]
    Ledger + settlement baseline: [0.5, 0.65]
    Real payment provider onboarding: [0.75, 0.6]
    Full microservice split: [0.8, 0.05]
    Listen-together/social: [0.8, 0.55]
    Advanced recommendation: [0.8, 0.75]
```

Speaker Notes:
This framework is used to stop scope creep. If a feature is high risk and non-blocking, it moves out of DA1 unless it directly impacts mandatory acceptance criteria.

---

## Slide 18 - DA1 In-Scope (Detailed)
On-slide text:
- Identity and multi-tenant access control
- B2B catalog and upload workflow
- Adaptive playback path (DASH-first)
- Discovery, playlist, recommendation baseline
- Moderation, promotion gates, and governance
- Mock billing, ledger, and monthly settlement
- CI/CD, observability, and benchmark evidence

Visual:
- Illustration Placeholder: scope checklist board
- 7 horizontal lanes with progress markers: Auth, Catalog, Playback, Discovery, Governance, Finance, Ops

Speaker Notes:
This is the official DA1 delivery contract. Each item maps to requirements and acceptance tests, so scope is measurable rather than narrative.

---

## Slide 19 - DA1 Out-of-Scope (Explicit Defer List)
On-slide text:
- Full microservice decomposition across all domains
- Real payment legal onboarding and live payout rails
- Listener personal music uploads and custom library
- Lyrics display and time-synced karaoke features
- Advanced social features (listen-together, comments, campaigns)
- Advanced recommendation stack requiring heavy data infra
- Full legal compliance implementation across regions

Visual (Mermaid):
```mermaid
flowchart LR
    A[Candidate Feature] --> B{Required for DA1 acceptance?}
    B -->|Yes| C[Keep in DA1 scope]
    B -->|No| D{High risk or high integration cost?}
    D -->|Yes| E[Move to DA2 backlog]
    D -->|No| F[Optional stretch only]
```

Speaker Notes:
This list protects delivery quality. Deferred does not mean unimportant; it means sequenced for the right phase.

---

## Slide 20 - Why ML Recommendation Is Out of Scope in DA1
On-slide text:
- DA1 keeps recommendation at baseline heuristic level
- No ML training pipeline, feature store, or model serving in DA1
- No large-scale behavior telemetry loops for model optimization
- Goal in DA1: correctness, governance, and delivery reliability first
- ML recommendation is a planned DA2 expansion track

Visual (Mermaid):
```mermaid
flowchart LR
  A[DA1 Foundation] --> B[Rule-based recommendation baseline]
  B --> C[Playback and event data quality]
  C --> D[DA2 ML Readiness]
  D --> E[Feature store + model training]
  E --> F[Online model serving and evaluation]
```

Speaker Notes:
ML recommendation is intentionally deferred because it depends on clean historical data, robust MLOps, and experimentation loops. DA1 focuses on producing the trusted data and product foundation needed before ML investment.

---

## Slide 21 - DA1 vs DA2 Boundary Map
On-slide text:
- DA1 goal: prove core platform correctness
- DA2 goal: expand scale and product breadth
- DA1 outputs become DA2 foundation artifacts

Visual (Mermaid):
```mermaid
flowchart TB
    subgraph DA1[DA1 - Foundation]
      A1[Auth + Tenancy]
      A2[Upload -> Transcode -> Playback]
      A3[Moderation + Promotion Rules]
      A4[Ledger + Settlement Baseline]
      A5[CI/CD + NFR Evidence]
    end

    subgraph DA2[DA2 - Expansion]
      B1[Service decomposition]
      B2[Advanced social/listen-together]
      B3[Advanced recommendation]
      B4[Real payment integrations]
      B5[Broader compliance depth]
    end

    A1 --> B1
    A2 --> B1
    A4 --> B4
    A5 --> B5
```

Speaker Notes:
The point of this boundary map is continuity: DA1 is not a throwaway prototype. It intentionally creates reusable technical and business artifacts for DA2.

---

## Slide 22 - Scope Guardrails During Implementation
On-slide text:
- Guardrail 1: no new domain unless one existing milestone is closed
- Guardrail 2: no infra expansion before end-to-end user flow passes
- Guardrail 3: no feature enters DA1 without acceptance test owner
- Guardrail 4: if schedule slips, cut breadth before cutting integrity

Visual:
- Illustration Placeholder: "scope firewall" diagram
- Left: incoming feature requests
- Middle: guardrail gate checks
- Right: DA1 committed backlog vs DA2 backlog

Speaker Notes:
These guardrails are practical controls to keep the project finishable. The team should cut optional breadth first, never tenant security, settlement correctness, or playback reliability.

---

## Slide 23 - PoC Progress (What Exists Now)
On-slide text:
- API skeleton for auth, playback, events, health
- Mock JWT with org and tier claims
- Org-scope checks in playback flow
- Local DASH artifact serving
- Multi-service local compose setup

Visual:
- Illustration Placeholder: screenshot collage
- 1 backend endpoint list screenshot
- 1 local compose services screenshot
- 1 playback test response screenshot

Speaker Notes:
Current status is a scaffolding PoC that validates architecture shape, not full DA1 completion.

---

## Slide 24 - Gap: PoC to DA1 Completion
On-slide text:
- DB-backed domain persistence
- Redis blacklist and caching
- Real queue-driven transcode pipeline
- Full B2B/B2C feature completion
- Edge deployment and purge automation
- NFR benchmarks and AKS CI/CD evidence

Visual (Mermaid):
```mermaid
gantt
    title DA1 Remaining Work (High-Level)
  dateFormat  MM-DD
  axisFormat  %m/%d
    section Foundation
  Schema + Auth Hardening           :a1, 04-25, 6d
    section Streaming Core
  Upload -> Queue -> Packaging      :a2, 05-01, 7d
    section Product Features
  B2C Discovery + B2B Ops           :a3, 05-08, 8d
    section Business Logic
  Billing + Ledger + Settlement     :a4, 05-16, 6d
    section Release
  NFR Validation + AKS Deployment   :a5, 05-22, 5d
```

Speaker Notes:
This slide keeps progress reporting honest and execution-oriented.

---

## Slide 25 - Risks and Mitigation
On-slide text:
- Scope creep
- Infra complexity before core flows
- Transcode backlog risk
- Tenant and finance integrity risk

Visual (Mermaid):
```mermaid
flowchart LR
    R1[Scope Creep] --> M1[Scope Gates + Milestone Exit Criteria]
    R2[Infra Overfocus] --> M2[Finish End-to-End Flows First]
    R3[Queue Backlog] --> M3[Worker Monitoring + Retry Policy]
    R4[Tenant or Ledger Defect] --> M4[Security Tests + Reconciliation Tests]
```

Speaker Notes:
Risk management is part of architecture, not a separate afterthought.

---

## Slide 26 - What Success Looks Like for DA1
On-slide text:
- Secure adaptive playback works end-to-end
- Business operations and moderation are enforceable
- Settlement baseline is reproducible and auditable
- Performance targets and deployment evidence are documented

Visual:
- Illustration Placeholder: final "definition of done" dashboard
- Four status cards: Playback, Governance, Finance, Ops
- Each card has green/yellow/red indicator for demo review

Speaker Notes:
DA1 success is measured by coherence, testability, and operational evidence, not by feature count alone.

---

## Slide 27 - Advisor Questions
On-slide text:
- Is this DA1 scope cut appropriate?
- Keep DASH-first if timeline tight?
- Is recommendation baseline sufficient?
- Are milestone priorities aligned with grading?

Visual:
- Illustration Placeholder: clean closing slide with roadmap arrow DA1 -> DA2

Speaker Notes:
Closing statement: The strategy is to deliver a secure, measurable core now, then expand breadth in DA2 once the foundation is proven.

---

## Optional Backup Slide A - Detailed System Architecture
Visual (Mermaid):
```mermaid
flowchart TB
    subgraph FE[Frontends]
      C[Consumer Web]
      B[Business Web]
    end

    subgraph BE[Backend]
      API[REST API]
      W[Worker]
      Q[Queue]
    end

    subgraph DATA[Data Layer]
      PG[(PostgreSQL)]
      RE[(Redis)]
      R2[(Cloudflare R2)]
    end

    subgraph EDGE[Delivery Edge]
      CFW[Cloudflare Worker]
      CDN[Cloudflare CDN]
    end

    C --> API
    B --> API
    API --> PG
    API --> RE
    API --> Q
    Q --> W
    W --> R2
    C --> CDN
    CDN --> CFW
    CFW --> R2
```

---

## Optional Backup Slide B - Settlement Logic Snapshot
Visual (Mermaid):
```mermaid
flowchart TD
    E[Playback Events] --> F{played_ms >= 30000?}
    F -->|No| X[Exclude from payout]
    F -->|Yes| V[Count as valid stream]
    V --> A[Aggregate by period]
    A --> J[Double-entry journal posting]
    J --> S[Payout statements]
```
