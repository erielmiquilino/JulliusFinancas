# 💰 Jullius Finanças

[![Deploy Monolith to VPS](https://github.com/erielmiquilino/JulliusFinancas/actions/workflows/deploy.yml/badge.svg)](https://github.com/erielmiquilino/JulliusFinancas/actions/workflows/deploy.yml)

Um aplicativo completo de gerenciamento de finanças pessoais, construído com **Angular 21** no frontend, **ASP.NET Core 10** no backend e **PostgreSQL** como banco de dados. Integra-se com **Telegram** para notificações e oferece recursos avançados como orçamentos, rastreamento de cartões e transações.

## 🎯 Sobre o Projeto

**Jullius Finanças** é uma solução full-stack para ajudar você a gerenciar suas finanças pessoais:

- 📊 Dashboard intuitivo com visualizações de gastos
- 💳 Rastreamento de cartões de crédito
- 🤖 [Assistente de Telegram com IA](docs/TELEGRAM_BOT.md) — registre despesas e consulte suas finanças por linguagem natural
- 📈 Análise de transações e orçamentos
- 🏷️ Categorização automática de despesas
- 🔐 Autenticação segura com JWT + Azure AD/Entra

## 🛠️ Stack Tecnológico

### Frontend

- **Angular 21** - Framework frontend moderno
- **Angular Material 21** - Componentes UI
- **TypeScript 5.9** - Tipagem estática
- **RxJS 7.8** - Programação reativa
- **Firebase** - Autenticação alternativa
- **MSAL Angular** - Autenticação Azure AD

### Backend

- **.NET 10** - Runtime ASP.NET Core
- **Entity Framework Core 9** - ORM para dados
- **PostgreSQL 16** - Banco de dados relacional
- **Serilog** - Logging estruturado
- **Telegram.Bot** - Integração com Telegram
- **JWT** - Autenticação baseada em tokens

### DevOps

- **Docker & Docker Compose** - Containerização
- **GitHub Actions** - CI/CD
- **Azure** - Hospedagem em produção

## 📋 Pré-requisitos

Antes de começar, certifique-se de ter instalado:

- **Node.js 20+** (para o frontend Angular)
- **.NET 10 SDK** (para o backend ASP.NET Core)
- **PostgreSQL 16** ou superior (ou use Docker)
- **Git** para clonar o repositório
- **npm** (geralmente vem com Node.js)

### Verificar instalações

```bash
# Verificar Node.js
node --version    # v20.x.x ou superior
npm --version     # 10.x.x ou superior

# Verificar .NET SDK
dotnet --version  # 10.0.x ou superior

# Verificar PostgreSQL
psql --version   # 16.x ou superior
```

## 🚀 Instruções de Setup Local para Desenvolvimento

### 1️⃣ Clonar o Repositório

```bash
git clone https://github.com/erielmiquilino/JulliusFinancas.git
cd JulliusFinancas
```

### 2️⃣ Configurar o Banco de Dados (PostgreSQL)

#### Opção A: Usando Docker Compose (Recomendado)

```bash
# Inicie o PostgreSQL em um container
docker-compose up -d

# Verifique se o container está rodando
docker-compose ps
```

#### Opção B: Instalação Local

```bash
# No Windows
psql -U postgres

# Crie o banco de dados
CREATE DATABASE jullius_financas;
```

Após criar o banco, atualize a string de conexão em `server/src/Jullius.ServiceApi/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5432;Database=jullius_financas;Username=postgres;Password=sua_senha;"
  }
}
```

### 3️⃣ Setup do Frontend (Angular)

> **🔥 Configuração do Firebase:** Para configurar a autenticação e as variáveis de ambiente necessárias, consulte o guia detalhado em [client/FIREBASE_CONFIG.md](client/FIREBASE_CONFIG.md).

```bash
cd client

# Instalar dependências
npm install

# Validar a instalação
npm list @angular/core  # Deve mostrar 21.1.3 ou superior
```

**Estrutura do Frontend:**

```text
client/
├── src/
│   ├── app/
│   │   ├── core/          # Guards, interceptors, auth
│   │   ├── features/      # Módulos de features
│   │   ├── layout/        # Header, menu lateral
│   │   └── shared/        # Componentes e serviços compartilhados
│   ├── assets/            # Arquivos estáticos
│   ├── environments/      # Config por ambiente
│   └── main.ts            # Entry point
├── proxy.conf.json        # Proxy para /api → localhost:8081
└── package.json
```

### 4️⃣ Setup do Backend (ASP.NET Core)

```bash
cd server/src

# Restaurar pacotes NuGet
dotnet restore JulliusApi.sln

# Compilar a solução
dotnet build JulliusApi.sln

# Validar build
dotnet build JulliusApi.sln /property:GenerateFullPaths=true
```

**Estrutura do Backend:**

```text
server/src/
├── Jullius.ServiceApi/     # API principal (ASP.NET Core)
│   ├── Controllers/        # Endpoints HTTP
│   ├── Services/           # Lógica de negócio
│   ├── Configuration/      # Setup de extensões
│   ├── Middleware/         # Custom middleware
│   ├── Telegram/           # Integração Telegram
│   └── Program.cs          # Entry point
├── Jullius.Domain/         # Modelos de domínio
├── Jullius.Data/           # DbContext e migrations
└── Jullius.Tests/          # Testes unitários (xUnit)
```

### 5️⃣ Configurar o Bot de Telegram (Opcional)

> **🤖 Bot de Telegram com IA:** Para configurar o assistente de Telegram com Google Gemini, consulte o guia completo em [docs/TELEGRAM_BOT.md](docs/TELEGRAM_BOT.md).

### 6️⃣ Inicializações de Banco de Dados

O banco é inicializado automaticamente na primeira execução:

```bash
# Aplicar migrations (se necessário manual)
cd server/src/Jullius.ServiceApi
dotnet ef database update
```

## 🏃 Como Rodar o Projeto Localmente

### Terminal 1: Backend API

```bash
cd server/src/Jullius.ServiceApi

# Executar com hot-reload
dotnet watch run

# Ou simplesmente
dotnet run
```

- API rodará em: **<http://localhost:8081>** (padrão ASP.NET Core)
- Swagger estará disponível em: **<http://localhost:8081/swagger>**
- Health check em: **<http://localhost:8081/health>**

### Terminal 2: Frontend Angular

```bash
cd client

# Iniciar servidor de desenvolvimento
npm start
```

- Frontend estará em: **<http://localhost:4200>**
- O proxy configurado em `proxy.conf.json` roteia `/api/*` para `http://localhost:8081/api/*`

### ✅ Verificar Status

```bash
# Testar API
curl http://localhost:8081/health

# Abrir no navegador
open http://localhost:4200
```

## 🧪 Testes

### Testes do Frontend (Angular)

```bash
cd client

# Rodar testes uma vez
npm test

# Modo watch (redebug automático)
npm test -- watch
```

**Cobertura de testes:**

- Testes co-locados com componentes (`*.spec.ts`)
- Mock de requisições HTTP
- Testes de lógica de templates e pipes

### Testes do Backend (.NET)

```bash
cd server/src

# Executar todos os testes
dotnet test JulliusApi.sln

# Com relatório de cobertura
dotnet test JulliusApi.sln /p:CollectCoverage=true

# Apenas testes de uma categoria
dotnet test JulliusApi.sln --filter "Category=Unit"
```

**Padrão de testes:**

- Framework: xUnit + FluentAssertions + Moq
- Nomenclatura: `Method_ShouldExpectation_WhenCondition`
- Localização: `Jullius.Tests/<Area>/*Tests.cs`

## 📦 Build e Deploy

### Build do Frontend

```bash
cd client

# Production build
npm run build

# Saída em: client/dist/
```

### Build do Backend

```bash
cd server/src

# Publicar como release
dotnet publish JulliusApi.sln --configuration Release

# Saída em: Jullius.ServiceApi/bin/Release/net10.0/publish/
```

### Docker

```bash
# Build da imagem da API
docker build -f Dockerfile -t jullius-api:latest ./server/src/Jullius.ServiceApi

# Build da imagem do frontend
docker build -f client/Dockerfile -t jullius-web:latest ./client

# Usar Docker Compose para toda a stack
docker-compose up --build
```

## 📂 Estrutura do Projeto

```text
JulliusFinancas/
├── client/                          # Frontend Angular 21
│   ├── src/app/
│   │   ├── features/               # Módulos de features
│   │   │   ├── auth/               # Autenticação
│   │   │   ├── dashboard/          # Dashboard principal
│   │   │   ├── cards/              # Gerenciar cartões
│   │   │   ├── categories/         # Categorias de despesas
│   │   │   ├── budgets/            # Orçamentos
│   │   │   ├── financial-transaction/  # Transações
│   │   │   └── overdue-accounts/   # Contas atrasadas
│   │   ├── core/                   # Guards, interceptors, auth logic
│   │   ├── layout/                 # Header, side menu
│   │   └── shared/                 # Componentes e serviços compartilhados
│   ├── proxy.conf.json             # Dev proxy config
│   └── package.json
│
├── server/src/                      # Backend ASP.NET Core 10
│   ├── Jullius.ServiceApi/
│   │   ├── Controllers/            # Endpoints REST API
│   │   ├── Services/               # Lógica de negócio
│   │   ├── Configuration/          # Setup de extensions/middleware
│   │   ├── Telegram/               # Integração Telegram Bot
│   │   ├── Middleware/             # Custom middleware (erro, auth)
│   │   ├── Application/DTOs/       # Data Transfer Objects
│   │   ├── Program.cs              # Configuração startup
│   │   └── appsettings.*.json      # Config por ambiente
│   │
│   ├── Jullius.Domain/             # Entidades e interfaces
│   │   └── Domain/Entities/
│   │
│   ├── Jullius.Data/               # DBContext e migrations
│   │   └── Data/
│   │
│   └── Jullius.Tests/              # Testes unitários
│       ├── Services/
│       ├── Domain/
│       └── Telegram/
│
├── infra/                          # Templates Azure, scripts de setup
├── automation/                     # Scripts PowerShell para automação (Veja /automation/README.md)
├── docker-compose.prod.yml         # Docker Compose produção
├── Dockerfile                      # Multi-stage para deployment
└── README.md                       # Este arquivo
```

## 🔧 Configuração de Ambiente

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5432;Database=jullius_financas;Username=postgres;Password=sua_senha;"
  },
  "AllowedHosts": "*",
  "Firebase": {
    "ApiKey": "seu_firebase_key",
    "AuthDomain": "seu-dominio.firebaseapp.com"
  },
  "Telegram": {
    "BotToken": "seu_token_do_bot",
    "ChannelId": "seu_id_do_canal"
  }
}
```

**⚠️ Nunca commite `appsettings.Development.json` com valores reais!**

## 🐛 Troubleshooting

### "Porta 8081 já está em uso"

```bash
# Encontre o processo usando a porta (Windows)
netstat -ano | findstr :8081

# Ou use uma porta diferente
dotnet run --project server/src/Jullius.ServiceApi/Jullius.ServiceApi.csproj --launch-profile https
```

### "npm start não conecta à API"

- Verifique se o backend está rodando em `http://localhost:8081`
- Confira `client/proxy.conf.json` aponta para o endereço correto
- Limpe cache e reinicie: `npm cache clean --force` e `npm start`

### "Erro de conexão com PostgreSQL"

```bash
# Verifique se PostgreSQL está rodando
docker-compose ps

# Reinicie o container
docker-compose restart

# Ou verifique credenciais em appsettings.Development.json
```

### "Migrations falhando"

```bash
# Reset do banco de dados
cd server/src/Jullius.ServiceApi
dotnet ef database drop
dotnet ef database update
```

## 📝 Padrões de Código

### TypeScript/Angular

```typescript
// kebab-case para arquivos/pastas
// PascalCase para classes/componentes
export class UserAuthService {
  private userSubject$ = new BehaviorSubject<User | null>(null);
  
  public user$ = this.userSubject$.asObservable();
}

// Use const para imutabilidade
const readonly ROLES = ['admin', 'user'];
```

### C# / ASP.NET Core

```csharp
// PascalCase para public members
public class TransactionService
{
    private readonly ITransactionRepository _repository;
    
    // Async methods end with Async
    public async Task<IEnumerable<Transaction>> GetTransactionsAsync()
    {
        return await _repository.GetAllAsync();
    }
}
```

## 🤝 Contribuindo

1. **Branch a partir de `main`**

   ```bash
   git checkout -b feat/minha-feature
   ```

2. **Siga os padrões de commit**
   - `feat: adicionar nova feature`
   - `fix: corrigir bug`
   - `refactor: reestruturar código`
   - `docs: atualizar documentação`

3. **Faça testes**

   ```bash
   npm test          # Frontend
   dotnet test       # Backend
   ```

4. **Crie um Pull Request**
   - Descreva a sua mudança
   - Referencie issues relacionadas
   - Inclua screenshots para mudanças visuais

## 📄 Licença

Este projeto está sob a licença MIT. Veja [LICENSE.txt](LICENSE.txt) para mais detalhes.

## 📞 Suporte

- 📧 Abra uma [issue no GitHub](https://github.com/erielmiquilino/JulliusFinancas/issues)
- 💬 Contribuições são bem-vindas!
- ⭐ Se este projeto foi útil, deixe uma estrela!

---

**Desenvolvido com ❤️ por [Eriel Miquilino](https://github.com/erielmiquilino)**

**Última atualização:** Fevereiro de 2026

---

> **⚠️ Nota:** Esta documentação foi criada com **IA (GitHub Copilot)**. Alguns detalhes podem variar com a sua máquina e configuração específica. Sinta-se livre para abrir uma issue se encontrar inconsistências ou informações desatualizadas.
