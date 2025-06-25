# 📚 Guia de Deploy - Jullius Finanças

Este guia fornece instruções detalhadas para configurar e executar o deploy da aplicação Jullius Finanças no Azure.

## 📋 Índice

1. [Pré-requisitos](#-pré-requisitos)
2. [Configuração do Azure OIDC](#-configuração-do-azure-oidc)
3. [Configuração dos Secrets no GitHub](#-configuração-dos-secrets-no-github)
4. [Deploy da Infraestrutura](#-deploy-da-infraestrutura)
5. [Deploy das Aplicações](#-deploy-das-aplicações)
6. [Monitoramento e Troubleshooting](#-monitoramento-e-troubleshooting)

## 🔧 Pré-requisitos

- Conta Azure com permissões para criar recursos
- Repositório GitHub com os códigos fonte
- Azure CLI instalado localmente
- GitHub CLI (opcional, mas recomendado)

## 🔐 Configuração do Azure OIDC

### Passo 1: Criar um Service Principal com OIDC

```bash
# 1. Faça login no Azure
az login

# 2. Defina variáveis
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RESOURCE_GROUP="rg-jullius-prod"
APP_NAME="jullius-github-actions"

# 3. Crie o App Registration
APP_ID=$(az ad app create --display-name $APP_NAME --query appId -o tsv)

# 4. Crie o Service Principal
SERVICE_PRINCIPAL_ID=$(az ad sp create --id $APP_ID --query id -o tsv)

# 5. Atribua permissões de Contributor ao Service Principal
az role assignment create \
  --role "Contributor" \
  --assignee $APP_ID \
  --subscription $SUBSCRIPTION_ID

# 6. Configure federated credentials para GitHub Actions
GITHUB_ORG="seu-usuario-ou-org"  # Substitua pelo seu usuário/organização
GITHUB_REPO="JulliusFinancas"    # Nome do repositório

# Para branch main
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'$GITHUB_ORG'/'$GITHUB_REPO':ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Para pull requests (opcional)
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-pr",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'$GITHUB_ORG'/'$GITHUB_REPO':pull_request",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Para ambientes específicos (production)
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-prod-env",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'$GITHUB_ORG'/'$GITHUB_REPO':environment:production",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# 7. Obtenha os IDs necessários
echo "AZURE_CLIENT_ID: $APP_ID"
echo "AZURE_TENANT_ID: $(az account show --query tenantId -o tsv)"
echo "AZURE_SUBSCRIPTION_ID: $SUBSCRIPTION_ID"
```

### Passo 2: Criar um Static Web App e obter o token de deploy

```bash
# 1. Crie o Static Web App (se ainda não existir)
STATIC_WEB_APP_NAME="jullius-spa-$(openssl rand -hex 4)"

az staticwebapp create \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --location "eastus2" \
  --sku Free

# 2. Obtenha o token de deploy
DEPLOYMENT_TOKEN=$(az staticwebapp secrets list \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "properties.apiKey" -o tsv)

echo "AZURE_STATIC_WEB_APPS_API_TOKEN: $DEPLOYMENT_TOKEN"
```

## 🔑 Configuração dos Secrets no GitHub

### Usando GitHub CLI

```bash
# Configure os secrets do repositório
gh secret set AZURE_CLIENT_ID --body "$APP_ID"
gh secret set AZURE_TENANT_ID --body "$(az account show --query tenantId -o tsv)"
gh secret set AZURE_SUBSCRIPTION_ID --body "$SUBSCRIPTION_ID"
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "$DEPLOYMENT_TOKEN"

# Gere uma senha segura para o PostgreSQL
PG_PASSWORD=$(openssl rand -base64 32)
gh secret set PG_ADMIN_PASSWORD --body "$PG_PASSWORD"

# Configure a URL da API (será atualizada após o deploy)
gh secret set API_URL --body "https://jullius-api-xxx.azurewebsites.net"
gh secret set STATIC_WEB_APP_URL --body "https://$STATIC_WEB_APP_NAME.azurestaticapps.net"
```

### Usando a Interface Web do GitHub

1. Navegue até **Settings** > **Secrets and variables** > **Actions**
2. Clique em **New repository secret**
3. Adicione os seguintes secrets:

| Nome do Secret | Descrição | Exemplo |
|----------------|-----------|---------|
| `AZURE_CLIENT_ID` | ID do App Registration | `12345678-1234-1234-1234-123456789012` |
| `AZURE_TENANT_ID` | ID do Tenant Azure | `87654321-4321-4321-4321-210987654321` |
| `AZURE_SUBSCRIPTION_ID` | ID da Subscription | `11111111-2222-3333-4444-555555555555` |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Token do Static Web App | `abc123...` |
| `PG_ADMIN_PASSWORD` | Senha do PostgreSQL | Senha forte gerada |
| `API_URL` | URL da API .NET | `https://jullius-api-xxx.azurewebsites.net` |
| `STATIC_WEB_APP_URL` | URL do Static Web App | `https://jullius-spa-xxx.azurestaticapps.net` |

## 🚀 Deploy da Infraestrutura

### Passo 1: Executar o workflow de infraestrutura

1. No GitHub, vá para **Actions**
2. Selecione **Infrastructure Deployment**
3. Clique em **Run workflow**
4. Preencha os parâmetros:
   - **Resource Group**: `rg-jullius-prod`
   - **Environment**: `production`
5. Clique em **Run workflow**

### Passo 2: Verificar o deploy

```bash
# Verifique os recursos criados
az resource list --resource-group $RESOURCE_GROUP --output table

# Teste a conectividade do PostgreSQL
az postgres server show \
  --resource-group $RESOURCE_GROUP \
  --name jullius-db-xxx
```

## 🌐 Deploy das Aplicações

### Deploy do Angular (Automático)

O deploy do Angular é executado automaticamente quando há push na branch `main` com alterações na pasta `client/`.

Para deploy manual:

1. Vá para **Actions** > **Deploy Angular App**
2. Clique em **Run workflow**
3. Selecione o ambiente e execute

### Deploy do .NET API (Automático)

O deploy da API é executado automaticamente quando há push na branch `main` com alterações na pasta `server/`.

Para deploy manual:

1. Vá para **Actions** > **Deploy .NET API**
2. Clique em **Run workflow**
3. Informe o Resource Group e ambiente
4. Execute o workflow

## 📊 Monitoramento e Troubleshooting

### Verificar logs dos workflows

```bash
# Listar execuções recentes
gh run list

# Ver logs de uma execução específica
gh run view <run-id> --log
```

### Verificar logs da aplicação no Azure

```bash
# Logs do Web App
az webapp log tail \
  --resource-group $RESOURCE_GROUP \
  --name jullius-api-xxx

# Métricas do Web App
az monitor metrics list \
  --resource $WEBAPP_ID \
  --metric "Http2xx,Http4xx,Http5xx" \
  --interval PT1H
```

### Comandos úteis de troubleshooting

```bash
# Reiniciar o Web App
az webapp restart \
  --resource-group $RESOURCE_GROUP \
  --name jullius-api-xxx

# Verificar configurações do Web App
az webapp config show \
  --resource-group $RESOURCE_GROUP \
  --name jullius-api-xxx

# Testar conexão com PostgreSQL
az postgres server firewall-rule list \
  --resource-group $RESOURCE_GROUP \
  --server-name jullius-db-xxx
```

## 🔄 Atualizações e Manutenção

### Atualizar secrets

```bash
# Atualizar a senha do PostgreSQL
NEW_PASSWORD=$(openssl rand -base64 32)
gh secret set PG_ADMIN_PASSWORD --body "$NEW_PASSWORD"

# Atualizar no Azure
az postgres server update \
  --resource-group $RESOURCE_GROUP \
  --name jullius-db-xxx \
  --admin-password "$NEW_PASSWORD"
```

### Backup do banco de dados

```bash
# Criar backup manual
az postgres server-backup create \
  --resource-group $RESOURCE_GROUP \
  --server-name jullius-db-xxx \
  --backup-name "manual-backup-$(date +%Y%m%d)"
```

## 📝 Notas Importantes

1. **Segurança**: Nunca commite secrets ou senhas no código
2. **Custos**: Monitore o uso dos recursos para evitar custos inesperados
3. **Backups**: Configure backups automáticos para o PostgreSQL
4. **Monitoramento**: Configure alertas no Azure Monitor
5. **Escalabilidade**: O plano Free tem limitações, considere upgrade para produção

## 🆘 Suporte

Em caso de problemas:

1. Verifique os logs dos workflows no GitHub Actions
2. Consulte os logs das aplicações no Azure Portal
3. Verifique a documentação oficial do Azure
4. Abra uma issue no repositório

---

**Última atualização**: Janeiro 2025
