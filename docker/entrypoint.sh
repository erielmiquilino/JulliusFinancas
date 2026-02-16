#!/bin/sh
set -e

echo "=== Iniciando Jullius Finanças ==="

# Função para aguardar o backend estar pronto
wait_for_backend() {
    echo "Aguardando backend estar pronto..."
    max_attempts=30
    attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        if curl -f http://localhost:8080/health > /dev/null 2>&1; then
            echo "Backend está pronto!"
            return 0
        fi
        
        attempt=$((attempt + 1))
        echo "Tentativa $attempt/$max_attempts - aguardando backend..."
        sleep 2
    done
    
    echo "Aviso: Backend não respondeu no tempo esperado, mas continuando..."
    return 1
}

# Iniciar backend em background
echo "Iniciando backend .NET..."
cd /app/api
dotnet Jullius.ServiceApi.dll &
BACKEND_PID=$!

# Aguardar backend estar pronto
wait_for_backend

# Verificar se o processo do backend ainda está rodando
if ! kill -0 $BACKEND_PID 2>/dev/null; then
    echo "ERRO: Processo do backend terminou inesperadamente!"
    exit 1
fi

echo "Backend iniciado com PID: $BACKEND_PID"

# Gerar env.js para o frontend (usando /api relativo para proxy do Nginx)
echo "Gerando env.js para frontend..."
mkdir -p /usr/share/nginx/html/assets
if [ -z "$FIREBASE_API_KEY" ]; then
        echo "ERRO: FIREBASE_API_KEY não definida no ambiente do container."
        exit 1
fi

cat > /usr/share/nginx/html/assets/env.js << EOF
(function (window) {
  window.env = window.env || {};

  // Output environment variables to object
  window.env.apiUrl = '/api';
  window.env.production = true;
  window.env.enableDebug = false;

  // Firebase config (should be passed as environment variables)
  window.env.firebaseProjectId = '${FIREBASE_PROJECT_ID}';
  window.env.firebaseAppId = '${FIREBASE_APP_ID}';
  window.env.firebaseStorageBucket = '${FIREBASE_STORAGE_BUCKET}';
  window.env.firebaseApiKey = '${FIREBASE_API_KEY}';
  window.env.firebaseAuthDomain = '${FIREBASE_AUTH_DOMAIN}';
  window.env.firebaseMessagingSenderId = '${FIREBASE_MESSAGING_SENDER_ID}';
})(this);
EOF

# Função para cleanup quando o container for parado
cleanup() {
    echo "Recebido sinal de parada, finalizando processos..."
    kill $BACKEND_PID 2>/dev/null || true
    nginx -s quit 2>/dev/null || true
    exit 0
}

# Registrar handler de sinais (sintaxe compatível com sh)
trap 'cleanup' TERM INT

# Iniciar Nginx em foreground (mantém container vivo)
echo "Iniciando Nginx..."
exec nginx -g "daemon off;"
