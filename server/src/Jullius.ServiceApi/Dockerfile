# Estágio de build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copiar todos os arquivos .csproj e .sln (se houver no nível do src)
# Isso ajuda a restaurar as dependências de todos os projetos corretamente.
# Se sua solution file (.sln) estiver em /server/src, copie-a também.
COPY *.sln ./
COPY */*.csproj ./

# Criar diretórios para cada projeto antes de copiar os .csproj
# Isso é necessário para que o restore encontre os arquivos no lugar certo
# Assumindo que seus projetos estão em subdiretórios como Jullius.ServiceApi/, Jullius.Data/, etc.
RUN find . -name "*.csproj" -exec dirname {} \; | xargs -I {} mkdir -p {}

# Copiar os arquivos .csproj para seus respectivos diretórios
# Exemplo, se tiver Jullius.Data/Jullius.Data.csproj e Jullius.ServiceApi/Jullius.ServiceApi.csproj
COPY Jullius.ServiceApi/Jullius.ServiceApi.csproj Jullius.ServiceApi/
COPY Jullius.Data/Jullius.Data.csproj Jullius.Data/
COPY Jullius.Domain/Jullius.Domain.csproj Jullius.Domain/

# Restaurar dependências para o projeto principal (ou para a solution se copiou o .sln)
RUN dotnet restore Jullius.ServiceApi/Jullius.ServiceApi.csproj

# Copiar o restante dos arquivos da aplicação (todo o código fonte de todos os projetos)
COPY . .

# Publicar o projeto da API
RUN dotnet publish Jullius.ServiceApi/Jullius.ServiceApi.csproj -c Release -o out

# Estágio de runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

EXPOSE 8080

# Definir o ponto de entrada para executar a aplicação
ENTRYPOINT ["dotnet", "Jullius.ServiceApi.dll"] 