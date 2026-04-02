# Documentação da API - StyleScan

## 🔗 Base URL

```
Development: http://localhost:5000/api/v1
Production: https://api.stylescan.app/api/v1
```

## 🔐 Autenticação

Todas as requisições (exceto login/register) devem incluir o token JWT no header:

```
Authorization: Bearer <token>
```

## 📋 Endpoints

### Autenticação

#### Registrar Novo Usuário

```http
POST /auth/register
Content-Type: application/json

{
  "email": "usuario@example.com",
  "password": "senha123",
  "firstName": "João",
  "lastName": "Silva",
  "dateOfBirth": "1995-05-15"
}
```

**Response (201 Created):**
```json
{
  "id": "uuid",
  "email": "usuario@example.com",
  "firstName": "João",
  "lastName": "Silva",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 1440
}
```

#### Login

```http
POST /auth/login
Content-Type: application/json

{
  "email": "usuario@example.com",
  "password": "senha123"
}
```

**Response (200 OK):**
```json
{
  "id": "uuid",
  "email": "usuario@example.com",
  "firstName": "João",
  "lastName": "Silva",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 1440
}
```

#### Refresh Token

```http
POST /auth/refresh
Content-Type: application/json

{
  "refreshToken": "eyJhbGciOiJIUzI1NiIs..."
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 1440
}
```

#### Logout

```http
POST /auth/logout
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "message": "Logout realizado com sucesso"
}
```

### Avatares

#### Criar Avatar

```http
POST /avatar/create
Authorization: Bearer <token>
Content-Type: multipart/form-data

{
  "photo": <arquivo>,
  "gender": "male|female",
  "bodyType": "slim|athletic|average|curvy",
  "skinTone": "light|medium|dark",
  "name": "Meu Avatar"
}
```

**Response (201 Created):**
```json
{
  "id": "uuid",
  "userId": "uuid",
  "name": "Meu Avatar",
  "modelUrl": "https://models.readyplayer.me/...",
  "gender": "male",
  "bodyType": "athletic",
  "skinTone": "medium",
  "createdAt": "2026-03-21T10:30:00Z",
  "updatedAt": "2026-03-21T10:30:00Z"
}
```

#### Listar Avatares do Usuário

```http
GET /avatar/list
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Meu Avatar",
      "modelUrl": "https://models.readyplayer.me/...",
      "gender": "male",
      "bodyType": "athletic",
      "createdAt": "2026-03-21T10:30:00Z"
    }
  ],
  "total": 1
}
```

#### Obter Detalhes do Avatar

```http
GET /avatar/{id}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "id": "uuid",
  "userId": "uuid",
  "name": "Meu Avatar",
  "modelUrl": "https://models.readyplayer.me/...",
  "gender": "male",
  "bodyType": "athletic",
  "skinTone": "medium",
  "measurements": {
    "height": 180,
    "chest": 95,
    "waist": 85,
    "hips": 92
  },
  "createdAt": "2026-03-21T10:30:00Z",
  "updatedAt": "2026-03-21T10:30:00Z"
}
```

#### Atualizar Avatar

```http
PUT /avatar/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Novo Nome",
  "bodyType": "curvy",
  "measurements": {
    "height": 165,
    "chest": 90,
    "waist": 80,
    "hips": 95
  }
}
```

**Response (200 OK):**
```json
{
  "id": "uuid",
  "name": "Novo Nome",
  "bodyType": "curvy",
  "measurements": {
    "height": 165,
    "chest": 90,
    "waist": 80,
    "hips": 95
  },
  "updatedAt": "2026-03-21T11:00:00Z"
}
```

#### Deletar Avatar

```http
DELETE /avatar/{id}
Authorization: Bearer <token>
```

**Response (204 No Content)**

### Looks

#### Gerar Looks Recomendados

```http
POST /looks/generate
Authorization: Bearer <token>
Content-Type: application/json

{
  "avatarId": "uuid",
  "occasion": "casual|formal|party|work|weekend",
  "style": "minimalist|classic|trendy|bohemian|sporty",
  "season": "spring|summer|fall|winter",
  "colorPreferences": ["blue", "black", "white"],
  "budget": 500
}
```

**Response (201 Created):**
```json
{
  "data": [
    {
      "id": "uuid",
      "avatarId": "uuid",
      "name": "Casual Weekend Look",
      "occasion": "weekend",
      "style": "trendy",
      "items": [
        {
          "id": "uuid",
          "name": "Jeans Azul",
          "category": "pants",
          "color": "blue",
          "price": 150,
          "storeId": "uuid",
          "storeName": "Loja X",
          "productUrl": "https://..."
        }
      ],
      "totalPrice": 450,
      "createdAt": "2026-03-21T10:30:00Z"
    }
  ],
  "total": 5
}
```

#### Listar Looks do Usuário

```http
GET /looks/list?avatarId={avatarId}&occasion={occasion}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Casual Weekend Look",
      "occasion": "weekend",
      "style": "trendy",
      "totalPrice": 450,
      "itemCount": 4,
      "createdAt": "2026-03-21T10:30:00Z"
    }
  ],
  "total": 12
}
```

#### Obter Detalhes do Look

```http
GET /looks/{id}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "id": "uuid",
  "avatarId": "uuid",
  "name": "Casual Weekend Look",
  "occasion": "weekend",
  "style": "trendy",
  "season": "spring",
  "items": [
    {
      "id": "uuid",
      "name": "Jeans Azul",
      "category": "pants",
      "color": "blue",
      "price": 150,
      "storeId": "uuid",
      "storeName": "Loja X",
      "productUrl": "https://...",
      "imageUrl": "https://..."
    }
  ],
  "totalPrice": 450,
  "createdAt": "2026-03-21T10:30:00Z"
}
```

#### Adicionar Look aos Favoritos

```http
POST /looks/{id}/favorite
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "message": "Look adicionado aos favoritos"
}
```

#### Remover Look dos Favoritos

```http
DELETE /looks/{id}/favorite
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "message": "Look removido dos favoritos"
}
```

### Loja

#### Listar Produtos

```http
GET /shop/products?category={category}&price_min={min}&price_max={max}&page={page}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Jeans Azul",
      "category": "pants",
      "price": 150,
      "color": "blue",
      "sizes": ["XS", "S", "M", "L", "XL"],
      "imageUrl": "https://...",
      "storeId": "uuid",
      "storeName": "Loja X",
      "rating": 4.5,
      "reviews": 120
    }
  ],
  "total": 250,
  "page": 1,
  "pageSize": 20
}
```

#### Obter Detalhes do Produto

```http
GET /shop/products/{id}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "id": "uuid",
  "name": "Jeans Azul",
  "category": "pants",
  "price": 150,
  "color": "blue",
  "description": "Jeans de alta qualidade...",
  "sizes": ["XS", "S", "M", "L", "XL"],
  "imageUrls": ["https://...", "https://..."],
  "storeId": "uuid",
  "storeName": "Loja X",
  "storeUrl": "https://lojax.com",
  "rating": 4.5,
  "reviews": 120,
  "inStock": true,
  "sku": "JEANS-BLUE-001"
}
```

#### Criar Pedido

```http
POST /shop/orders
Authorization: Bearer <token>
Content-Type: application/json

{
  "lookId": "uuid",
  "items": [
    {
      "productId": "uuid",
      "quantity": 1,
      "size": "M"
    }
  ],
  "shippingAddress": {
    "street": "Rua A, 123",
    "city": "São Paulo",
    "state": "SP",
    "zipCode": "01310-100",
    "country": "Brazil"
  }
}
```

**Response (201 Created):**
```json
{
  "id": "uuid",
  "userId": "uuid",
  "lookId": "uuid",
  "items": [
    {
      "productId": "uuid",
      "quantity": 1,
      "price": 150
    }
  ],
  "totalPrice": 150,
  "status": "pending",
  "createdAt": "2026-03-21T10:30:00Z"
}
```

### Usuário

#### Obter Perfil

```http
GET /user/profile
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "id": "uuid",
  "email": "usuario@example.com",
  "firstName": "João",
  "lastName": "Silva",
  "dateOfBirth": "1995-05-15",
  "gender": "male",
  "profileImageUrl": "https://...",
  "createdAt": "2026-03-21T10:30:00Z"
}
```

#### Atualizar Perfil

```http
PUT /user/profile
Authorization: Bearer <token>
Content-Type: application/json

{
  "firstName": "João",
  "lastName": "Silva",
  "profileImageUrl": "https://...",
  "bio": "Amante de moda"
}
```

**Response (200 OK):**
```json
{
  "id": "uuid",
  "email": "usuario@example.com",
  "firstName": "João",
  "lastName": "Silva",
  "bio": "Amante de moda",
  "updatedAt": "2026-03-21T11:00:00Z"
}
```

#### Obter Histórico de Compras

```http
GET /user/orders?page={page}&limit={limit}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "data": [
    {
      "id": "uuid",
      "totalPrice": 450,
      "status": "delivered",
      "createdAt": "2026-03-20T10:30:00Z"
    }
  ],
  "total": 5,
  "page": 1
}
```

## 🔍 Códigos de Status HTTP

| Código | Significado |
|--------|------------|
| 200 | OK - Requisição bem-sucedida |
| 201 | Created - Recurso criado com sucesso |
| 204 | No Content - Requisição bem-sucedida, sem conteúdo |
| 400 | Bad Request - Requisição inválida |
| 401 | Unauthorized - Autenticação necessária |
| 403 | Forbidden - Acesso negado |
| 404 | Not Found - Recurso não encontrado |
| 409 | Conflict - Conflito (ex: email já existe) |
| 500 | Internal Server Error - Erro no servidor |

## 🚨 Tratamento de Erros

Todas as respostas de erro seguem este formato:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Descrição do erro",
    "details": {
      "field": "Informações adicionais"
    }
  }
}
```

**Exemplo:**
```json
{
  "error": {
    "code": "INVALID_EMAIL",
    "message": "Email inválido",
    "details": {
      "email": "O formato do email é inválido"
    }
  }
}
```

## 📝 Paginação

Endpoints que retornam listas suportam paginação:

```
GET /endpoint?page=1&limit=20
```

**Response:**
```json
{
  "data": [...],
  "total": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

## 🔄 Rate Limiting

- **Limite:** 100 requisições por minuto por usuário
- **Header:** `X-RateLimit-Remaining`, `X-RateLimit-Reset`

## 📚 Exemplos com cURL

### Login

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "usuario@example.com",
    "password": "senha123"
  }'
```

### Criar Avatar

```bash
curl -X POST http://localhost:5000/api/v1/avatar/create \
  -H "Authorization: Bearer <token>" \
  -F "photo=@/path/to/photo.jpg" \
  -F "gender=male" \
  -F "bodyType=athletic" \
  -F "name=Meu Avatar"
```

### Gerar Looks

```bash
curl -X POST http://localhost:5000/api/v1/looks/generate \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "avatarId": "uuid",
    "occasion": "casual",
    "style": "trendy",
    "season": "spring",
    "budget": 500
  }'
```

---

**Última atualização:** 21 de março de 2026
