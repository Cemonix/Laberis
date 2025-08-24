# Use Node.js 24 for building
FROM node:24-alpine AS build-env
WORKDIR /app

# Build arguments for environment variables
ARG VITE_API_BASE_URL=http://localhost:5000/api
ARG VITE_APP_NAME=Laberis
ARG VITE_APP_VERSION=0.1.0

# Set environment variables
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_APP_NAME=$VITE_APP_NAME
ENV VITE_APP_VERSION=$VITE_APP_VERSION

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Run type check first, then build the application
RUN npm run build

# Use nginx-unprivileged for serving static files (runs as non-root by default)
FROM nginxinc/nginx-unprivileged:alpine AS production

# Switch to root temporarily to clean and copy files
USER root

# Clean the html directory and copy built application
RUN rm -rf /usr/share/nginx/html/*
COPY --from=build-env /app/dist /usr/share/nginx/html/

# Fix ownership for nginx user
RUN chown -R nginx:nginx /usr/share/nginx/html

# Switch back to nginx user
USER nginx

WORKDIR /usr/share/nginx/html

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port (nginx-unprivileged uses 8080)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/ || exit 1