# 🔥 Configuração Firebase - Jullius Finanças

Este documento explica como configurar o Firebase para autenticação no projeto Jullius Finanças.

## 📋 Pré-requisitos

1. Conta no Google/Firebase
2. Node.js e npm instalados
3. Angular CLI

## 🚀 Configuração do Firebase Console

### 1. Criar Projeto no Firebase

1. Acesse [Firebase Console](https://console.firebase.google.com/)
2. Clique em "Criar projeto"
3. Digite o nome: `jullius-financas` (ou nome de sua escolha)
4. Desabilite Google Analytics (opcional)
5. Clique em "Criar projeto"

### 2. Configurar Autenticação

1. No painel do Firebase, vá em **Authentication**
2. Clique em **Começar**
3. Vá na aba **Sign-in method**
4. Habilite os provedores desejados:
   - **Email/senha**: Habilite (obrigatório)
   - **Google**: Opcional
   - **Facebook**: Opcional

### 3. Registrar Aplicativo Web

1. Na página inicial do projeto, clique no ícone **Web** `</>`
2. Digite o nome do app: `Jullius Finanças`
3. **NÃO** marque "Configure Firebase Hosting"
4. Clique em **Registrar app**
5. **IMPORTANTE**: Copie as configurações que aparecerão

## ⚙️ Configuração no Projeto Angular

### 1. Configurar Variáveis de Ambiente

Edite os arquivos de ambiente com as configurações do Firebase:

#### `src/environments/environment.ts` (Desenvolvimento)

```typescript
export const environment = {
  production: false,
  apiUrl: '/api',
  firebase: {
    projectId: 'seu-project-id',
    appId: 'seu-app-id',
    storageBucket: 'seu-storage-bucket',
    apiKey: 'sua-api-key',
    authDomain: 'seu-auth-domain',
    messagingSenderId: 'seu-messaging-sender-id',
    measurementId: 'seu-measurement-id', // Opcional
  }
};
```

#### `src/environments/environment.prod.ts` (Produção)

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://sua-api-producao.com/api',
  firebase: {
    projectId: 'seu-project-id-prod',
    appId: 'seu-app-id-prod',
    storageBucket: 'seu-storage-bucket-prod',
    apiKey: 'sua-api-key-prod',
    authDomain: 'seu-auth-domain-prod',
    messagingSenderId: 'seu-messaging-sender-id-prod',
    measurementId: 'seu-measurement-id-prod', // Opcional
  }
};
```

### 2. Onde Encontrar as Configurações

No Firebase Console:

1. Vá em **Configurações do projeto** (ícone de engrenagem)
2. Role até **Seus apps**
3. Selecione seu app web
4. Em **Configuração do SDK**, você encontrará:

```javascript
const firebaseConfig = {
  apiKey: "AIza...",
  authDomain: "seu-projeto.firebaseapp.com",
  projectId: "seu-projeto",
  storageBucket: "seu-projeto.appspot.com",
  messagingSenderId: "123456789",
  appId: "1:123456789:web:abc123",
  measurementId: "G-ABC123DEF" // Opcional
};
```

### 3. Configurações de Segurança (Importante!)

#### Domínios Autorizados

1. No Firebase Console, vá em **Authentication**
2. Clique na aba **Settings**
3. Em **Authorized domains**, adicione:
   - `localhost` (para desenvolvimento)
   - Seu domínio de produção

#### Regras de Segurança do Firestore (Se usar)

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    // Permitir leitura/escrita apenas para usuários autenticados
    match /{document=**} {
      allow read, write: if request.auth != null;
    }
  }
}
```

## 🧪 Testando a Configuração

### 1. Verificar Console

Abra o console do navegador (F12) e verifique se não há erros relacionados ao Firebase.

### 2. Testar Login

1. Acesse a tela de login: `http://localhost:4200/auth/login`
2. Faça login com as credenciais fornecidas pelo administrador
3. Verifique se é redirecionado para o dashboard

**Nota**: A criação de novas contas só é possível através de convites (funcionalidade a ser implementada).

## 🔧 Comandos Úteis

```bash
# Instalar dependências (já feito)
npm install @angular/fire

# Rodar em desenvolvimento
npm start

# Build para produção
npm run build

# Visualizar build de produção
npm run build && npx http-server dist/jullius-app
```

## 🚨 Problemas Comuns

### 1. "FirebaseError: Missing or insufficient permissions"

- Verifique se as regras de segurança estão configuradas
- Confirme se o usuário está autenticado

### 2. "FirebaseError: Invalid API key"

- Verifique se a API key está correta no environment
- Confirme se o projeto Firebase está ativo

### 3. "FirebaseError: Domain not authorized"

- Adicione o domínio em Authentication > Settings > Authorized domains

### 4. Erro de CORS

- Verifique se está rodando na porta correta (4200)
- Confirme se o domínio está autorizado no Firebase

## 📚 Recursos Adicionais

- [Documentação Firebase Auth](https://firebase.google.com/docs/auth)
- [Angular Fire](https://github.com/angular/angularfire)
- [Firebase Console](https://console.firebase.google.com/)

## 🛡️ Segurança

⚠️ **IMPORTANTE**:

- Nunca commite as chaves do Firebase no repositório
- Use variáveis de ambiente em produção
- Configure regras de segurança adequadas
- Monitore o uso e custos no Firebase Console

---

## ✅ Checklist de Configuração

- [ ] Projeto Firebase criado
- [ ] Autenticação configurada (Email/Senha habilitado)
- [ ] App web registrado no Firebase
- [ ] Variáveis de ambiente configuradas
- [ ] Domínios autorizados adicionados
- [ ] Teste de login realizado
- [ ] Console sem erros Firebase

🎉 **Configuração concluída com sucesso!**
