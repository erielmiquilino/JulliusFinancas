# docker build -t jullius-financas:latest . --no-cache && docker run -p 80:80 --name jullius-financas -d -e ASPNETCORE_ENVIRONMENT=Production jullius-financas:latest
# ============================================
# Stage 1: Frontend Build (Angular)
# ============================================
FROM node:slim AS frontend-build
WORKDIR /app/client

# Copiar package files
COPY client/package*.json ./

# Instalar dependências
RUN npm ci --legacy-peer-deps

# Copiar código fonte do frontend
COPY client/ ./

# Build da aplicação Angular
RUN npm run build

# ============================================
# Stage 2: Backend Build (.NET)
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS backend-build
WORKDIR /app

# Copiar arquivos .csproj primeiro para melhor cache
COPY server/src/Jullius.ServiceApi/Jullius.ServiceApi.csproj server/src/Jullius.ServiceApi/
COPY server/src/Jullius.Data/Jullius.Data.csproj server/src/Jullius.Data/
COPY server/src/Jullius.Domain/Jullius.Domain.csproj server/src/Jullius.Domain/

# Restaurar dependências
RUN dotnet restore server/src/Jullius.ServiceApi/Jullius.ServiceApi.csproj

# Copiar o restante dos arquivos do backend (mantendo estrutura)
COPY server/src/Jullius.ServiceApi/ server/src/Jullius.ServiceApi/
COPY server/src/Jullius.Data/ server/src/Jullius.Data/
COPY server/src/Jullius.Domain/ server/src/Jullius.Domain/

# Publicar aplicação
RUN dotnet publish server/src/Jullius.ServiceApi/Jullius.ServiceApi.csproj \
    -c Release \
    -o /app/out \
    --no-restore

# ============================================
# Stage 3: Runtime (Nginx + .NET)
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Instalar Nginx e dependências
RUN apt-get update && apt-get install -y \
    nginx \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Configurar diretórios e Nginx
WORKDIR /app
RUN mkdir -p /app/api /usr/share/nginx/html \
    && rm -f /etc/nginx/sites-enabled/default

# Copiar artefatos do frontend
COPY --from=frontend-build /app/client/dist/jullius-app/browser /usr/share/nginx/html

# Copiar artefatos do backend
COPY --from=backend-build /app/out /app/api

# Copiar configurações
COPY docker/nginx.conf /etc/nginx/sites-available/jullius
RUN ln -s /etc/nginx/sites-available/jullius /etc/nginx/sites-enabled/jullius
COPY docker/entrypoint.sh /docker/entrypoint.sh

# Tornar script executável
RUN chmod +x /docker/entrypoint.sh

# Variáveis de ambiente
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Expor porta 80 (Nginx)
EXPOSE 80

# Healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=90s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Ponto de entrada
ENTRYPOINT ["/docker/entrypoint.sh"]
