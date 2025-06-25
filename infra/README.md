# 🏗️ Infraestrutura - Jullius Finanças

Esta pasta contém todos os arquivos de Infraestrutura como Código (IaC) para o projeto Jullius Finanças.

## 📁 Estrutura

```text
infra/
├── azuredeploy.json              # ARM Template principal
├── azuredeploy.parameters.json   # Parâmetros do ARM Template
├── setup-azure-resources.ps1     # Script de configuração inicial
├── .gitignore                    # Ignora arquivos sensíveis
└── README.md                     # Este arquivo
```

## 🚀 Quick Start

### 1. Configuração Inicial (Execute uma única vez)

```powershell
# Execute o script de setup com seu usuário/organização do GitHub
.\setup-azure-resources.ps1 -GitHubOrg "seu-usuario-github"
```

### 2. Deploy Manual via Azure CLI

```bash
# Login no Azure
az login

# Criar Resource Group
az group create --name rg-jullius-prod --location eastus

# Deploy do ARM Template
az deployment group create \
  --resource-group rg-jullius-prod \
  --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json \
  --parameters postgresqlAdminPassword="SuaSenhaSegura123!"
```

## 📋 Recursos Criados

| Recurso | Tipo | SKU | Descrição |
|---------|------|-----|-----------|
| App Service Plan | Linux | F1 (Free) | Hospeda a API .NET |
| Web App | .NET 9 | - | API Backend |
| Static Web App | - | Free | Frontend Angular |
| PostgreSQL | Single Server | Basic | Banco de dados |

## 🔧 Parâmetros Customizáveis

- `webAppName`: Nome do Web App (padrão: auto-gerado)
- `staticWebAppName`: Nome do Static Web App (padrão: auto-gerado)
- `postgresqlServerName`: Nome do servidor PostgreSQL (padrão: auto-gerado)
- `location`: Região do Azure (padrão: mesma do Resource Group)

## 🔐 Segurança

- As senhas devem ser armazenadas como secrets no GitHub
- Use o Azure Key Vault para produção
- Habilite o backup automático do PostgreSQL
- Configure firewall rules apropriadas

## 📊 Monitoramento

Após o deploy, configure:

- Application Insights para a API
- Alertas no Azure Monitor
- Log Analytics Workspace

## 💰 Estimativa de Custos

Com as configurações atuais (tiers gratuitos/básicos):

- **App Service Plan F1**: Gratuito
- **Static Web App Free**: Gratuito
- **PostgreSQL Basic**: ~$25-35/mês
- **Total estimado**: ~$25-35/mês

> ⚠️ Para produção, considere fazer upgrade para tiers pagos para melhor performance e SLA.

## 🔄 Atualizações

Para atualizar a infraestrutura:

1. Modifique o `azuredeploy.json`
2. Teste em ambiente de desenvolvimento
3. Execute o deploy via GitHub Actions ou CLI

## 📚 Referências

- [ARM Templates Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/)
- [Azure Web Apps](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Static Web Apps](https://docs.microsoft.com/en-us/azure/static-web-apps/)
- [Azure Database for PostgreSQL](https://docs.microsoft.com/en-us/azure/postgresql/)
