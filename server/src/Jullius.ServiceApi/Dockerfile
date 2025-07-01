# Estágio de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copiar arquivos de projeto primeiro para melhor cache
COPY Jullius.ServiceApi/Jullius.ServiceApi.csproj Jullius.ServiceApi/
COPY Jullius.Data/Jullius.Data.csproj Jullius.Data/
COPY Jullius.Domain/Jullius.Domain.csproj Jullius.Domain/

# Restaurar dependências
RUN dotnet restore Jullius.ServiceApi/Jullius.ServiceApi.csproj

# Copiar o restante dos arquivos
COPY . .

# Publicar a aplicação
RUN dotnet publish Jullius.ServiceApi/Jullius.ServiceApi.csproj \
    -c Release \
    -o out \
    --no-restore

# Estágio de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Instalar dependências necessárias para Azure
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copiar arquivos publicados
COPY --from=build /app/out ./

# Configurar variáveis de ambiente para Azure
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080

# Expor porta
EXPOSE 8080

# Health check para Docker
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Ponto de entrada
ENTRYPOINT ["dotnet", "Jullius.ServiceApi.dll"] 