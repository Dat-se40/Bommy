FROM node:22-alpine AS node-builder

WORKDIR /backend

COPY package*.json ./
RUN npm ci

COPY tsconfig.json ./
COPY src ./src
RUN npm run build

FROM heroiclabs/nakama:3.22.0

COPY --from=node-builder /backend/build/index.js /nakama/data/modules/build/index.js
COPY local.yml /nakama/data/local.yml
