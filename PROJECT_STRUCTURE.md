# Estrutura do Projeto StyleScan

## Visão Geral

O projeto StyleScan segue uma arquitetura modular e escalável, separando claramente o frontend (Ionic/Angular), o backend (.NET 8) e a documentação.

## Estrutura de Diretórios

```
/home/ubuntu/stylescan/
├── frontend/                          # Aplicativo Ionic/Angular (Mobile-first)
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                 # Serviços e guards globais
│   │   │   ├── shared/               # Componentes e pipes compartilhados
│   │   │   ├── modules/              # Módulos de funcionalidades
│   │   │   │   ├── auth/
│   │   │   │   ├── avatar/
│   │   │   │   ├── looks/
│   │   │   │   ├── shop/
│   │   │   │   └── user/
│   │   │   └── app.component.ts
│   │   ├── assets/                   # Imagens, ícones, fontes
│   │   ├── environments/             # Configurações por ambiente
│   │   └── main.ts
│   ├── package.json
│   ├── angular.json
│   ├── ionic.config.json
│   ├── capacitor.config.ts
│   └── tsconfig.json
│
├── backend/                           # API .NET 8
│   ├── Controllers/                  # Endpoints da API
│   │   ├── AuthController.cs
│   │   ├── AvatarController.cs
│   │   ├── LooksController.cs
│   │   ├── ShopController.cs
│   │   └── UsersController.cs
│   ├── Models/                       # Modelos de dados
│   │   ├── User.cs
│   │   ├── Avatar.cs
│   │   ├── Look.cs
│   │   ├── Clothing.cs
│   │   └── Store.cs
│   ├── Services/                     # Lógica de negócio
│   │   ├── IAuthService.cs
│   │   ├── IAvatarService.cs
│   │   ├── ILooksService.cs
│   │   ├── IShopService.cs
│   │   └── IAIService.cs
│   ├── Data/                         # Contexto do banco de dados
│   │   ├── StyleScanDbContext.cs
│   │   └── Migrations/
│   ├── Middleware/                   # Middlewares customizados
│   ├── Utilities/                    # Funções utilitárias
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── backend.csproj
│
├── docs/                              # Documentação do projeto
│   ├── API.md                        # Documentação da API
│   ├── DATABASE.md                   # Schema do banco de dados
│   ├── ARCHITECTURE.md               # Decisões de arquitetura
│   └── SETUP.md                      # Instruções de setup
│
├── .gitignore
├── README.md
└── PROJECT_STRUCTURE.md              # Este arquivo
```

## Descrição dos Componentes

### Frontend (Ionic/Angular)

O frontend é uma aplicação mobile-first desenvolvida com Ionic e Angular, focando em uma experiência de usuário intuitiva e responsiva. A estrutura modular permite que cada funcionalidade (autenticação, avatar, looks, loja) seja desenvolvida e testada independentemente.

**Módulos principais:**
- **Auth:** Autenticação e registro de usuários.
- **Avatar:** Criação e personalização de avatares 3D.
- **Looks:** Geração e visualização de looks recomendados.
- **Shop:** Integração com catálogo de roupas e lojas.
- **User:** Perfil do usuário, histórico e favoritos.

### Backend (.NET 8)

O backend é uma API REST escalável desenvolvida em C# com .NET 8, responsável pela lógica de negócio, autenticação, integração com banco de dados e serviços de IA.

**Componentes principais:**
- **Controllers:** Endpoints da API para cada funcionalidade.
- **Services:** Implementação da lógica de negócio.
- **Models:** Representação dos dados do domínio.
- **Data:** Contexto do banco de dados e migrações (Entity Framework).
- **Middleware:** Tratamento de erros, autenticação, logging.

### Banco de Dados

O banco de dados será relacional (PostgreSQL ou SQL Server), com as seguintes tabelas principais:

- **Users:** Informações do usuário.
- **Avatars:** Dados dos avatares 3D criados.
- **Looks:** Combinações de roupas recomendadas.
- **Clothing:** Catálogo de peças de roupa.
- **Stores:** Informações das lojas parceiras.
- **UserPreferences:** Preferências de estilo do usuário.
- **AuditLog:** Histórico de ações do usuário.

## Convenções de Código

### Frontend (Angular/TypeScript)

- **Componentes:** `*.component.ts`, `*.component.html`, `*.component.scss`
- **Serviços:** `*.service.ts`
- **Módulos:** Cada funcionalidade em seu próprio módulo dentro de `modules/`
- **Naming:** camelCase para variáveis e funções, PascalCase para classes

### Backend (C# .NET)

- **Controllers:** PascalCase, sufixo `Controller`
- **Services:** PascalCase, interfaces prefixadas com `I`
- **Models:** PascalCase
- **Métodos:** PascalCase
- **Propriedades:** PascalCase com auto-properties

## Próximos Passos

1. **Configurar banco de dados:** Definir schema e criar migrações iniciais.
2. **Implementar autenticação:** JWT e OAuth 2.0 no backend.
3. **Criar modelos de dados:** Definir entidades e relacionamentos.
4. **Desenvolver componentes base:** Layouts, headers, footers.
5. **Integrar serviços de IA:** Conectar OpenAI e Replicate.

## Recursos Úteis

- [Ionic Documentation](https://ionicframework.com/docs)
- [Angular Documentation](https://angular.io/docs)
- [.NET 8 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
