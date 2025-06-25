# Setup Azure Resources for Jullius Finanças
# Este script automatiza a configuração inicial dos recursos Azure e GitHub

param(
    [Parameter(Mandatory=$true)]
    [string]$GitHubOrg,
    
    [Parameter(Mandatory=$false)]
    [string]$GitHubRepo = "JulliusFinancas",
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "rg-jullius-prod",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "eastus2"
)

Write-Host "🚀 Iniciando configuração do Azure para Jullius Finanças" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green

# Verificar se está logado no Azure
Write-Host "`n📋 Verificando login no Azure..." -ForegroundColor Yellow
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "❌ Você não está logado no Azure. Executando 'az login'..." -ForegroundColor Red
    az login
    $account = az account show | ConvertFrom-Json
}

Write-Host "✅ Logado como: $($account.user.name)" -ForegroundColor Green
Write-Host "📌 Subscription: $($account.name) ($($account.id))" -ForegroundColor Cyan

# Variáveis
$subscriptionId = $account.id
$tenantId = $account.tenantId
$appName = "jullius-github-actions"

# Criar Resource Group
Write-Host "`n📦 Criando Resource Group..." -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroup | ConvertFrom-Json
if (-not $rgExists) {
    az group create --name $ResourceGroup --location $Location
    Write-Host "✅ Resource Group '$ResourceGroup' criado" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Resource Group '$ResourceGroup' já existe" -ForegroundColor Cyan
}

# Criar App Registration
Write-Host "`n🔐 Configurando Service Principal com OIDC..." -ForegroundColor Yellow

# Verificar se o app já existe
$existingApp = az ad app list --display-name $appName --query "[0]" | ConvertFrom-Json
if ($existingApp) {
    $appId = $existingApp.appId
    Write-Host "ℹ️  App Registration '$appName' já existe (ID: $appId)" -ForegroundColor Cyan
} else {
    $app = az ad app create --display-name $appName | ConvertFrom-Json
    $appId = $app.appId
    Write-Host "✅ App Registration criado (ID: $appId)" -ForegroundColor Green
}

# Criar Service Principal
$spExists = az ad sp show --id $appId 2>$null
if (-not $spExists) {
    az ad sp create --id $appId
    Write-Host "✅ Service Principal criado" -ForegroundColor Green
} else {
    Write-Host "ℹ️  Service Principal já existe" -ForegroundColor Cyan
}

# Atribuir role de Contributor
Write-Host "`n🔑 Atribuindo permissões..." -ForegroundColor Yellow
az role assignment create `
    --role "Contributor" `
    --assignee $appId `
    --subscription $subscriptionId `
    --scope "/subscriptions/$subscriptionId"
Write-Host "✅ Permissões de Contributor atribuídas" -ForegroundColor Green

# Configurar Federated Credentials
Write-Host "`n🔗 Configurando Federated Credentials para GitHub Actions..." -ForegroundColor Yellow

$federatedCreds = @(
    @{
        name = "github-main"
        subject = "repo:${GitHubOrg}/${GitHubRepo}:ref:refs/heads/main"
        description = "Deploy from main branch"
    },
    @{
        name = "github-pr"
        subject = "repo:${GitHubOrg}/${GitHubRepo}:pull_request"
        description = "Deploy from pull requests"
    }
)

foreach ($cred in $federatedCreds) {
    # Criar JSON como string com escape adequado para PowerShell
    $credJsonString = "{`"name`":`"$($cred.name)`",`"issuer`":`"https://token.actions.githubusercontent.com`",`"subject`":`"$($cred.subject)`",`"description`":`"$($cred.description)`",`"audiences`":[`"api://AzureADTokenExchange`"]}"

    $existingCred = az ad app federated-credential list --id $appId --query "[?name=='$($cred.name)']" | ConvertFrom-Json
    if ($existingCred.Count -eq 0) {
        # Usar arquivo temporário para evitar problemas de escape
        $tempFile = [System.IO.Path]::GetTempFileName()
        $credJsonString | Out-File -FilePath $tempFile -Encoding UTF8 -NoNewline
        try {
            az ad app federated-credential create --id $appId --parameters "@$tempFile"
            Write-Host "✅ Federated credential '$($cred.name)' criado" -ForegroundColor Green
        } finally {
            Remove-Item $tempFile -ErrorAction SilentlyContinue
        }
    } else {
        Write-Host "ℹ️  Federated credential '$($cred.name)' já existe" -ForegroundColor Cyan
    }
}

# Gerar senha para PostgreSQL
Write-Host "`n🔐 Gerando senha segura para PostgreSQL..." -ForegroundColor Yellow
# Usar método alternativo que não depende de System.Web
$chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*"
$pgPassword = -join ((1..32) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
Write-Host "✅ Senha gerada (mantenha em local seguro!)" -ForegroundColor Green

# Criar arquivo de secrets
Write-Host "`n📝 Criando arquivo de configuração..." -ForegroundColor Yellow
$secretsContent = @"
# GitHub Secrets Configuration
# Copie estes valores para os secrets do seu repositório GitHub

AZURE_CLIENT_ID=$appId
AZURE_TENANT_ID=$tenantId
AZURE_SUBSCRIPTION_ID=$subscriptionId
PG_ADMIN_PASSWORD=$pgPassword

# Estes serão preenchidos após o deploy da infraestrutura:
AZURE_STATIC_WEB_APPS_API_TOKEN=<será gerado após criar o Static Web App>
API_URL=<será definido após deploy do Web App>
STATIC_WEB_APP_URL=<será definido após deploy do Static Web App>
"@

$secretsFile = "github-secrets.txt"
$secretsContent | Out-File -FilePath $secretsFile -Encoding UTF8
Write-Host "✅ Arquivo de secrets criado em: $secretsFile" -ForegroundColor Green

# Instruções finais
Write-Host "`n✨ Configuração concluída!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host "`n📋 Próximos passos:" -ForegroundColor Yellow
Write-Host "1. Configure os secrets no GitHub usando os valores em: $secretsFile" -ForegroundColor White
Write-Host "2. Execute o workflow 'Infrastructure Deployment' no GitHub Actions" -ForegroundColor White
Write-Host "3. Após o deploy da infra, atualize os secrets API_URL e STATIC_WEB_APP_URL" -ForegroundColor White
Write-Host "4. Execute os workflows de deploy das aplicações" -ForegroundColor White

Write-Host "`n💡 Dica: Use o GitHub CLI para configurar os secrets automaticamente:" -ForegroundColor Cyan
Write-Host "   gh secret set AZURE_CLIENT_ID --body `"$appId`"" -ForegroundColor DarkGray
Write-Host "   gh secret set AZURE_TENANT_ID --body `"$tenantId`"" -ForegroundColor DarkGray
Write-Host "   gh secret set AZURE_SUBSCRIPTION_ID --body `"$subscriptionId`"" -ForegroundColor DarkGray
Write-Host "   gh secret set PG_ADMIN_PASSWORD --body `"$pgPassword`"" -ForegroundColor DarkGray

# Criar .gitignore para o arquivo de secrets
if (-not (Test-Path ".gitignore")) {
    "github-secrets.txt" | Out-File -FilePath ".gitignore" -Encoding UTF8
    Write-Host "`n⚠️  Arquivo .gitignore criado em infra/ para proteger os secrets" -ForegroundColor Yellow
} 