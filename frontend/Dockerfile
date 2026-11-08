# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

COPY opspulse-web/package.json opspulse-web/package-lock.json ./
RUN npm ci

COPY opspulse-web/ ./
# The API is reached through nginx at runtime; build-time calls are proxied by the dev config only.
RUN npm run build

# ---- Runtime stage: nginx serving the SPA and proxying API/SignalR ----
FROM nginx:1.27-alpine AS final
COPY opspulse-web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
