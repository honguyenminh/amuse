Cơ sở lý thuyết — Công nghệ và chiến lược Frontend (Amuse)

Tài liệu nháp phục vụ phần Cơ sở lý thuyết trong báo cáo đồ án. Nội dung tập trung vào frontend: công nghệ được chọn, cơ chế hoạt động, và chiến lược triển khai cụ thể trong dự án Amuse. Chi tiết triển khai và runbook kiểm thử nằm ở các tài liệu kỹ thuật cùng thư mục: b2c-frontend-foundation.md, playback.md, business-portal-tenancy.md, commerce.md, ci-cd.md.


        1. Tổng quan kiến trúc giao diện

Phần giao diện của Amuse không được xây dựng dưới dạng một Single Page Application duy nhất phục vụ mọi đối tượng người dùng. Giải pháp tách thành hai ứng dụng Next.js độc lập, cùng nằm trong monorepo frontend:

    • Consumer (frontend/consumer, cổng phát triển 3000): người nghe cuối (B2C) — khám phá catalog, phát trực tuyến adaptive, thư viện, playlist, mua hoặc sở hữu track/release.
    • Business (frontend/business, cổng 3001): thành viên tổ chức và platform operator (B2B) — upload, quản lý catalog, thành viên, giá, payout, vận hành nền tảng.

Cấu trúc monorepo:

    • frontend/consumer — Next.js 16, cổng nghe nhạc
    • frontend/business — Next.js 16, cổng doanh nghiệp và platform
    • frontend/packages/catalog-text — package dùng chung cho hiển thị văn bản mô tả catalog

Điểm khác biệt so với kiến trúc frontend thông thường:

    • Hai persona, hai app — auth, gate, và claim gating không dùng chung một router.
    • Consumer ưu tiên SEO và phát nhạc — catalog public render server + ISR; playback client-heavy với DASH.
    • Business ưu tiên workspace đa tenant — chọn persona org/platform, refresh token theo context, nav theo claims JWT.
    • Chia sẻ mã tối thiểu — chỉ catalog-text; mỗi app tự quản API client và auth.


        2. Next.js 16 và chiến lược render

            2.1. Framework Next.js và App Router

Next.js là framework React full-stack trên Node.js (Vercel). Phiên bản 16 dùng App Router (src/app): routing theo thư mục, layout lồng nhau, Server Component mặc định (server), Client Component (use client) khi cần tương tác trình duyệt.

Các phương pháp render Next.js hỗ trợ:

    • Static Site Generation (SSG) — HTML sinh lúc build
    • Server-Side Rendering (SSR) — HTML sinh mỗi request
    • Incremental Static Regeneration (ISR) — cache trang với revalidate (giây), tái sinh nền khi hết hạn
    • Client-only — fetch sau mount trên browser

            2.2. Phân tầng chiến lược render trong Amuse

Amuse không dùng một chiến lược cho toàn app mà phân tầng theo loại trang:

    • Catalog public (SEO) — ví dụ /artist/.../release/...: Server Component + ISR (revalidate = 3600). Crawler và share link có metadata + HTML; giảm tải API.
    • Redirect tương thích — /release/{uuid}: permanent redirect sang URL slug canonical.
    • Home / search (consumer) — /home, /search: Client Component + fetch sau mount. Trade-off SEO trang chủ để linh hoạt tương tác.
    • Auth — /login, /signup: Client (form, cookie HttpOnly, tham số next).
    • Playback — PlaybackProvider, /playing: Client-only (DOM audio, dash.js, rAF seekbar).
    • Business portal — (portal)/*: Client shell + PortalGate (session, persona, claim nav).

Thông số cache catalog (consumer):

    • PUBLIC_PAGE_REVALIDATE_SECONDS = 3600 (trang artist, release, …)
    • SITEMAP_REVALIDATE_SECONDS = 86400 (sitemap.xml)

Triển khai container: cả hai app dùng output standalone — Docker copy .next/standalone + static, chạy node server.js.

            2.3. Nhóm route (route groups)

Route group (thư mục trong ngoặc đơn) không ảnh hưởng URL:

    • (b2c)/ — consumer browse + library; bọc B2cGate
    • (auth)/ — login, signup; không qua gate portal
    • (portal)/ — business; bọc PortalGate

Layout lồng: app/layout.tsx (font, Providers) → layout nhóm (gate) → page.

            2.4. Hai đường gọi API: server và client

    • serverPublicFetch — server only (module server-only); không auth; cache next.revalidate / tags; dùng cho catalog công khai
    • publicFetch / catalogClient — browser; không auth
    • authFetch — browser; Bearer + credentials include (cookie refresh)

Biến môi trường:

    • NEXT_PUBLIC_API_BASE_URL — browser và build-time (dash.js, presigned redirect)
    • API_INTERNAL_BASE_URL — server trong K8s (gọi API nội bộ, tránh ingress công khai)

Header X-Amuse-Client: web trên mọi request — backend phân biệt web (refresh trong cookie) và mobile (refresh trong body).


        3. React 19, TypeScript, và kiến trúc component

            3.1. React

React: state → Virtual DOM diff → cập nhật DOM. Amuse dùng React 19 với hooks và Context.

Tầng component:

    • Primitive — Button, Card, Slider, Skeleton
    • Feature — ReleasePageView, MiniPlayer
    • Shell — AppShell (consumer), PortalNav (business)

Cây Provider (consumer) — thứ tự có chủ ý:

    • ThemeProvider
    • AuthProvider
    • LikedTracksProvider
    • PlaybackProvider
    • SnackbarProvider, KeyboardShortcutsProvider, PlaybackContextMenuProvider, …

PlaybackProvider ghi playingSeed vào ThemeProvider → ThemeProvider phải bọc ngoài PlaybackProvider.

            3.2. TypeScript

    • Static typing cho DTO mirror backend (lib/api/types.ts, financeTypes.ts)
    • ApiError — status + code (RFC 7807 ProblemDetails)
    • CI: tsc --noEmit trước next build


        4. Tailwind CSS và hai hệ thống giao diện

            4.1. Tailwind CSS v4

Utility-first CSS; class semantic (bg-primary, text-on-surface) map tới biến CSS. PostCSS plugin @tailwindcss/postcss.

            4.2. Consumer — theme động Material Design 3

Pipeline theme runtime:

    • Color seed (OKLCH) — từ cover art hoặc hash fallback
    • Material Color Utilities — sinh palette semantic
    • Biến --amuse-* — inject lên document.documentElement qua ThemeProvider
    • Ưu tiên seed: pageSeed > playingSeed > defaultSeed; pause → biến thể paused

Bổ sung server-side:

    • getCachedCoverArtColorSeed + ThemeSeedStyles trên trang release SSR — tránh flash màu trước hydrate

Quality gate:

    • pnpm check:colors — từ chối màu literal (hex, zinc-*) ngoài token

Typography: utility text-display-large … text-label-medium (Material type scale).

            4.3. Business — shadcn, Base UI, TipTap

    • shadcn — component copy vào repo, chỉnh sửa được
    • Base UI + Tailwind — nền component
    • Lucide — icon
    • TipTap — editor mô tả release; lưu Markdown; hiển thị qua catalog-text

Lý do tách hai design system: consumer = app nghe nhạc, branding động; business = form/dữ liệu dày, UI ổn định.


        5. pnpm, monorepo, và package chia sẻ

    • pnpm — package manager; store dedupe; ghim pnpm@11.5.2
    • Workspace — mỗi app có pnpm-workspace.yaml trỏ ../packages/*

Package @amuse/catalog-text:

    • Parse và hiển thị formatted catalog text (paragraph, bold, link, hashtag)
    • FormattedCatalogText — render an toàn (allowlist URL)
    • catalogHashtagPath, normalizeHashtagTag — routing hashtag thống nhất
    • transpilePackages trong next.config — Next biên dịch TS nguồn package

Chiến lược chia sẻ:

    • Chỉ share logic presentation thuần (text format, path helper)
    • Không share auth, API client, UI shell


        6. Chiến lược xác thực

            6.1. Mô hình token (cả hai app)

    • Access token — JWT ngắn hạn, in-memory (sessionStore), header Authorization Bearer
    • Refresh token — HttpOnly cookie, credentials include; không localStorage
    • withRefreshLock — single-flight refresh khi nhiều request 401

            6.2. Consumer — persona listener

Luồng sau đăng nhập:

    • Login → access token + cookie refresh
    • bootstrapListener — POST ensure listener profile, verify persona listener
    • Restore session on mount — refresh cookie → bootstrap lại

Auth gating có chọn lọc:

    • Anonymous OK — /home, artist, release (B2cGate chỉ chờ restore, không redirect login)
    • Bắt buộc session — playback (playQueue → /login?next=...), library cá nhân, checkout

Quyết định sản phẩm: SEO (index catalog) + funnel (đăng nhập khi bấm phát).

            6.3. Business — đa persona

Luồng:

    • Login → list personas (org / platform)
    • /select-persona nếu nhiều workspace
    • refreshTokens(personaContext) — JWT gắn org_id + claims
    • Onboarding portal profile nếu thiếu
    • PortalGate — redirect login/persona/onboarding; tách /platform/* vs /dashboard

Closed org UX (404 + tenancy.organization_not_found):

    • authFetch notify → AuthProvider reload personas → switch workspace → banner

            6.4. Claim gating (business)

    • Nav và action đọc claims từ JWT (jwtClaims.ts, platformClaims.ts)
    • Không check thô manage:platform:organizations — dùng helper mirror PlatformClaims
    • platform:root và manage:platform:all imply toàn bộ quyền platform
    • Ví dụ: Finance nav khi read:payout:all; Platform Accounting khi canReadPlatformAccounting


        7. Chiến lược SEO và discoverability (consumer)

URL canonical:

    • /artist/{artistSlug}/release/{releaseSlug}
    • /artist/{artistKey}
    • /playlist/{playlistId}, /hashtag/{tag}
    • /release/{releaseId} → permanent redirect sang slug

Metadata (generateMetadata trên server):

    • Title, description
    • Open Graph (music.album, profile)
    • Twitter card
    • Canonical URL (lib/seo/canonical.ts)
    • Mô tả rút gọn qua excerptText

Khám phá:

    • app/sitemap.ts — paginate API catalog; revalidate 86400
    • app/robots.ts — crawl rules
    • isPublicBrowsePath() — route phải SSR/ISR không phụ thuộc auth


        8. Phát nhạc adaptive streaming (consumer)

Phần kỹ thuật đặc thù nhất của frontend Amuse.

Luồng phát:

    • authFetch GET .../tracks/{id}/stream-info
    • API trả manifest DASH relative, loudness, renditions, isOwner
    • dash.js v5 attach HTMLAudioElement (Media Source Extensions)
    • Manifest/segment qua API; segment → 302 presigned R2/MinIO
    • addRequestInterceptor gắn Authorization + X-Amuse-Client (dash v5; RequestModifier cũ không dùng)

Yêu cầu: NEXT_PUBLIC_API_BASE_URL trỏ đúng host API (không phải origin Next.js).

State management:

    • Reducer thuần PlaybackState — unit test Vitest
    • Side effects trong PlaybackProvider — audio, dash, fetch, theme bridge
    • playbackOutput (Web Audio) — fade in/out; normalization từ stream-info.loudness.linearGainLu

Commerce trên playback:

    • selectRendition — non-owner cap ≤128 kbps preview; owner full ladder

UX kỹ thuật:

    • Seekbar — requestAnimationFrame ~60 Hz khi playing; scrub local state khi drag
    • Repeat, shuffle, previous (quy tắc 3 giây)
    • Volume normalization toggle — refresh gain không reload track

Chi tiết: playback.md.


        9. Commerce và tài chính trên giao diện

Consumer (financeClient):

    • Release page — PWYW, free acquire, Stripe checkout redirect
    • /library/purchases — poll sau ?checkout=success
    • TrackDownloadButton — owner-only download

Business:

    • ReleasePricingPanel — claim manage:catalog:pricing:all
    • Payout wizard Gate B, balance, withdraw — claims payout
    • Platform — accounting, refunds, payout profiles (platform claims)

Chi tiết: commerce.md.


        10. Kiểm thử và chất lượng mã

Consumer:

    • tsc --noEmit (CI)
    • check:colors (CI)
    • Vitest — reducer playback, theme, SEO paths, API errors, …
    • next build (CI)
    • pnpm lint — local; chưa CI

Business:

    • tsc --noEmit (CI)
    • next build (CI)
    • Chưa có unit test script
    • ESLint local; chưa CI

Chiến lược test: ưu tiên logic thuần (reducer, money format, parse text); tránh mock DOM nặng.


        11. Container hóa và CI/CD frontend

Dockerfile (multi-stage):

    • deps — pnpm install --frozen-lockfile
    • builder — NEXT_PUBLIC_API_BASE_URL build-arg; pnpm build
    • runner — user nextjs, standalone, node server.js
    • Context: frontend/ (gồm packages/catalog-text)

GitHub Actions:

    • Hai pipeline độc lập — consumer, business
    • Path filter packages/** → chạy cả hai
    • Publish → GHCR → Trivy → bump tag amuse-deploy → Argo CD sync

Build-time env (NEXT_PUBLIC_API_BASE_URL bake theo branch/tag):

    • master, pr-* — API dev
    • staging — API staging
    • production — API production
    • Đổi host API = rebuild image

Chi tiết: ci-cd.md.


        12. Tóm tắt công nghệ — vai trò trong Amuse

    • Next.js 16 App Router — ISR catalog SEO; standalone Docker; route groups
    • React 19 — Provider tree; Server/Client split
    • TypeScript 5.x — DTO mirror backend; ProblemDetails codes
    • Tailwind v4 — consumer MD3 tokens; business shadcn
    • pnpm — monorepo; catalog-text share
    • dash.js 5 — DASH player; manifest qua API; auth interceptor
    • Material Color Utilities — cover art → theme seed
    • TipTap — rich text editor catalog (business)
    • shadcn / Base UI / Lucide — business UI kit
    • Vitest — unit test logic thuần (consumer)
    • @amuse/catalog-text — formatted text + hashtag an toàn


        13. Hạn chế và hướng cải thiện

    • Home/search client-only — cân nhắc SSR/RSC cho SEO home
    • Queue mất sau login redirect — chưa persist localStorage
    • Token hết hạn mid-DASH — chưa refresh trong dash interceptor
    • Signed segment expiry — chưa auto re-fetch stream-info on media error
    • Ảnh bìa dùng img — chưa next/image + remotePatterns (chờ CORS CDN)
    • Business thiếu Vitest — claim gating cần test helper
    • ESLint chưa trong CI
    • Edge JWT playback (Cloudflare Worker) chưa triển khai — frontend vẫn API-centric


        14. Gợi ý cấu trúc khi chép vào báo cáo Word

    • 2.x.1 — Kiến trúc hai portal + monorepo (mục 1, 5)
    • 2.x.2 — Next.js và render/ISR (mục 2)
    • 2.x.3 — React, TypeScript, Tailwind, design system (mục 3, 4)
    • 2.x.4 — Auth, persona, claim gating (mục 6)
    • 2.x.5 — SEO catalog (mục 7)
    • 2.x.6 — Playback DASH và dash.js (mục 8)
    • 2.x.7 — Commerce UI (mục 9)
    • 2.x.8 — CI/CD và Docker frontend (mục 11)

Hình minh họa đề xuất: cây Provider; luồng stream-info → dash.js → R2; luồng auth đa persona business.


        15. Tài liệu liên quan trong repository

    • b2c-foundation-spec.md — spec token, auth, routes
    • b2c-frontend-foundation.md — runbook kiểm thử thủ công
    • playback.md — PlaybackProvider, DASH, seekbar
    • business-portal-tenancy.md — UX org và platform
    • commerce.md — surface billing UI
    • ci-cd.md — pipeline GitHub Actions
    • ads/frontend/ — yêu cầu sản phẩm
    • ads/auth/auth-index.md — auth và claims mirror backend
