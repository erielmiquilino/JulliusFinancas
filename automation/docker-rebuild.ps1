# Script para reconstruir e executar o container Docker do Jullius Finanças
$ErrorActionPreference = "Continue"

Write-Host "=== Iniciando Processo Docker: Jullius Finanças ===" -ForegroundColor Cyan

# Retorna para a raiz do projeto para executar o docker build
Push-Location "$PSScriptRoot\.."

# 1. Limpeza
Write-Host "Removendo container e imagem antigos..." -ForegroundColor Yellow
docker stop jullius-financas 2>$null
docker rm jullius-financas 2>$null
docker rmi jullius-financas:latest 2>$null

# 2. Build da Imagem
Write-Host "Buildando nova imagem (tag: latest)..." -ForegroundColor Cyan
docker build -t jullius-financas:latest .

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro crítico: Falha ao construir a imagem Docker." -ForegroundColor Red
    Pop-Location
    exit 1
}

# 3. Execução do Container
Write-Host "Iniciando container 'jullius-financas' na porta 80..." -ForegroundColor Green
docker run -p 80:80 --name jullius-financas -d jullius-financas:latest

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSucesso! Aplicação rodando em http://localhost" -ForegroundColor Green
} else {
    Write-Host "`nErro: Falha ao iniciar o container." -ForegroundColor Red
}

Pop-Location
