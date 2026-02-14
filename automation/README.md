# 🤖 Scripts de Automação

Esta pasta contém scripts em PowerShell para facilitar tarefas comuns de desenvolvimento e manutenção do projeto Jullius Finanças.

## 📜 Scripts Disponíveis

### 1. `start-local.ps1`

**Objetivo:** Iniciar o ambiente de desenvolvimento local rapidamente.

- Abre janelas separadas para o Backend (.NET) e Frontend (Angular).
- Executa `dotnet run` e `npm start`.
- **Uso:** `.\start-local.ps1`
- **Opcional:** Use a flag `-Restore` para rodar `npm install` e `dotnet restore` antes de iniciar.

### 2. `docker-rebuild.ps1`

**Objetivo:** Forçar a reconstrução e reinicialização dos containers Docker.

- Derruba os containers atuais (`down`).
- Remove volumes antigos (limpeza).
- Reconstrói as imagens (`build`).
- Sobe os containers novamente em background (`up -d`).
- **Uso:** `.\docker-rebuild.ps1`

### 3. `import-card-transactions.ps1`

**Objetivo:** Importar transações de cartão de crédito em lote via API.

- Útil para popular dados de faturas antigas ou migração de dados.
- **Configuração:** Edite as variáveis `$apiBaseUrl`, `$cardId`, e `$token` no início do arquivo antes de rodar.
- **Uso:** `.\import-card-transactions.ps1`

### 4. `import-financial-transactions.ps1`

**Objetivo:** Importar transações financeiras gerais (receitas/despesas) em lote.

- Similar ao importador de cartões, mas para o fluxo de caixa diário.
- **Configuração:** Edite `$categoryId` e `$token` no arquivo.
- **Uso:** `.\import-financial-transactions.ps1`

---

## ⚠️ Notas Importantes de Segurança

- **NUNCA** commite esses scripts com tokens JWT reais ou senhas hardcoded.
- Os scripts de importação vêm com dados de exemplo (`EXEMPLO`). Ajuste para sua necessidade real, mas reverta ou não commite suas alterações com dados sensíveis.
