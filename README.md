# StyleScan - Virtual Try-On com IA

Um aplicativo revolucionário que usa scanner corporal e IA para criar avatares 3D realistas, recomendar looks personalizados e oferecer experimentação virtual de roupas, integrando-se a lojas reais para facilitar a compra online.

## 🎯 Objetivo

Transformar a experiência de compra online de roupas, reduzindo devoluções e aumentando a satisfação do usuário através de avatares 3D precisos e recomendações de estilo personalizadas.

## 🚀 Stack Tecnológico

| Componente | Tecnologia | Versão |
|-----------|-----------|--------|
| **Frontend** | Ionic + Angular | Latest |
| **Backend** | .NET | 8.0 |
| **Banco de Dados** | PostgreSQL / SQL Server | Latest |
| **IA/ML** | OpenAI + Replicate | APIs |
| **3D Avatars** | Ready Player Me | API |
| **Autenticação** | JWT + OAuth 2.0 | - |

## 📋 Pré-requisitos

Antes de começar, certifique-se de que você tem instalado:

- **Node.js** (v22+) e npm
- **.NET SDK 8.0** ou superior
- **Git**
- **PostgreSQL** ou **SQL Server** (para banco de dados local)

## 🛠️ Setup Inicial

### 1. Clone o Repositório

```bash
cd /home/ubuntu/stylescan
```

### 2. Setup do Frontend (Ionic/Angular)

```bash
cd frontend

# Instalar dependências
npm install

# Executar o servidor de desenvolvimento
ionic serve

# Ou para compilar para produção
ionic build --prod
```

O aplicativo estará disponível em `http://localhost:4200`.

### 3. Setup do Backend (.NET 8)

```bash
cd backend

# Restaurar dependências
dotnet restore

# Executar o servidor de desenvolvimento
dotnet run

# Ou compilar para produção
dotnet publish -c Release
```

A API estará disponível em `https://localhost:7000` (HTTPS) ou `http://localhost:5000` (HTTP).

### 4. Configurar Banco de Dados

```bash
# No diretório backend, executar migrações (quando criadas)
dotnet ef database update
```

## 📁 Estrutura do Projeto

```
stylescan/
├── frontend/          # Aplicativo Ionic/Angular
├── backend/           # API .NET 8
├── docs/              # Documentação
├── README.md          # Este arquivo
└── PROJECT_STRUCTURE.md  # Detalhes da estrutura
```

Para mais detalhes sobre a estrutura, consulte [PROJECT_STRUCTURE.md](./PROJECT_STRUCTURE.md).

## 🔐 Variáveis de Ambiente

### Frontend

Criar arquivo `frontend/src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  openaiApiKey: 'your-openai-key',
  replicateApiKey: 'your-replicate-key',
};
```

### Backend

Criar arquivo `backend/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=stylescan_dev;User Id=sa;Password=YourPassword123;"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "stylescan",
    "Audience": "stylescan-app"
  },
  "ExternalApis": {
    "OpenAI": {
      "ApiKey": "your-openai-key"
    },
    "Replicate": {
      "ApiKey": "your-replicate-key"
    }
  }
}
```

## 🧪 Testes

### Frontend

```bash
cd frontend

# Executar testes unitários
npm run test

# Executar testes com cobertura
npm run test -- --code-coverage
```

### Backend

```bash
cd backend

# Executar testes
dotnet test
```

## 📚 Documentação

- [API Documentation](./docs/API.md) - Endpoints e modelos da API
- [Database Schema](./docs/DATABASE.md) - Estrutura do banco de dados
- [Architecture Decisions](./docs/ARCHITECTURE.md) - Decisões técnicas
- [Setup Guide](./docs/SETUP.md) - Guia detalhado de setup

## 🔄 Fluxo de Desenvolvimento

1. **Criar branch:** `git checkout -b feature/nome-da-feature`
2. **Fazer alterações:** Implementar a funcionalidade
3. **Testar:** Executar testes localmente
4. **Commit:** `git commit -m "feat: descrição da mudança"`
5. **Push:** `git push origin feature/nome-da-feature`
6. **Pull Request:** Criar PR para revisão

## 🚢 Deploy

### Frontend

```bash
cd frontend

# Build para produção
ionic build --prod

# Deploy para Netlify, Vercel ou outro serviço
```

### Backend

```bash
cd backend

# Publicar para produção
dotnet publish -c Release -o ./publish

# Deploy para Azure, AWS, Heroku, etc.
```

## 🐛 Troubleshooting

### Erro: "dotnet: command not found"

```bash
# Instalar .NET SDK 8
sudo apt-get install dotnet-sdk-8.0
```

### Erro: "ionic: command not found"

```bash
# Instalar Ionic CLI globalmente
npm install -g @ionic/cli
```

### Erro de conexão com banco de dados

Verificar se o PostgreSQL/SQL Server está rodando e as credenciais em `appsettings.Development.json` estão corretas.

## 📞 Suporte

Para dúvidas ou problemas, abra uma issue no repositório ou entre em contato com a equipe de desenvolvimento.

## 📄 Licença

Este projeto é propriedade do StyleScan. Todos os direitos reservados.

## 👥 Contribuidores

- **Manus AI** - Setup inicial e arquitetura

---

**Última atualização:** 21 de março de 2026
