# syntax=docker/dockerfile:1
# Build from repository root: docker build -f frontend/consumer/Dockerfile frontend

FROM node:22-alpine AS base
RUN corepack enable
WORKDIR /app

FROM base AS deps
COPY packages/catalog-text/package.json packages/catalog-text/
COPY consumer/package.json consumer/pnpm-lock.yaml consumer/pnpm-workspace.yaml consumer/
COPY packages/catalog-text packages/catalog-text
WORKDIR /app/consumer
RUN --mount=type=cache,target=/root/.local/share/pnpm/store,sharing=locked \
    pnpm install --frozen-lockfile

FROM base AS builder
COPY packages/catalog-text packages/catalog-text
COPY consumer consumer
WORKDIR /app/consumer
COPY --from=deps /app/consumer/node_modules ./node_modules
ARG NEXT_PUBLIC_API_BASE_URL=https://api.example.com
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
RUN pnpm build

FROM node:22-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
ENV PORT=3000
RUN addgroup -S nodejs && adduser -S nextjs -G nodejs
COPY --from=builder /app/consumer/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/consumer/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/consumer/.next/static ./.next/static
USER nextjs
EXPOSE 3000
CMD ["node", "server.js"]
