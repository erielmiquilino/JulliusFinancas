# Configuração do Bot de Telegram — Jullius Finanças

O Jullius Finanças oferece um assistente de Telegram com inteligência artificial
(Google Gemini) capaz de registrar despesas, compras em cartão e responder
consultas financeiras — tudo por linguagem natural.

Este guia cobre **todo o processo** de criação, configuração e ativação do bot.

---

## Índice

1. [Visão Geral da Arquitetura](#1-visão-geral-da-arquitetura)
2. [Pré-requisitos](#2-pré-requisitos)
3. [Criar o Bot no Telegram](#3-criar-o-bot-no-telegram)
4. [Obter a Chave do Google Gemini](#4-obter-a-chave-do-google-gemini)
5. [Configurar as Chaves no Jullius](#5-configurar-as-chaves-no-jullius)
6. [Registrar o Webhook](#6-registrar-o-webhook)
7. [Testar a Integração](#7-testar-a-integração)
8. [Comandos Suportados](#8-comandos-suportados)
9. [Exemplos de Uso](#9-exemplos-de-uso)
10. [Segurança e Criptografia](#10-segurança-e-criptografia)
11. [Configuração em Produção (Docker)](#11-configuração-em-produção-docker)
12. [Troubleshooting](#12-troubleshooting)
13. [Referência Técnica](#13-referência-técnica)

---

## 1. Visão Geral da Arquitetura

```text
Telegram ──webhook──▶ /api/telegram/webhook/{secret}
                              │
                     TelegramBotService
                              │
                    ConversationOrchestrator   ◀── ConversationStateStore (in-memory)
                        │           │
              GeminiAssistantService │
              (classifica intenção)  │
                                    ▼
                          IIntentHandler
                     ┌────────┼────────┐
              CreateExpense  CardPurchase  FinancialConsulting
```

**Fluxo resumido:**

1. O usuário envia uma mensagem ao bot no Telegram.
2. O Telegram faz um POST no webhook configurado.
3. O `TelegramBotService` valida a autorização do chat.
4. O `ConversationOrchestrator` gerencia o estado da conversa.
5. O `GeminiAssistantService` classifica a intenção via IA.
6. O handler correspondente executa a ação (criar despesa, compra no cartão ou
   responder consulta financeira).

---

## 2. Pré-requisitos

Antes de configurar o bot, você precisa:

- **Backend do Jullius Finanças rodando** (local ou em produção)
- **Banco de dados PostgreSQL** com as migrations aplicadas
- **Conta no Telegram** para criar o bot
- **Conta Google** para gerar a chave da API Gemini
- **URL pública com HTTPS** para o webhook (em produção). Para desenvolvimento
  local, use um túnel como [ngrok](https://ngrok.com) ou
  [Cloudflare Tunnel](https://developers.cloudflare.com/cloudflare-one/connections/connect-networks/)

---

## 3. Criar o Bot no Telegram

### 3.1. Abrir o BotFather

1. No Telegram, procure por **@BotFather** ou acesse
   [t.me/BotFather](https://t.me/BotFather).
2. Inicie uma conversa com `/start`.

### 3.2. Criar um Novo Bot

1. Envie o comando `/newbot`.
2. Escolha um **nome de exibição** (exemplo: `Jullius Finanças`).
3. Escolha um **username** que termine em `bot` (exemplo: `jullius_financas_bot`).
4. O BotFather retornará um **token** no formato:

   ```text
   1234567890:ABCdefGHIjklMNOpqrSTUvwxYZ
   ```

5. **Copie e guarde** esse token com segurança. Não o compartilhe publicamente.

### 3.3. Configurações Opcionais do Bot

Ainda no BotFather, você pode personalizar o bot:

```text
/setdescription   → Descrição exibida no perfil do bot
/setabouttext     → Texto "Sobre" do bot
/setuserpic       → Foto de perfil do bot
/setcommands      → Menu de comandos (veja abaixo)
```

**Comandos sugeridos** para `/setcommands`:

```text
start - Exibir menu de ajuda
ajuda - Exibir menu de ajuda
cancelar - Cancelar operação atual
```

### 3.4. Obter seu Chat ID

Para restringir o bot a apenas um usuário (recomendado para finanças pessoais):

1. Envie qualquer mensagem para o bot recém-criado.
2. Acesse no navegador:

   ```text
   https://api.telegram.org/bot<SEU_TOKEN>/getUpdates
   ```

3. Na resposta JSON, localize o campo `"chat": { "id": 1234567890 }`.
4. **Copie o número** — este é o seu **Chat ID**.

> **Nota:** Se a resposta estiver vazia, envie outra mensagem ao bot e tente
> novamente.

---

## 4. Obter a Chave do Google Gemini

O bot usa a API do **Google Gemini** (modelo `gemini-3-flash-preview`) para
classificar intenções e gerar respostas financeiras inteligentes.

### 4.1. Acessar o Google AI Studio

1. Acesse [aistudio.google.com](https://aistudio.google.com).
2. Faça login com sua conta Google.

### 4.2. Gerar uma API Key

1. No menu lateral, clique em **"Get API Key"** (ou "Obter chave de API").
2. Clique em **"Create API Key"**.
3. Selecione ou crie um projeto do Google Cloud.
4. **Copie a chave gerada** (formato: `AIzaSy...`).

### 4.3. Limites e Custos

- O plano gratuito inclui uma cota generosa para uso pessoal.
- Consulte a [página de preços do Gemini](https://ai.google.dev/pricing) para
  detalhes atualizados.

---

## 5. Configurar as Chaves no Jullius

Existem duas formas de cadastrar as chaves: pela **interface web** ou pela
**API REST**. A interface web é o caminho recomendado.

### 5.1. Via Interface Web (Recomendado)

1. Acesse o Jullius Finanças no navegador (exemplo: `http://localhost:4200`).
2. Faça login com suas credenciais.
3. Navegue até **Configurações** (`/settings`) no menu lateral.
4. Preencha os campos na seção **Telegram Bot**:

   | Campo | Valor |
   |---|---|
   | Token do Bot | O token recebido do BotFather |
   | Chat ID Autorizado | Seu Chat ID numérico |

5. Preencha o campo na seção **Google Gemini**:

   | Campo | Valor |
   |---|---|
   | Chave API Gemini | A chave do Google AI Studio |

6. Clique em **Salvar** para cada configuração.
7. Use os botões **Testar Conexão** para validar cada chave.

### 5.2. Via API REST

Se preferir configurar via API (útil para automação ou ambientes sem frontend):

```bash
# Definir o token base da API
API_URL="http://localhost:8081/api/BotConfiguration"
AUTH_TOKEN="seu_jwt_token"

# 1. Configurar Token do Bot Telegram
curl -X PUT "$API_URL/TelegramBotToken" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"value": "1234567890:ABCdefGHIjklMNOpqrSTUvwxYZ", "description": "Token do bot Telegram"}'

# 2. Configurar Chat ID Autorizado
curl -X PUT "$API_URL/TelegramAuthorizedChatId" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"value": "1234567890", "description": "Chat ID do usuário autorizado"}'

# 3. Configurar Chave do Gemini
curl -X PUT "$API_URL/GeminiApiKey" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"value": "AIzaSy...", "description": "Chave API Google Gemini"}'
```

### 5.3. Chaves de Configuração

O sistema utiliza quatro chaves armazenadas no banco de dados:

| Chave | Obrigatória | Descrição |
|---|---|---|
| `TelegramBotToken` | Sim | Token de autenticação do bot (do BotFather) |
| `TelegramAuthorizedChatId` | Recomendada | Restringe mensagens a um único chat. Se não definida, qualquer usuário pode usar o bot |
| `TelegramWebhookSecret` | Auto-gerada | Segredo na URL do webhook. Gerada automaticamente no registro do webhook |
| `GeminiApiKey` | Sim | Chave da API Google Gemini para classificação por IA |

> **Importante:** Todos os valores são criptografados no banco de dados via
> ASP.NET Core Data Protection. A API nunca expõe os valores armazenados em
> listagens.

---

## 6. Registrar o Webhook

O webhook é o mecanismo pelo qual o Telegram envia mensagens para o seu
servidor. Você precisa de uma **URL pública com HTTPS**.

### 6.1. Para Desenvolvimento Local

Use o [ngrok](https://ngrok.com) para criar um túnel:

```bash
# Instalar ngrok (se ainda não tiver)
# https://ngrok.com/download

# Criar túnel para a porta da API
ngrok http 8081
```

O ngrok fornecerá uma URL como `https://abc123.ngrok-free.app`.

### 6.2. Registrar via Interface Web

1. Na página **Configurações** (`/settings`), na seção **Webhook**:
2. Informe a URL base (exemplo: `https://abc123.ngrok-free.app` ou
   `https://seu-dominio.com`).
3. Clique em **Registrar Webhook**.
4. O sistema irá:
   - Gerar automaticamente um `TelegramWebhookSecret` (se ainda não existir).
   - Construir a URL final: `https://seu-dominio.com/api/telegram/webhook/{secret}`.
   - Chamar a API do Telegram para registrar o webhook.

### 6.3. Registrar via API REST

```bash
curl -X POST "$API_URL/register-webhook" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"baseUrl": "https://seu-dominio.com"}'
```

**Resposta de sucesso:**

```json
{
  "success": true,
  "webhookUrl": "https://seu-dominio.com/api/telegram/webhook/a1b2c3d4..."
}
```

### 6.4. Verificar o Webhook

Para confirmar que o webhook está ativo:

```bash
curl "https://api.telegram.org/bot<SEU_TOKEN>/getWebhookInfo"
```

A resposta deve conter `"url": "https://..."` e `"pending_update_count": 0`.

---

## 7. Testar a Integração

### 7.1. Testar Conexão do Bot

Na interface web, use o botão **Testar Conexão Telegram** na página de
Configurações. Internamente, ele chama:

```bash
# Via API
curl -X POST "$API_URL/test-telegram" \
  -H "Authorization: Bearer $AUTH_TOKEN"
```

**Retorno esperado:** informações do bot (`id`, `first_name`, `username`).

### 7.2. Testar Conexão do Gemini

Use o botão **Testar Conexão Gemini** ou:

```bash
curl -X POST "$API_URL/test-gemini" \
  -H "Authorization: Bearer $AUTH_TOKEN"
```

**Retorno esperado:** lista de modelos disponíveis na API.

### 7.3. Testar de Ponta a Ponta

1. Abra o Telegram e vá até o chat do seu bot.
2. Envie: `Gastei 50 reais de almoço em alimentação`.
3. O bot deve responder pedindo confirmação com os dados extraídos.
4. Responda `sim` para confirmar o registro.

---

## 8. Comandos Suportados

O bot responde aos seguintes comandos:

| Comando | Descrição |
|---|---|
| `/start` | Exibe mensagem de boas-vindas e instruções de uso |
| `/ajuda` ou `/help` | Exibe o menu de ajuda com exemplos |
| `/cancelar` ou `/cancel` | Cancela a operação em andamento |
| `/reset` | Reseta o estado da conversa atual |

---

## 9. Exemplos de Uso

### 9.1. Registrar Despesa Simples

```text
Usuário: Gastei 45 de almoço em alimentação
Bot:     📝 Confirma o lançamento?
         💸 Almoço — R$ 45,00 em Alimentação
         ⏳ Pendente
         Responda sim para confirmar ou não para cancelar.
Usuário: sim
Bot:     ✅ Despesa registrada com sucesso!
```

### 9.2. Registrar Múltiplas Despesas

```text
Usuário: Lance 22,50 de almoço em essenciais e 79 de carregador em não planejado
Bot:     📝 Confirma 2 lançamentos?
         1. 💸 Almoço — R$ 22,50 em Essenciais
         2. 💸 Carregador — R$ 79,00 em Não planejado
         Responda sim para confirmar ou não para cancelar.
```

### 9.3. Registrar Compra no Cartão com Parcelas

```text
Usuário: Comprei um notebook de 3000 em 10x no nubank
Bot:     📝 Confirma o lançamento?
         💳 Notebook — R$ 3.000,00 no Nubank (10x de R$ 300,00)
         Responda sim para confirmar ou não para cancelar.
```

### 9.4. Despesa já Paga

```text
Usuário: Paguei 120 de internet em essenciais, já pago
Bot:     📝 Confirma o lançamento?
         💸 Internet — R$ 120,00 em Essenciais
         ✅ Pago
```

### 9.5. Consulta Financeira

```text
Usuário: Como estão meus gastos esse mês?
Bot:     📊 Análise Financeira — Fevereiro 2026
         Total gasto: R$ 2.450,00
         Orçamento restante: R$ 550,00
         ...
```

---

## 10. Segurança e Criptografia

### 10.1. Armazenamento de Segredos

- **Nenhum segredo** (token do bot, API key) fica em arquivos de configuração
  (`appsettings.json`) nem em variáveis de ambiente.
- Todos os valores sensíveis são armazenados na tabela `BotConfiguration` do
  banco de dados, **criptografados** via ASP.NET Core Data Protection.
- O propósito de criptografia é `"Jullius.BotConfiguration.Encryption"`.

### 10.2. Proteção do Webhook

- A URL do webhook contém um **segredo aleatório** no path
  (`/api/telegram/webhook/{secret}`).
- O segredo é validado a cada requisição. Requisições com segredo inválido
  recebem `401 Unauthorized`.
- O segredo é gerado automaticamente como um GUID de 32 caracteres
  hexadecimais.

### 10.3. Autorização por Chat ID

- Quando `TelegramAuthorizedChatId` está configurado, apenas mensagens daquele
  chat são processadas. Todas as outras são silenciosamente ignoradas.
- **Recomendação:** sempre configure o Chat ID em produção para evitar uso não
  autorizado.

### 10.4. Persistência de Chaves de Criptografia

Em produção (Docker), configure o volume de persistência das chaves:

```json
// appsettings.Production.json
{
  "DataProtection": {
    "ApplicationName": "JulliusFinancasApi",
    "KeysPath": "/var/jullius/keys"
  }
}
```

> **Atenção:** Se as chaves de criptografia forem perdidas (reinício do
> container sem volume persistente), os valores criptografados no banco
> ficam ilegíveis. Será necessário recadastrar as configurações.

---

## 11. Configuração em Produção (Docker)

### 11.1. Volume para Chaves de Criptografia

Adicione ao seu `docker-compose.yml`:

```yaml
services:
  api:
    volumes:
      - jullius-keys:/var/jullius/keys

volumes:
  jullius-keys:
```

### 11.2. Checklist de Deploy

1. Faça deploy do backend com HTTPS habilitado.
2. Acesse a interface web e configure as chaves (Seção 5).
3. Registre o webhook com a URL pública de produção (Seção 6).
4. Verifique o webhook com `getWebhookInfo` (Seção 6.4).
5. Teste enviando uma mensagem ao bot.

### 11.3. Verificação de Saúde

O endpoint `/health` confirma que a API está operacional:

```bash
curl https://seu-dominio.com/health
```

---

## 12. Troubleshooting

### Bot não responde às mensagens

| Causa Provável | Solução |
|---|---|
| Webhook não registrado | Verifique com `getWebhookInfo` (Seção 6.4) |
| Token inválido | Use o botão "Testar Conexão Telegram" na interface |
| Chat ID não autorizado | Confirme que o Chat ID salvo corresponde ao seu chat |
| Erro no Gemini | Use o botão "Testar Conexão Gemini" na interface |
| URL sem HTTPS | O Telegram exige HTTPS para webhooks |

### Erro "Não consegui entender sua mensagem"

- A resposta do Gemini pode ter sido truncada. Verifique os logs do servidor
  para alertas de `MAX_TOKENS`.
- Tente reformular a mensagem de forma mais simples.

### Erro "Chave API do Gemini não configurada"

- A chave `GeminiApiKey` não foi cadastrada ou a criptografia foi perdida.
- Recadastre a chave pela interface web ou API.

### Chaves criptografadas ilegíveis após reinício

- As chaves do Data Protection foram perdidas.
- Monte um volume persistente para `/var/jullius/keys` (Seção 11.1).
- Recadastre todas as configurações do bot.

### Mensagens duplicadas ou transações duplicadas

- Certifique-se de que existe apenas um webhook registrado.
- Verifique com `getWebhookInfo` se a URL está correta.

### Resposta lenta do bot (mais de 5 segundos)

- A latência típica é de 2-4 segundos (devido à chamada ao Gemini).
- Se o modelo Gemini usar muitos "thinking tokens", pode demorar mais.
- Verifique a conectividade de rede do servidor com a API do Google.

---

## 13. Referência Técnica

### 13.1. Endpoints da API de Configuração

Todos os endpoints requerem autenticação (`Authorization: Bearer <token>`).

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/BotConfiguration` | Lista todas as chaves (sem valores) |
| `GET` | `/api/BotConfiguration/{key}` | Retorna o valor decriptado de uma chave |
| `PUT` | `/api/BotConfiguration/{key}` | Cria ou atualiza uma chave (valor é criptografado) |
| `DELETE` | `/api/BotConfiguration/{key}` | Remove uma chave |
| `POST` | `/api/BotConfiguration/test-telegram` | Testa conexão com o bot Telegram |
| `POST` | `/api/BotConfiguration/test-gemini` | Testa conexão com a API Gemini |
| `POST` | `/api/BotConfiguration/register-webhook` | Registra o webhook no Telegram |

### 13.2. Modelo de IA

| Propriedade | Valor |
|---|---|
| Modelo | `gemini-3-flash-preview` |
| API Base | `https://generativelanguage.googleapis.com/v1beta/models` |
| Temperature (classificação) | 0.1 |
| Temperature (consultoria) | 0.7 |
| Max Output Tokens | 8192 (classificação e consultoria) / 4096 (follow-up) |
| Formato de resposta | `application/json` (classificação e follow-up) |

### 13.3. Estado da Conversa

O estado é mantido **em memória** (não é persistido no banco):

| Propriedade | Valor |
|---|---|
| Armazenamento | `ConcurrentDictionary` (in-memory) |
| TTL | 10 minutos de inatividade |
| Limpeza | A cada 2 minutos via Timer |
| Persistência | Não (estado perdido ao reiniciar a aplicação) |

### 13.4. Intenções Suportadas

| Intenção | Campos Obrigatórios | Ação |
|---|---|---|
| `CREATE_EXPENSE` | `description`, `amount`, `categoryName` | Cria uma transação financeira (despesa) |
| `CREATE_CARD_PURCHASE` | `description`, `amount`, `cardName` | Cria uma transação no cartão de crédito |
| `FINANCIAL_CONSULTING` | — | Consulta financeira com resposta em linguagem natural |

### 13.5. Arquivos Relevantes

| Arquivo | Descrição |
|---|---|
| `server/src/Jullius.ServiceApi/Controllers/TelegramWebhookController.cs` | Endpoint do webhook |
| `server/src/Jullius.ServiceApi/Controllers/BotConfigurationController.cs` | CRUD de configurações e registro de webhook |
| `server/src/Jullius.ServiceApi/Telegram/TelegramBotService.cs` | Processamento de mensagens e envio de respostas |
| `server/src/Jullius.ServiceApi/Telegram/ConversationOrchestrator.cs` | Máquina de estados da conversa |
| `server/src/Jullius.ServiceApi/Telegram/ConversationState.cs` | Modelo de estado da conversa |
| `server/src/Jullius.ServiceApi/Telegram/ConversationStateStore.cs` | Store in-memory com TTL |
| `server/src/Jullius.ServiceApi/Telegram/IntentHandlers/` | Handlers de cada intenção |
| `server/src/Jullius.ServiceApi/Application/Services/GeminiAssistantService.cs` | Integração com a API do Gemini |
| `server/src/Jullius.ServiceApi/Application/Services/BotConfigurationService.cs` | Serviço de criptografia de configurações |
| `server/src/Jullius.ServiceApi/Configuration/TelegramExtensions.cs` | Registro de serviços no DI |
| `client/src/app/features/settings/` | Página de configurações no frontend |
