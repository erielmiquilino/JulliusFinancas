# 🚀 Guia de Configuração Pós-Open Source

Este repositório foi sanitizado para segurança. Para que o deploy e a execução funcionem corretamente, você precisa configurar os segredos no GitHub e variáveis de ambiente.

## 🔑 GitHub Secrets Necessários

Adicione os seguintes segredos no seu repositório GitHub (`Settings` > `Secrets and variables` > `Actions`):

| Nome do Segredo | Descrição | Exemplo |
| :--- | :--- | :--- |
| `DB_PASSWORD` | Senha do banco de dados de produção | `Sup3rSecr3t!` |
| `DOCKER_REGISTRY` | URL do seu registry (ex: GHCR ou DockerHub) | `registry.seu-dominio.com` ou `ghcr.io` |
| `DOMAIN_NAME` | Domínio onde a aplicação será hospedada | `meu-app.com` |
| `REGISTRY_USER` | Usuário do Docker Registry | `seu-usuario` |
| `REGISTRY_PASSWORD` | Senha do Docker Registry | `sua-senha` |
| `SSH_HOST` | Host do servidor VPS | `192.168.1.100` |
| `SSH_USER` | Usuário SSH | `deploy` |
| `SSH_PRIVATE_KEY` | Chave privada SSH | `-----BEGIN OPENSSH PRIVATE KEY-----...` |
| `FIREBASE_API_KEY` | API Key do Firebase Project | `AIzaSy...` |
| `FIREBASE_AUTH_DOMAIN` | Domínio de Auth do Firebase | `seu-app.firebaseapp.com` |
| `FIREBASE_PROJECT_ID` | ID do Projeto Firebase | `seu-app-id` |
| `FIREBASE_STORAGE_BUCKET` | Bucket de Storage | `seu-app.appspot.com` |
| `FIREBASE_MESSAGING_SENDER_ID` | Sender ID do Messaging | `123456789` |
| `FIREBASE_APP_ID` | App ID do Firebase | `1:123456789:web:abc...` |

## 🌍 Variáveis de Ambiente Locais

Para rodar localmente, crie um arquivo `.env` na raiz ou configure as variáveis no seu sistema/IDE:

```bash
# Exemplo de .env local (NÃO COMMITE ESTE ARQUIVO)
ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=5432;Database=jullius_financas;Username=postgres;Password=root"
FIREBASE_API_KEY="AIzaSy..."
# ... outras variáveis do Firebase
```

## 🐳 Ajustes no Deploy (GitHub Actions)

O arquivo `.github/workflows/deploy.yml` foi atualizado para injetar automaticamente as credenciais do Firebase no arquivo `.env` do servidor de produção durante o deploy. Certifique-se de que os segredos acima estejam configurados no GitHub.

## 📜 Histórico do Git

**Atenção:** O histórico de commits foi **resetado** para remover vazamentos de segredos antigos. Este repositório agora contém apenas um commit inicial limpo. Se você tiver cópias locais antigas com histórico, **não faça merge** delas; faça um novo clone deste repositório.
