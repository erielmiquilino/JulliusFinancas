# Plano de Ação — Substituir Firebase Auth por JWT Embedded

> **Status**: PLANEJAMENTO (nenhum código implementado)
> **Data**: 01/03/2026
> **Autor**: Arquitetura de Software

---

## Sumário

- [Contexto e Objetivo](#contexto-e-objetivo)
- [Decisões Arquiteturais](#decisões-arquiteturais)
- [Fase 1 — Mapeamento do Fluxo Atual (Firebase)](#fase-1--mapeamento-do-fluxo-atual-firebase)
- [Fase 2 — Backend: Entidades e Infraestrutura de Dados](#fase-2--backend-entidades-e-infraestrutura-de-dados)
- [Fase 3 — Backend: Serviço de Autenticação e Endpoints](#fase-3--backend-serviço-de-autenticação-e-endpoints)
- [Fase 4 — Frontend: Remover Firebase e Implementar Auth Custom](#fase-4--frontend-remover-firebase-e-implementar-auth-custom)
- [Fase 5 — Infraestrutura: Docker e CI/CD](#fase-5--infraestrutura-docker-e-cicd)
- [Fase 6 — Testes](#fase-6--testes)
- [Fase 7 — Cleanup e Documentação](#fase-7--cleanup-e-documentação)
- [Ordem de Implementação](#ordem-de-implementação)
- [Critérios de Verificação (Definition of Done)](#critérios-de-verificação-definition-of-done)

---

## Contexto e Objetivo

Remover **completamente** a dependência do Firebase Authentication deste projeto Open Source e substituí-lo por uma solução de **autenticação embedded (Custom JWT)** gerenciada diretamente pela nossa API ASP.NET Core e banco de dados PostgreSQL.

### O que muda

| Aspecto | Antes (Firebase) | Depois (JWT Embedded) |
|---|---|---|
| Emissão de tokens | Firebase (Google Cloud) | Nossa API (`POST /api/auth/login`) |
| Validação de tokens | OIDC Discovery do Google | Chave simétrica própria (HMAC-SHA256) |
| Armazenamento de usuários | Firebase Console | Tabela `Users` no PostgreSQL |
| Hash de senhas | Firebase (interno) | BCrypt (work factor 12) |
| Recuperação de senha | Firebase envia e-mail | SMTP próprio (MailKit) |
| Refresh de sessão | Firebase SDK (automático) | Refresh Token rotativo via API |

### Premissa

A **migração dos usuários existentes** no Firebase para o novo banco será feita manualmente e **não faz parte** deste plano. Foco total na nova arquitetura.

---

## Decisões Arquiteturais

| Decisão | Escolha | Justificativa |
|---|---|---|
| Multi-tenancy | **Não** — mantém single-user | App é de uso pessoal; auth funciona como portão de acesso |
| Cadastro público | **Não** — apenas admin cria contas | Contas criadas via seed/script. Sem endpoint `POST /api/auth/register` |
| Recuperação de senha | **SMTP próprio** (MailKit + SendGrid/Gmail) | E-mail com token de reset enviado pela API |
| Refresh Token | **Sim** — access token 15min + refresh token 30 dias | Renovação silenciosa sem re-login frequente |
| Algoritmo JWT | **HMAC-SHA256** (chave simétrica) | Simples para monolito; migrar para RSA se virar microserviços |
| Hash de senha | **BCrypt** (work factor 12) | Suporte maduro via `BCrypt.Net-Next`; sem Argon2 por simplicidade |
| Storage do token no frontend | Access token em **memória**, refresh token em **localStorage** | Trade-off aceitável para app single-user |

---

## Fase 1 — Mapeamento do Fluxo Atual (Firebase)

### 1.1 Fluxo de Autenticação Atual

```
┌──────────────────────────────────────────────────────────────────┐
│                    FLUXO ATUAL (FIREBASE)                        │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. index.html carrega env.js (config Firebase em runtime)       │
│                                                                  │
│  2. AppModule inicializa Firebase:                               │
│     provideFirebaseApp(() => initializeApp(environment.firebase)) │
│     provideAuth(() => getAuth())                                 │
│                                                                  │
│  3. AuthService escuta onAuthStateChanged()                      │
│     → Se sem sessão: authGuard redireciona para /auth/login      │
│                                                                  │
│  4. LoginComponent → AuthService.login()                         │
│     → signInWithEmailAndPassword(auth, email, password)          │
│     → Firebase retorna sessão → onAuthStateChanged dispara       │
│     → Navega para /dashboard                                     │
│                                                                  │
│  5. A cada request HTTP, authInterceptor:                        │
│     → idToken(auth).pipe(take(1))                                │
│     → Clona request com Authorization: Bearer <Firebase JWT>     │
│                                                                  │
│  6. Backend recebe request:                                      │
│     → UseAuthentication() valida JWT via OIDC do Google          │
│       (https://securetoken.google.com/jullius-financas)          │
│     → [Authorize] libera ou bloqueia acesso                      │
│     → Sem scoping de dados por usuário                           │
│                                                                  │
│  7. Logout → signOut(auth) → navega para /auth/login             │
│                                                                  │
│  8. Reset de senha → sendPasswordResetEmail(auth, email)         │
│     → Firebase envia e-mail; 100% gerenciado pelo Firebase       │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### 1.2 Inventário Completo de Arquivos com Dependência Firebase

#### Frontend (Angular)

| Arquivo | Dependência Firebase | O que faz |
|---|---|---|
| `client/package.json` | `@angular/fire` ^20.0.1, `firebase` ^11.9.1 | Pacotes NPM |
| `client/src/app/app.module.ts` | `provideFirebaseApp`, `provideAuth`, `initializeApp`, `getAuth` | Inicialização do Firebase no Angular |
| `client/src/app/core/auth/services/auth.service.ts` | `signInWithEmailAndPassword`, `signOut`, `sendPasswordResetEmail`, `onAuthStateChanged`, `Auth` | Serviço principal de autenticação |
| `client/src/app/core/auth/interceptors/auth.interceptor.ts` | `Auth`, `idToken` de `@angular/fire/auth` | Injeta token Firebase em cada request HTTP |
| `client/src/app/core/auth/models/user.model.ts` | Interface `User` modelada conforme `FirebaseUser` | Campos: `uid`, `photoURL`, `isAnonymous`, `emailVerified` |
| `client/src/app/core/auth/guards/auth.guard.ts` | Usa `AuthService.authState$` (indiretamente Firebase) | Guard de rotas autenticadas |
| `client/src/app/core/auth/guards/no-auth.guard.ts` | Usa `AuthService.authState$` (indiretamente Firebase) | Guard de rotas públicas |
| `client/src/app/features/auth/components/login/login.component.ts` | Chama `authService.login()` → Firebase | Tela de login |
| `client/src/app/features/auth/components/forgot-password/forgot-password.component.ts` | Chama `authService.resetPassword()` → Firebase | Tela de recuperação de senha |
| `client/src/app/shared/components/user-menu/user-menu.component.ts` | Lê `authService.getCurrentUser()` (dados Firebase) | Menu do usuário (avatar, nome, logout) |
| `client/src/environments/environment.ts` | Bloco `firebase: { projectId, appId, apiKey, ... }` | Config Firebase desenvolvimento |
| `client/src/environments/environment.prod.ts` | Bloco `firebase: { ... }` com `window.env` | Config Firebase produção |
| `client/src/assets/env.js` | Variáveis `firebaseProjectId`, `firebaseApiKey`, etc. | Injeção runtime de config Firebase |
| `client/FIREBASE_CONFIG.md` | Documentação completa do setup Firebase | Guia de configuração |

#### Backend (ASP.NET Core)

| Arquivo | Dependência Firebase | O que faz |
|---|---|---|
| `server/src/Jullius.ServiceApi/Configuration/AuthenticationExtensions.cs` | `AddFirebaseAuthentication()` — Authority = Google OIDC | Configuração JWT Bearer validando tokens Firebase |
| `server/src/Jullius.ServiceApi/Program.cs` | `services.AddFirebaseAuthentication(configuration)` | Registro do middleware de auth Firebase |
| `server/src/Jullius.ServiceApi/appsettings.json` | Seção `"Firebase": { "ProjectId": "jullius-financas" }` | Config do project ID Firebase |
| Todos os Controllers (`[Authorize]`) | Indiretamente — dependem do token ser Firebase | 8 controllers protegidos |

#### Infraestrutura / CI/CD

| Arquivo | Dependência Firebase | O que faz |
|---|---|---|
| `.github/workflows/deploy.yml` | 6 secrets: `FIREBASE_PROJECT_ID`, `FIREBASE_APP_ID`, `FIREBASE_STORAGE_BUCKET`, `FIREBASE_API_KEY`, `FIREBASE_AUTH_DOMAIN`, `FIREBASE_MESSAGING_SENDER_ID` | Injeta config Firebase no `.env` do VPS |
| `docker-compose.prod.yml` | `env_file: .env` (contém vars Firebase) | Passa vars para container |
| `docker/entrypoint.sh` | Valida `FIREBASE_API_KEY`, gera `env.js` com config Firebase | Startup do container |

#### Não impactados (sem Firebase)

- `Jullius.Domain/` — nenhuma entidade referencia Firebase diretamente
- `Jullius.Data/` — DbContext sem tabelas de usuário
- `Jullius.Tests/` — testes existentes não mockam Firebase
- `docker/nginx.conf` — sem config de auth
- `Dockerfile` — sem variáveis Firebase build-time
- `TelegramWebhookController` — auth própria (sem `[Authorize]`)

### 1.3 Observações Importantes

1. **Sem Firebase Admin SDK** no backend — validação via JWT Bearer padrão (OIDC do Google). Isso simplifica a migração: basta trocar a Authority/chave de validação.
2. **Arquitetura single-user** — nenhum controller extrai o Firebase UID das claims. Auth é binário (válido/inválido).
3. **Sem fluxo de cadastro** — apenas login e reset de senha existem.
4. **Pacotes MSAL não utilizados** — `@azure/msal-angular` e `@azure/msal-browser` estão no `package.json` mas sem nenhum import no código. Devem ser removidos na limpeza.
5. **`BotConfiguration.UserId`** — único campo `UserId` no domínio (string, varchar(200)). É um identificador genérico, não acoplado ao Firebase UID.

---

## Fase 2 — Backend: Entidades e Infraestrutura de Dados

### 2.1 Nova Entidade `User`

**Arquivo**: `server/src/Jullius.Domain/Domain/Entities/User.cs`

```
User
├── Id              : Guid (PK)
├── Email           : string (unique, not null, max 256)
├── PasswordHash    : string (not null, max 512)
├── DisplayName     : string? (max 200)
├── IsActive        : bool (default true)
├── CreatedAt       : DateTime (UTC)
└── UpdatedAt       : DateTime (UTC)
```

### 2.2 Nova Entidade `RefreshToken`

**Arquivo**: `server/src/Jullius.Domain/Domain/Entities/RefreshToken.cs`

```
RefreshToken
├── Id              : Guid (PK)
├── UserId          : Guid (FK → User, not null)
├── Token           : string (indexed, not null, max 512)
├── ExpiresAt       : DateTime (UTC)
├── CreatedAt       : DateTime (UTC)
├── RevokedAt       : DateTime? (UTC)
└── ReplacedByToken : string? (max 512) — para rotação
```

> **Rotação**: ao usar um refresh token, ele é revogado e substituído por um novo. `ReplacedByToken` rastreia a cadeia. Se um token já revogado for reapresentado, toda a cadeia é invalidada (detecção de roubo).

### 2.3 Nova Entidade `PasswordResetToken`

**Arquivo**: `server/src/Jullius.Domain/Domain/Entities/PasswordResetToken.cs`

```
PasswordResetToken
├── Id              : Guid (PK)
├── UserId          : Guid (FK → User, not null)
├── TokenHash       : string (not null, max 512) — SHA256 do token
├── ExpiresAt       : DateTime (UTC, tipicamente +1h)
└── UsedAt          : DateTime? (UTC)
```

> **Segurança**: o token raw é enviado por e-mail. No banco armazenamos apenas o SHA256 hash. Na validação, fazemos hash do token recebido e comparamos.

### 2.4 Interfaces de Repositório

**Arquivos** em `server/src/Jullius.Domain/Domain/Repositories/`:

- `IUserRepository.cs` — `GetByEmailAsync(email)`, `GetByIdAsync(id)`, `AddAsync(user)`, `UpdateAsync(user)`
- `IRefreshTokenRepository.cs` — `AddAsync(token)`, `GetByTokenAsync(token)`, `RevokeAsync(id)`, `RevokeAllByUserIdAsync(userId)`
- `IPasswordResetTokenRepository.cs` — `AddAsync(token)`, `GetByTokenHashAsync(hash)`, `MarkAsUsedAsync(id)`

### 2.5 Atualizar DbContext e Configurações EF

**Arquivos impactados**:
- `server/src/Jullius.Data/Context/JulliusDbContext.cs` — adicionar 3 novos `DbSet<>`
- `server/src/Jullius.Data/Configurations/UserConfiguration.cs` (novo)
- `server/src/Jullius.Data/Configurations/RefreshTokenConfiguration.cs` (novo)
- `server/src/Jullius.Data/Configurations/PasswordResetTokenConfiguration.cs` (novo)

**Índices importantes**:
- `Users.Email` — unique index
- `RefreshTokens.Token` — index
- `RefreshTokens.UserId` — index (FK)
- `PasswordResetTokens.TokenHash` — index
- `PasswordResetTokens.UserId` — index (FK)

### 2.6 Implementar Repositórios

**Arquivos** em `server/src/Jullius.Data/Repositories/`:
- `UserRepository.cs`, `RefreshTokenRepository.cs`, `PasswordResetTokenRepository.cs`

### 2.7 Migração EF

**Comando**: `dotnet ef migrations add AddAuthenticationTables --project server/src/Jullius.Data --startup-project server/src/Jullius.ServiceApi`

Cria tabelas: `Users`, `RefreshTokens`, `PasswordResetTokens`

### 2.8 Seed de Usuário Admin

No serviço `DatabaseMigrationService` ou diretamente no `Program.cs`, após aplicar migrações:

- Verificar se existe usuário com e-mail configurado em `appsettings` (seção `Admin:Email`)
- Se não existir, criar com senha BCrypt hash da variável `Admin:DefaultPassword`
- Em produção, `Admin:DefaultPassword` virá de variável de ambiente (GitHub Secret)
- Log: "Admin user created" ou "Admin user already exists"

---

## Fase 3 — Backend: Serviço de Autenticação e Endpoints

### 3.1 Pacotes NuGet a Adicionar

**Arquivo**: `server/src/Jullius.ServiceApi/Jullius.ServiceApi.csproj`

| Pacote | Finalidade |
|---|---|
| `BCrypt.Net-Next` | Hash e verificação de senhas |
| `MailKit` | Envio de e-mails via SMTP |

> `Microsoft.AspNetCore.Authentication.JwtBearer` já existe — será reconfigurado.

### 3.2 DTOs de Autenticação

**Arquivos** em `server/src/Jullius.ServiceApi/Application/DTOs/`:

```
LoginRequestDto        { Email, Password }
LoginResponseDto       { AccessToken, RefreshToken, ExpiresIn }
RefreshTokenRequestDto { RefreshToken }
RefreshTokenResponseDto { AccessToken, RefreshToken, ExpiresIn }
ForgotPasswordRequestDto { Email }
ResetPasswordRequestDto  { Token, NewPassword }
```

### 3.3 `TokenService`

**Arquivo**: `server/src/Jullius.ServiceApi/Application/Services/TokenService.cs`

Responsabilidades:
- **Gerar Access Token JWT** (15min): claims `sub` (userId), `email`, `name`, `jti` (GUID único), assinado com HMAC-SHA256
- **Gerar Refresh Token**: string criptograficamente aleatória (64 bytes → Base64), expiração 30 dias
- **Validar Access Token**: usado internamente se necessário (o middleware JwtBearer faz a validação principal)

Configuração lida de `IOptions<JwtSettings>`:
```
JwtSettings
├── Secret              : string (≥256 bits / 32 chars)
├── Issuer              : string (ex: "jullius-api")
├── Audience            : string (ex: "jullius-app")
├── AccessTokenMinutes  : int (default: 15)
└── RefreshTokenDays    : int (default: 30)
```

### 3.4 `AuthService`

**Arquivo**: `server/src/Jullius.ServiceApi/Application/Services/AuthService.cs`

| Método | Lógica |
|---|---|
| `LoginAsync(email, password)` | Busca User por email → verifica BCrypt → gera access+refresh tokens → salva RefreshToken no DB → retorna `LoginResponseDto` |
| `RefreshAsync(refreshToken)` | Busca RefreshToken no DB → valida (não expirado, não revogado) → revoga o antigo → gera novo par → salva novo RefreshToken → retorna `RefreshTokenResponseDto` |
| `ForgotPasswordAsync(email)` | Busca User por email → gera token aleatório → salva SHA256 hash no DB como `PasswordResetToken` → envia e-mail com link contendo token raw |
| `ResetPasswordAsync(token, newPassword)` | Faz SHA256 do token → busca `PasswordResetToken` por hash → valida (não expirado, não usado) → atualiza `User.PasswordHash` com BCrypt → marca token como usado → revoga todos refresh tokens do user |
| `LogoutAsync(refreshToken)` | Busca e revoga o refresh token no DB |

### 3.5 `EmailService`

**Arquivo**: `server/src/Jullius.ServiceApi/Services/EmailService.cs`

- Usa `MailKit.Net.Smtp.SmtpClient` para enviar e-mails
- Template HTML simples para e-mail de reset (link com token)
- Configuração via `IOptions<SmtpSettings>`:

```
SmtpSettings
├── Host        : string (ex: "smtp.gmail.com")
├── Port        : int (ex: 587)
├── Username    : string
├── Password    : string
├── FromAddress : string (ex: "noreply@jullius.com")
└── FromName    : string (ex: "Jullius Finanças")
```

### 3.6 `AuthController`

**Arquivo**: `server/src/Jullius.ServiceApi/Controllers/AuthController.cs`

| Endpoint | Método | Auth | Descrição |
|---|---|---|---|
| `POST /api/auth/login` | `LoginAsync` | `[AllowAnonymous]` | Retorna access + refresh tokens |
| `POST /api/auth/refresh` | `RefreshAsync` | `[AllowAnonymous]` | Renova par de tokens |
| `POST /api/auth/forgot-password` | `ForgotPasswordAsync` | `[AllowAnonymous]` | Envia e-mail de reset |
| `POST /api/auth/reset-password` | `ResetPasswordAsync` | `[AllowAnonymous]` | Reseta senha com token |
| `POST /api/auth/logout` | `LogoutAsync` | `[Authorize]` | Revoga refresh token |

> **Segurança**: `forgot-password` sempre retorna 200 OK independente de o e-mail existir (evita enumeração de usuários).

### 3.7 Reconfigurar `AuthenticationExtensions.cs`

**Arquivo**: `server/src/Jullius.ServiceApi/Configuration/AuthenticationExtensions.cs`

**Antes** (Firebase):
```csharp
options.Authority = $"https://securetoken.google.com/{projectId}";
options.TokenValidationParameters = new TokenValidationParameters {
    ValidIssuer = $"https://securetoken.google.com/{projectId}",
    ValidAudience = projectId,
};
```

**Depois** (JWT próprio):
```csharp
options.TokenValidationParameters = new TokenValidationParameters {
    ValidateIssuer = true,
    ValidIssuer = jwtSettings.Issuer,
    ValidateAudience = true,
    ValidAudience = jwtSettings.Audience,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
    ClockSkew = TimeSpan.Zero  // sem tolerância de clock
};
```

- Remover referência a Firebase/Google
- Renomear método para `AddJwtAuthentication()`
- Registrar `JwtSettings` e `SmtpSettings` via `IOptions<T>`

### 3.8 Atualizar Configurações (`appsettings`)

**`appsettings.json`** — remover seção `Firebase`, adicionar:

```json
{
  "Jwt": {
    "Secret": "",
    "Issuer": "jullius-api",
    "Audience": "jullius-app",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 30
  },
  "Smtp": {
    "Host": "",
    "Port": 587,
    "Username": "",
    "Password": "",
    "FromAddress": "",
    "FromName": "Jullius Finanças"
  },
  "Admin": {
    "Email": "admin@jullius.com",
    "DefaultPassword": ""
  }
}
```

**`appsettings.Development.json`** — valores locais (chave JWT dev, SMTP local como MailHog/Papercut)

**`appsettings.Production.json`** — valores virão de variáveis de ambiente:
- `Jwt__Secret`, `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, etc.

### 3.9 Registrar Serviços no DI

**Arquivo**: `server/src/Jullius.ServiceApi/Configuration/DependencyInjectionExtensions.cs`

Adicionar em `AddRepositories()`:
- `IUserRepository` → `UserRepository`
- `IRefreshTokenRepository` → `RefreshTokenRepository`
- `IPasswordResetTokenRepository` → `PasswordResetTokenRepository`

Adicionar em `AddApplicationServices()`:
- `TokenService` (Scoped)
- `AuthService` (Scoped)
- `EmailService` (Scoped)

---

## Fase 4 — Frontend: Remover Firebase e Implementar Auth Custom

### 4.1 Remover Dependências NPM

**Arquivo**: `client/package.json`

Remover:
- `@angular/fire`
- `firebase`
- `@azure/msal-angular` (não utilizado)
- `@azure/msal-browser` (não utilizado)

### 4.2 Remover Inicialização Firebase

**Arquivo**: `client/src/app/app.module.ts`

Remover:
```typescript
// REMOVER estes imports e providers:
import { provideFirebaseApp, initializeApp } from '@angular/fire/app';
import { provideAuth, getAuth } from '@angular/fire/auth';

// REMOVER dos providers:
provideFirebaseApp(() => initializeApp(environment.firebase)),
provideAuth(() => getAuth()),
```

### 4.3 Reescrever `AuthService`

**Arquivo**: `client/src/app/core/auth/services/auth.service.ts`

**Antes**: usa `@angular/fire/auth` diretamente (signInWithEmailAndPassword, signOut, etc.)

**Depois**: usa `HttpClient` para chamar a API:

| Método | Implementação |
|---|---|
| `login(email, password)` | `POST /api/auth/login` → armazena access token em memória (BehaviorSubject), refresh token em `localStorage` |
| `logout()` | `POST /api/auth/logout` → limpa tokens → redireciona para `/auth/login` |
| `refreshToken()` | `POST /api/auth/refresh` → atualiza access token em memória |
| `resetPassword(email)` | `POST /api/auth/forgot-password` |
| `confirmResetPassword(token, newPassword)` | `POST /api/auth/reset-password` |
| `getCurrentUser()` | Decodifica payload do JWT (base64) → extrai claims `sub`, `email`, `name` |
| `isAuthenticated()` | Verifica se access token existe e não expirou |
| **Inicialização** | Ao carregar a app, tenta refresh silencioso se houver refresh token no localStorage |

### 4.4 Reescrever `User` Model

**Arquivo**: `client/src/app/core/auth/models/user.model.ts`

**Antes** (Firebase):
```typescript
interface User {
  uid: string;
  email: string | null;
  displayName: string | null;
  photoURL: string | null;
  emailVerified: boolean;
  isAnonymous: boolean;
  metadata: { creationTime?: string; lastSignInTime?: string; };
}
```

**Depois** (JWT próprio):
```typescript
interface User {
  id: string;           // claim "sub" do JWT
  email: string;
  displayName: string | null;
}

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;    // segundos
}

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

interface LoginCredentials {
  email: string;
  password: string;
}
```

### 4.5 Reescrever `auth.interceptor.ts`

**Arquivo**: `client/src/app/core/auth/interceptors/auth.interceptor.ts`

**Antes**: usa `idToken()` do `@angular/fire/auth` para obter token Firebase.

**Depois**:
1. Obtém access token do `AuthService` (em memória)
2. Anexa como `Authorization: Bearer <token>`
3. **Intercepta respostas 401**: tenta refresh silencioso → re-envia request original
4. Se refresh falhar (401 no refresh) → limpa sessão, redireciona para `/auth/login`

```
┌─────────────────────────────────────────────────┐
│           INTERCEPTOR (NOVO FLUXO)              │
├─────────────────────────────────────────────────┤
│                                                  │
│  request                                         │
│    ├─ tem access token? → anexa Bearer header    │
│    └─ não tem? → envia sem header                │
│                                                  │
│  response                                        │
│    ├─ 401?                                       │
│    │   ├─ tem refresh token?                     │
│    │   │   ├─ POST /api/auth/refresh             │
│    │   │   │   ├─ sucesso → retry request        │
│    │   │   │   └─ falha → logout + redirect      │
│    │   │   └─ não tem → logout + redirect        │
│    │   └─ não é 401 → propaga erro               │
│    └─ ok → propaga resposta                      │
│                                                  │
└─────────────────────────────────────────────────┘
```

### 4.6 Guards (Alteração Mínima)

**Arquivos**:
- `client/src/app/core/auth/guards/auth.guard.ts`
- `client/src/app/core/auth/guards/no-auth.guard.ts`

Esses guards já dependem apenas do `AuthService.authState$` — se a interface `AuthState` não mudar (e não vai mudar), **não precisam de alteração**. Apenas validar que continuam funcionando com o novo `AuthService`.

### 4.7 Atualizar Componentes de UI

**`LoginComponent`** (`client/src/app/features/auth/components/login/login.component.ts`):
- Ajustar chamada para novo `authService.login()` (retorna `Observable<TokenResponse>`)
- Remover tradução de códigos de erro Firebase → adaptar para erros HTTP (400, 401, 500)
- Lógica de UI (formulário, validação) permanece igual

**`ForgotPasswordComponent`** (`client/src/app/features/auth/components/forgot-password/forgot-password.component.ts`):
- Ajustar para novo `authService.resetPassword()` (agora chama API)
- Mensagem de sucesso: "Se o e-mail existir, enviaremos instruções de redefinição"

**`UserMenuComponent`** (`client/src/app/shared/components/user-menu/user-menu.component.ts`):
- Ajustar leitura de user para novo modelo (sem `photoURL`, `isAnonymous`)
- Avatar: usar iniciais do `displayName` ou `email` em vez de `photoURL`

**`HeaderComponent`** (`client/src/app/layout/header/header.component.ts`):
- Sem alteração se continuar usando `authService.isAuthenticated()`

### 4.8 Criar `ResetPasswordComponent` (NOVO)

**Arquivo**: `client/src/app/features/auth/components/reset-password/reset-password.component.ts`

- **Rota**: `/auth/reset-password?token=xxx`
- **Formulário**: nova senha + confirmação de senha
- **Ação**: `POST /api/auth/reset-password` com `{ token, newPassword }`
- **Sucesso**: mensagem + redirect para login
- **Erro**: token inválido/expirado → mensagem + link para "Esqueci minha senha"

Registrar rota em `client/src/app/features/auth/auth.routes.ts`.

### 4.9 Limpar Configurações de Ambiente

**`client/src/environments/environment.ts`** e **`environment.prod.ts`**:
- Remover bloco `firebase: { ... }` inteiro
- Manter apenas `apiUrl` (já existente)

**`client/src/assets/env.js`**:
- Remover todas as variáveis `firebase*`
- Manter `apiUrl` se necessário para runtime injection

---

## Fase 5 — Infraestrutura: Docker e CI/CD

### 5.1 Atualizar `docker/entrypoint.sh`

- **Remover**: validação de `FIREBASE_API_KEY` (linhas ~50-68)
- **Remover**: geração de config Firebase no `env.js`
- **Manter**: geração de `env.js` com `apiUrl` se necessário
- **Adicionar**: (opcional) validação de `Jwt__Secret` para fail-fast

### 5.2 Atualizar `docker-compose.prod.yml`

Adicionar na seção `environment`:

```yaml
environment:
  - ConnectionStrings__DefaultConnection=...
  - Jwt__Secret=${JWT_SECRET}
  - Jwt__Issuer=${JWT_ISSUER:-jullius-api}
  - Jwt__Audience=${JWT_AUDIENCE:-jullius-app}
  - Smtp__Host=${SMTP_HOST}
  - Smtp__Port=${SMTP_PORT:-587}
  - Smtp__Username=${SMTP_USERNAME}
  - Smtp__Password=${SMTP_PASSWORD}
  - Smtp__FromAddress=${SMTP_FROM_ADDRESS}
  - Admin__DefaultPassword=${ADMIN_DEFAULT_PASSWORD}
```

### 5.3 Atualizar `.github/workflows/deploy.yml`

**Remover** da escrita do `.env` (steps SSH):
```
FIREBASE_PROJECT_ID, FIREBASE_APP_ID, FIREBASE_STORAGE_BUCKET,
FIREBASE_API_KEY, FIREBASE_AUTH_DOMAIN, FIREBASE_MESSAGING_SENDER_ID
```

**Adicionar**:
```
JWT_SECRET=${{ secrets.JWT_SECRET }}
SMTP_HOST=${{ secrets.SMTP_HOST }}
SMTP_PORT=${{ secrets.SMTP_PORT }}
SMTP_USERNAME=${{ secrets.SMTP_USERNAME }}
SMTP_PASSWORD=${{ secrets.SMTP_PASSWORD }}
SMTP_FROM_ADDRESS=${{ secrets.SMTP_FROM_ADDRESS }}
ADMIN_DEFAULT_PASSWORD=${{ secrets.ADMIN_DEFAULT_PASSWORD }}
```

### 5.4 GitHub Secrets — Ações no Repositório

| Ação | Secret |
|---|---|
| **Criar** | `JWT_SECRET` (gerar com `openssl rand -base64 64`) |
| **Criar** | `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM_ADDRESS` |
| **Criar** | `ADMIN_DEFAULT_PASSWORD` |
| **Remover** | `FIREBASE_PROJECT_ID`, `FIREBASE_APP_ID`, `FIREBASE_STORAGE_BUCKET`, `FIREBASE_API_KEY`, `FIREBASE_AUTH_DOMAIN`, `FIREBASE_MESSAGING_SENDER_ID` |

---

## Fase 6 — Testes

### 6.1 Testes Unitários Backend (xUnit)

**Arquivos** em `server/src/Jullius.Tests/Services/`:

| Arquivo | Cenários |
|---|---|
| `AuthServiceTests.cs` | Login válido/inválido, usuário inexistente, usuário inativo, conta bloqueada |
| `TokenServiceTests.cs` | Geração de JWT com claims corretas, expiração correta, refresh token aleatório |
| `RefreshToken Tests` | Token válido, expirado, revogado, rotação, detecção de reuso |
| `PasswordReset Tests` | Geração de token, expiração, token já usado, hash SHA256 correto |

Padrão de nomenclatura: `Method_ShouldExpectation_WhenCondition` (conforme `AGENTS.md`).

### 6.2 Testes Frontend (Jasmine/Karma)

- Atualizar specs existentes removendo mocks de `@angular/fire/auth`
- Testar `AuthService` com `HttpClientTestingModule`
- Testar interceptor: request normal, refresh em 401, logout em refresh falho
- Testar `ResetPasswordComponent` (novo)

---

## Fase 7 — Cleanup e Documentação

### 7.1 Deletar

| Arquivo | Motivo |
|---|---|
| `client/FIREBASE_CONFIG.md` | Não mais aplicável |

### 7.2 Criar

| Arquivo | Conteúdo |
|---|---|
| `docs/AUTHENTICATION.md` | Documentação completa da nova arquitetura de autenticação (diagramas, tabelas, configuração, fluxos, segurança) |

### 7.3 Atualizar

| Arquivo | Alteração |
|---|---|
| `AGENTS.md` | Seção "Security & Configuration" — substituir referências Firebase por JWT/SMTP |
| `README.md` | Instruções de setup — remover Firebase, documentar variáveis JWT/SMTP |
| `docs/OPEN_SOURCE_SETUP.md` | Atualizar instruções se contiver referências a Firebase |

---

## Ordem de Implementação

> Cada passo é uma unidade que pode ser implementada, testada e commitada independentemente.

| # | Fase | Descrição | Arquivos Impactados |
|---|---|---|---|
| 1 | 2.1–2.4 | Entidades + interfaces de repositório (Domain) | `Jullius.Domain/Domain/Entities/User.cs`, `RefreshToken.cs`, `PasswordResetToken.cs`, `Repositories/IUserRepository.cs`, `IRefreshTokenRepository.cs`, `IPasswordResetTokenRepository.cs` |
| 2 | 2.5–2.6 | DbContext + configs EF + repositórios (Data) | `Jullius.Data/Context/JulliusDbContext.cs`, `Configurations/UserConfiguration.cs`, `RefreshTokenConfiguration.cs`, `PasswordResetTokenConfiguration.cs`, `Repositories/UserRepository.cs`, `RefreshTokenRepository.cs`, `PasswordResetTokenRepository.cs` |
| 3 | 2.7 | Migração EF | `Jullius.Data/Migrations/` (nova migração) |
| 4 | 3.1 | Pacotes NuGet | `Jullius.ServiceApi.csproj` |
| 5 | 3.2–3.5 | DTOs + TokenService + AuthService + EmailService | `Application/DTOs/*.cs`, `Application/Services/TokenService.cs`, `Application/Services/AuthService.cs`, `Services/EmailService.cs` |
| 6 | 3.6 | AuthController | `Controllers/AuthController.cs` |
| 7 | 3.7–3.9 | Reconfigurar auth backend + appsettings + DI | `Configuration/AuthenticationExtensions.cs`, `Configuration/DependencyInjectionExtensions.cs`, `appsettings*.json`, `Program.cs` |
| 8 | 2.8 | Seed de admin user | `Program.cs` ou `DatabaseMigrationService` |
| 9 | 4.1–4.2 | Remover Firebase do Angular | `package.json`, `app.module.ts` |
| 10 | 4.3–4.5 | Reescrever auth frontend (service, model, interceptor) | `auth.service.ts`, `user.model.ts`, `auth.interceptor.ts` |
| 11 | 4.7–4.8 | Atualizar/criar componentes UI | `login.component.ts`, `forgot-password.component.ts`, `reset-password.component.ts` (novo), `user-menu.component.ts`, `auth.routes.ts` |
| 12 | 4.9 | Limpar config ambiente | `environment.ts`, `environment.prod.ts`, `env.js` |
| 13 | 5.1–5.3 | Docker + CI/CD | `entrypoint.sh`, `docker-compose.prod.yml`, `deploy.yml` |
| 14 | 6.1–6.2 | Testes backend + frontend | `Jullius.Tests/Services/*`, specs Angular |
| 15 | 7.1–7.3 | Cleanup + documentação | `FIREBASE_CONFIG.md` (delete), `docs/AUTHENTICATION.md` (novo), `AGENTS.md`, `README.md` |

---

## Critérios de Verificação (Definition of Done)

- [ ] `dotnet build server/src/JulliusApi.sln` compila sem erros
- [ ] `dotnet test server/src/JulliusApi.sln` — todos os testes passam (existentes + novos)
- [ ] `cd client && npm run build` compila sem erros
- [ ] `cd client && npm test` — todos os testes passam
- [ ] `grep -ri "firebase" client/src/ server/src/` retorna **zero** hits
- [ ] `grep -ri "@angular/fire" client/src/` retorna **zero** hits
- [ ] Login com admin → recebe access + refresh tokens → acessa dashboard
- [ ] Refresh silencioso funciona (access token expira → renova automaticamente)
- [ ] Logout revoga refresh token (tentativa de refresh após logout falha)
- [ ] Forgot password envia e-mail com link de reset
- [ ] Reset password com token válido → senha alterada → login funciona com nova senha
- [ ] Reset password com token expirado/usado → erro adequado
- [ ] `docker compose up` funciona **sem** variáveis `FIREBASE_*`
- [ ] Pipeline GitHub Actions roda green (sem referência a secrets Firebase)
- [ ] Documentação `docs/AUTHENTICATION.md` existe e está completa
