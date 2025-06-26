# configure-github-secrets.ps1
# Este script obtém os outputs da infraestrutura do Azure e os configura como secrets no GitHub.

param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubOrg,
    
    [Parameter(Mandatory=$false)]
    [string]$GitHubRepo = "JulliusFinancas",
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "rg-jullius-prod"
)

# --- PASSO 1: Verificação de Pré-requisitos ---
Write-Host "==================================================================" -ForegroundColor Green
Write-Host "🚀 Script para Configurar Secrets do GitHub Pós-Deploy da Infra" -ForegroundColor Green
Write-Host "==================================================================" -ForegroundColor Green

Write-Host "`n📋 Verificação de Pré-requisitos:" -ForegroundColor Yellow
Write-Host "1. Você já executou o script 'infra/setup-azure-resources.ps1'?"
Write-Host "2. Você já executou o pipeline 'Infrastructure Deployment' (.github/workflows/infra-deploy.yml) com sucesso no GitHub Actions?"

$confirmation = ""
while ($confirmation.ToLower() -ne 's' -and $confirmation.ToLower() -ne 'n') {
    $confirmation = Read-Host -Prompt "`n-> Responda com 's' (sim) ou 'n' (não) para continuar"
}

if ($confirmation.ToLower() -eq 'n') {
    Write-Host "`n❌ Ação cancelada. Por favor, complete os passos de 1 e 2 antes de executar este script." -ForegroundColor Red
    return
}

Write-Host "✅ Pré-requisitos confirmados. Prosseguindo..." -ForegroundColor Green


# --- PASSO 2: Verificação de Ferramentas e Login ---
Write-Host "`n🛠️ Verificando ferramentas necessárias (Azure CLI e GitHub CLI)..." -ForegroundColor Yellow

# Verificar se a Azure CLI está instalada
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Azure CLI não encontrada. Por favor, instale-a antes de continuar." -ForegroundColor Red
    return
}

# Verificar se a GitHub CLI está instalada
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "❌ GitHub CLI não encontrada. Por favor, instale-a ('winget install GitHub.cli') antes de continuar." -ForegroundColor Red
    return
}

# Verificar login no Azure
$account = az account show 2>$null
if (-not $account) {
    Write-Host "⏳ Realizando login no Azure..." -ForegroundColor Cyan
    az login
}
Write-Host "✅ Logado no Azure como: $((az account show | ConvertFrom-Json).user.name)" -ForegroundColor Green

# Verificar login no GitHub CLI
$ghAuthStatus = gh auth status 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "⏳ Realizando login no GitHub CLI..." -ForegroundColor Cyan
    gh auth login
}
Write-Host "✅ Logado no GitHub CLI." -ForegroundColor Green


# --- PASSO 3: Obter Valores da Infraestrutura no Azure ---
Write-Host "`n🔍 Obtendo informações da infraestrutura do Azure..." -ForegroundColor Yellow

# Obter nome e URL do Web App
Write-Host "   - Buscando API App Service..."
$webApp = az webapp list --resource-group $ResourceGroup --query "[?contains(name, 'jullius-api')].{name:name, url:defaultHostName}" | ConvertFrom-Json
if (-not $webApp) {
    Write-Host "❌ Nenhum App Service com 'jullius-api' no nome foi encontrado no resource group '$ResourceGroup'." -ForegroundColor Red
    return
}
$apiUrl = "https://$($webApp.url)"
Write-Host "   ✅ API URL: $apiUrl" -ForegroundColor Cyan

# Obter nome, URL e token do Static Web App
Write-Host "   - Buscando Static Web App..."
$staticWebApp = az staticwebapp list --resource-group $ResourceGroup --query "[0].{name:name, url:defaultHostname}" | ConvertFrom-Json
if (-not $staticWebApp) {
    Write-Host "❌ Nenhum Static Web App encontrado no resource group '$ResourceGroup'." -ForegroundColor Red
    return
}
$staticWebAppUrl = "https://$($staticWebApp.url)"
Write-Host "   ✅ Static Web App URL: $staticWebAppUrl" -ForegroundColor Cyan

Write-Host "   - Obtendo token de deploy do Static Web App..."
$staticWebAppToken = az staticwebapp secrets list --name $staticWebApp.name --resource-group $ResourceGroup --query "properties.apiKey" -o tsv
if (-not $staticWebAppToken) {
    Write-Host "❌ Não foi possível obter o token de deploy (API Key) para o Static Web App '$($staticWebApp.name)'." -ForegroundColor Red
    return
}
Write-Host "   ✅ Token de deploy obtido com sucesso." -ForegroundColor Cyan


# --- PASSO 4: Configurar Secrets no GitHub ---
Write-Host "`n🔐 Configurando secrets no repositório GitHub '$($GitHubOrg)/$($GitHubRepo)'..." -ForegroundColor Yellow

try {
    Write-Host "   - Configurando API_URL..."
    gh secret set API_URL --body "$apiUrl" --repo "${GitHubOrg}/${GitHubRepo}"
    
    Write-Host "   - Configurando STATIC_WEB_APP_URL..."
    gh secret set STATIC_WEB_APP_URL --body "$staticWebAppUrl" --repo "${GitHubOrg}/${GitHubRepo}"

    Write-Host "   - Configurando AZURE_STATIC_WEB_APPS_API_TOKEN..."
    gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "$staticWebAppToken" --repo "${GitHubOrg}/${GitHubRepo}"
    
    Write-Host "`n✨ Sucesso! Os secrets foram configurados no GitHub." -ForegroundColor Green
    Write-Host "   Agora você pode executar os pipelines de deploy para o Angular e .NET." -ForegroundColor White

} catch {
    Write-Host "`n❌ Ocorreu um erro ao configurar os secrets no GitHub." -ForegroundColor Red
    Write-Host $_.Exception.Message
}
