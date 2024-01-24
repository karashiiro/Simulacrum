# Build this container image from the monorepo root: docker build . -f ./src/simulacrum-cloud-api/Dockerfile -t simulacrum-cloud-api
FROM node:18-alpine as base

FROM base AS builder
# Check https://github.com/nodejs/docker-node/tree/b4117f9333da4138b03a546ec926ef50a31506c3#nodealpine to understand why libc6-compat might be needed.
RUN apk add --no-cache libc6-compat
RUN apk update
RUN corepack enable
# Set working directory
WORKDIR /app
RUN yarn global add turbo
COPY . .
RUN turbo prune simulacrum-cloud-api --docker

# Add lockfile and package.json's of isolated subworkspace
FROM base AS installer
RUN apk add --no-cache libc6-compat
RUN apk update
RUN corepack enable
WORKDIR /app

# First install dependencies (as they change less often)
COPY .gitignore .gitignore
COPY --from=builder /app/out/json/ .
COPY --from=builder /app/out/yarn.lock ./yarn.lock
RUN yarn install

# Build the project and its dependencies
COPY --from=builder /app/out/full/ .
COPY turbo.json turbo.json

RUN yarn turbo build --filter=simulacrum-cloud-api...

# Do a production install including only the dependencies we need for running this package
FROM base AS trimmed
WORKDIR /app
RUN apk add --no-cache libc6-compat
RUN apk update
RUN corepack enable
RUN corepack install --global yarn@4.0.2

COPY .gitignore .gitignore
COPY --from=builder /app/out/json/ .
COPY --from=builder /app/out/yarn.lock ./yarn.lock

RUN yarn workspaces focus simulacrum-cloud-api --production

FROM base AS runner
WORKDIR /app

# Don't run production as root
RUN addgroup --system --gid 1001 nestjs
RUN adduser --system --uid 1001 nestjs
USER nestjs
COPY --from=trimmed /app/node_modules node_modules
COPY --from=installer /app/src/simulacrum-cloud-api/dist/apps/hostctl/main.js .

EXPOSE 3000
ENV NODE_ENV production
HEALTHCHECK --interval=10m --timeout=5s \
    CMD curl -f http://localhost:3000/health || exit 1

CMD node main.js