# syntax=docker/dockerfile:1
# Build from repository root: docker build -f frontend/business/Dockerfile frontend

FROM node:22-alpine AS base
RUN corepack enable
WORKDIR /app

FROM base AS deps
COPY packages/catalog-text/package.json packages/catalog-text/
COPY business/package.json business/pnpm-lock.yaml business/pnpm-workspace.yaml business/
COPY packages/catalog-text packages/catalog-text
WORKDIR /app/business
RUN --mount=type=cache,target=/root/.local/share/pnpm/store,sharing=locked \
    pnpm install --frozen-lockfile

FROM base AS builder
COPY packages/catalog-text packages/catalog-text
COPY business business
WORKDIR /app/business
COPY --from=deps /app/business/node_modules ./node_modules
ARG NEXT_PUBLIC_API_BASE_URL=https://api.example.com
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
RUN pnpm build

FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
ENV PORT=3001
RUN addgroup -S nodejs && adduser -S nextjs -G nodejs \
    && rm -rf /usr/local/lib/node_modules/npm /usr/local/bin/npm /usr/local/bin/npx
COPY --from=builder /app/business/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/business/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/business/.next/static ./.next/static
USER nextjs
EXPOSE 3001
CMD ["node", "server.js"]
