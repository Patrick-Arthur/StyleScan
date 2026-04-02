# Arquitetura do StyleScan

## 🏗️ Visão Geral

O StyleScan segue uma arquitetura em camadas (layered architecture) com separação clara entre frontend, backend e serviços externos de IA. Esta abordagem garante escalabilidade, manutenibilidade e flexibilidade para futuras evoluções.

## 📐 Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                    Camada de Apresentação                        │
│              (Ionic/Angular - Mobile-First)                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   Auth UI    │  │  Avatar UI   │  │  Looks UI    │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└─────────────────────────────────────────────────────────────────┘
                             ↓ HTTP/REST
┌─────────────────────────────────────────────────────────────────┐
│                    Camada de API (.NET 8)                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │            Controllers (Endpoints)                       │   │
│  │  /auth  /avatar  /looks  /shop  /users                  │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │            Services (Business Logic)                    │   │
│  │  AuthService  AvatarService  LooksService  ShopService  │   │
│  └──────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │            Middleware & Cross-Cutting Concerns          │   │
│  │  Authentication  Authorization  Error Handling  Logging │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
        ↓                           ↓                    ↓
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│  PostgreSQL DB   │    │  Serviços de IA  │    │  Serviços Ext.   │
│                  │    │                  │    │                  │
│  - Users         │    │  - OpenAI        │    │  - Ready Player  │
│  - Avatars       │    │  - Replicate     │    │  - 3DLOOK        │
│  - Looks         │    │  - Hugging Face  │    │  - Stripe        │
│  - Clothing      │    │                  │    │                  │
│  - Stores        │    │                  │    │                  │
└──────────────────┘    └──────────────────┘    └──────────────────┘
```

## 🔄 Fluxo de Dados

### 1. Autenticação e Registro

```
1. Usuário insere credenciais (Frontend)
   ↓
2. Frontend envia POST /api/auth/register ou /api/auth/login
   ↓
3. Backend valida credenciais e gera JWT token
   ↓
4. Token retornado ao frontend e armazenado (localStorage/sessionStorage)
   ↓
5. Requisições subsequentes incluem token no header Authorization
```

### 2. Criação de Avatar

```
1. Usuário faz upload de foto (Frontend)
   ↓
2. Frontend envia POST /api/avatar/create com imagem
   ↓
3. Backend processa imagem e chama OpenAI/Replicate para gerar avatar 3D
   ↓
4. Avatar 3D salvo no banco de dados
   ↓
5. URL do avatar retornada ao frontend
   ↓
6. Frontend exibe avatar 3D usando Ready Player Me
```

### 3. Geração de Looks

```
1. Usuário seleciona preferências de estilo (Frontend)
   ↓
2. Frontend envia POST /api/looks/generate com preferências
   ↓
3. Backend consulta banco de dados para roupas compatíveis
   ↓
4. Backend chama OpenAI para gerar recomendações
   ↓
5. Looks gerados salvos no banco de dados
   ↓
6. Lista de looks retornada ao frontend
   ↓
7. Frontend exibe looks com try-on virtual
```

## 🗂️ Estrutura de Diretórios Detalhada

### Frontend (Ionic/Angular)

```
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── guards/
│   │   │   │   ├── auth.guard.ts
│   │   │   │   └── role.guard.ts
│   │   │   ├── interceptors/
│   │   │   │   ├── auth.interceptor.ts
│   │   │   │   └── error.interceptor.ts
│   │   │   ├── services/
│   │   │   │   ├── auth.service.ts
│   │   │   │   ├── api.service.ts
│   │   │   │   └── storage.service.ts
│   │   │   └── core.module.ts
│   │   ├── shared/
│   │   │   ├── components/
│   │   │   │   ├── header/
│   │   │   │   ├── footer/
│   │   │   │   └── loading-spinner/
│   │   │   ├── pipes/
│   │   │   │   └── currency.pipe.ts
│   │   │   ├── directives/
│   │   │   └── shared.module.ts
│   │   ├── modules/
│   │   │   ├── auth/
│   │   │   │   ├── pages/
│   │   │   │   │   ├── login/
│   │   │   │   │   ├── register/
│   │   │   │   │   └── forgot-password/
│   │   │   │   ├── services/
│   │   │   │   └── auth.module.ts
│   │   │   ├── avatar/
│   │   │   │   ├── pages/
│   │   │   │   │   ├── avatar-create/
│   │   │   │   │   ├── avatar-customize/
│   │   │   │   │   └── avatar-list/
│   │   │   │   ├── components/
│   │   │   │   │   └── avatar-viewer/
│   │   │   │   ├── services/
│   │   │   │   └── avatar.module.ts
│   │   │   ├── looks/
│   │   │   │   ├── pages/
│   │   │   │   │   ├── looks-list/
│   │   │   │   │   ├── looks-detail/
│   │   │   │   │   └── looks-create/
│   │   │   │   ├── components/
│   │   │   │   │   ├── look-card/
│   │   │   │   │   └── try-on-viewer/
│   │   │   │   ├── services/
│   │   │   │   └── looks.module.ts
│   │   │   ├── shop/
│   │   │   │   ├── pages/
│   │   │   │   │   ├── shop-list/
│   │   │   │   │   ├── product-detail/
│   │   │   │   │   └── checkout/
│   │   │   │   ├── services/
│   │   │   │   └── shop.module.ts
│   │   │   └── user/
│   │   │       ├── pages/
│   │   │       │   ├── profile/
│   │   │       │   ├── favorites/
│   │   │       │   └── history/
│   │   │       ├── services/
│   │   │       └── user.module.ts
│   │   ├── app-routing.module.ts
│   │   ├── app.component.ts
│   │   └── app.module.ts
│   ├── assets/
│   │   ├── images/
│   │   ├── icons/
│   │   └── fonts/
│   ├── environments/
│   │   ├── environment.ts
│   │   └── environment.prod.ts
│   ├── styles/
│   │   ├── global.scss
│   │   └── variables.scss
│   └── main.ts
├── package.json
├── angular.json
├── ionic.config.json
└── tsconfig.json
```

### Backend (.NET 8)

```
backend/
├── Controllers/
│   ├── AuthController.cs
│   ├── AvatarController.cs
│   ├── LooksController.cs
│   ├── ShopController.cs
│   ├── UsersController.cs
│   └── HealthController.cs
├── Models/
│   ├── Domain/
│   │   ├── User.cs
│   │   ├── Avatar.cs
│   │   ├── Look.cs
│   │   ├── Clothing.cs
│   │   ├── Store.cs
│   │   └── UserPreference.cs
│   └── DTOs/
│       ├── Auth/
│       │   ├── LoginRequest.cs
│       │   ├── RegisterRequest.cs
│       │   └── AuthResponse.cs
│       ├── Avatar/
│       │   ├── CreateAvatarRequest.cs
│       │   └── AvatarResponse.cs
│       ├── Looks/
│       │   ├── GenerateLooksRequest.cs
│       │   └── LookResponse.cs
│       └── Shop/
│           ├── ProductResponse.cs
│           └── OrderRequest.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IAvatarService.cs
│   │   ├── ILooksService.cs
│   │   ├── IShopService.cs
│   │   ├── IAIService.cs
│   │   └── IUserService.cs
│   └── Implementations/
│       ├── AuthService.cs
│       ├── AvatarService.cs
│       ├── LooksService.cs
│       ├── ShopService.cs
│       ├── AIService.cs
│       └── UserService.cs
├── Data/
│   ├── StyleScanDbContext.cs
│   ├── Migrations/
│   │   └── [Migration files]
│   └── Repositories/
│       ├── IRepository.cs
│       ├── Repository.cs
│       └── [Specific repositories]
├── Middleware/
│   ├── ErrorHandlingMiddleware.cs
│   ├── AuthenticationMiddleware.cs
│   └── LoggingMiddleware.cs
├── Utilities/
│   ├── JwtTokenGenerator.cs
│   ├── PasswordHasher.cs
│   ├── ImageProcessor.cs
│   └── Constants.cs
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── backend.csproj
```

## 🔐 Segurança

### Autenticação

- **JWT (JSON Web Tokens):** Tokens com expiração configurável
- **Refresh Tokens:** Para renovação segura de sessões
- **Password Hashing:** Bcrypt ou PBKDF2 para armazenamento seguro

### Autorização

- **Role-Based Access Control (RBAC):** Admin, User, Premium User
- **Claims-Based Authorization:** Permissões granulares por funcionalidade

### Proteção de Dados

- **HTTPS/TLS:** Todas as comunicações criptografadas
- **CORS:** Configuração restritiva de origens permitidas
- **Input Validation:** Validação em frontend e backend
- **SQL Injection Prevention:** Uso de ORM (Entity Framework)

## 📊 Padrões de Design

### Backend

- **Repository Pattern:** Abstração de acesso a dados
- **Dependency Injection:** Inversão de controle via DI container
- **Service Layer:** Separação de lógica de negócio
- **DTO Pattern:** Transferência de dados entre camadas
- **Middleware Pattern:** Cross-cutting concerns

### Frontend

- **Component-Based Architecture:** Componentes reutilizáveis
- **Smart/Dumb Components:** Componentes inteligentes e apresentacionais
- **Service-Based State Management:** Serviços centralizados
- **RxJS Observables:** Programação reativa

## 🚀 Escalabilidade

### Banco de Dados

- **Índices:** Otimização de queries frequentes
- **Particionamento:** Divisão de dados por usuário ou data
- **Caching:** Redis para dados frequentemente acessados
- **Read Replicas:** Para distribuição de carga de leitura

### Backend

- **Horizontal Scaling:** Múltiplas instâncias atrás de load balancer
- **Async/Await:** Processamento não-bloqueante
- **Queue System:** Filas para processamento de IA assíncrono
- **Microservices (Futuro):** Separação de serviços de IA

### Frontend

- **Lazy Loading:** Carregamento sob demanda de módulos
- **Code Splitting:** Divisão de bundle por rota
- **Service Workers:** Caching offline
- **Progressive Enhancement:** Funcionalidades degradáveis

## 🔄 CI/CD Pipeline

```
Code Push → GitHub → GitHub Actions
    ↓
    └─→ Lint & Format Check
    └─→ Unit Tests
    └─→ Integration Tests
    └─→ Build
    └─→ Docker Build (opcional)
    └─→ Deploy to Staging
    └─→ Smoke Tests
    └─→ Deploy to Production
```

## 📈 Monitoramento e Logging

- **Application Insights:** Monitoramento de performance
- **Serilog:** Logging estruturado no backend
- **Error Tracking:** Sentry ou similar
- **Analytics:** Mixpanel ou Google Analytics
- **Alertas:** Notificações de erros críticos

## 🔮 Evolução Futura

### Curto Prazo (3-6 meses)
- Implementação completa do MVP
- Integração com 2-3 lojas parceiras
- Testes de usuário e feedback

### Médio Prazo (6-12 meses)
- Expansão de parcerias com lojas
- Funcionalidades sociais (compartilhamento de looks)
- Aplicativo nativo (React Native)

### Longo Prazo (12+ meses)
- Microserviços para serviços de IA
- Machine Learning para recomendações personalizadas
- Integração com redes sociais
- Marketplace próprio

---

**Última atualização:** 21 de março de 2026
