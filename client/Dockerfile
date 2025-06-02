# Estágio de build
FROM node:slim AS build
WORKDIR /app

# Copiar package.json e package-lock.json
COPY package*.json ./

# Instalar dependências
RUN npm install

# Copiar o restante dos arquivos da aplicação
COPY . .

# Construir a aplicação para produção
RUN npm run build --prod

# Estágio de produção (servir com Nginx)
FROM nginx:alpine

# Copiar os arquivos de build do estágio anterior para o diretório do Nginx
COPY --from=build /app/dist/jullius-app/browser /usr/share/nginx/html

# Copiar uma configuração personalizada do Nginx, se necessário (opcional)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expor a porta 80
EXPOSE 80

# Comando para iniciar o Nginx
CMD ["nginx", "-g", "daemon off;"]
