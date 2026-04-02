# Deploy do Backend no Railway

## Estrutura recomendada

- Crie um novo projeto no Railway
- Conecte o repositorio do GitHub
- Use `backend` como `Root Directory`
- Use o `Dockerfile` desta pasta como builder

## Se aparecer o erro do Railpack

Se o deploy falhar com `Error creating build plan with Railpack`:

1. Abra o servico no Railway
2. Entre em `Settings`
3. Procure a configuracao de `Builder`
4. Troque de `Railpack/Nixpacks` para `Dockerfile`
5. Confirme que o `Root Directory` esta como `backend`
6. Rode um novo deploy

## Variaveis de ambiente

Configure estas variaveis no servico:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true`
- `Jwt__SecretKey=coloque-uma-chave-forte-com-32-caracteres-ou-mais`
- `Jwt__Issuer=stylescan-api`
- `Jwt__Audience=stylescan-app`
- `ExternalApis__OpenAI__ApiKey=...`
- `ExternalApis__OpenAI__Model=gpt-image-1-mini`
- `ExternalApis__OpenAI__AnalysisModel=gpt-4.1-mini`
- `ExternalApis__OpenAI__BaseUrl=https://api.openai.com/v1`
- `ExternalApis__MercadoPago__AccessToken=...`
- `ExternalApis__MercadoPago__PublicKey=...`
- `ExternalApis__MercadoPago__WebhookSecret=...`
- `ExternalApis__MercadoPago__BaseUrl=https://api.mercadopago.com`
- `ExternalApis__MercadoPago__FrontBaseUrl=https://stylescan2000.netlify.app`
- `ExternalApis__MercadoPago__PublicFrontBaseUrl=https://stylescan2000.netlify.app`
- `ExternalApis__MercadoPago__NotificationUrl=https://SEU-BACKEND/api/v1/payments/mercado-pago/webhook`
- `Cors__AllowedOrigins__0=https://stylescan2000.netlify.app`

Se depois voce usar dominio proprio, troque as URLs acima para o dominio final.

## Banco de dados

Voce pode:

1. Criar um PostgreSQL no proprio Railway
2. Ou usar Neon/Supabase e colar a connection string em `ConnectionStrings__DefaultConnection`

## Checklist final

1. Deploy do backend concluido
2. URL publica do backend abrindo `/health`
3. `ExternalApis__MercadoPago__NotificationUrl` apontando para a URL publica final
4. Webhook do Mercado Pago configurado para a mesma URL
5. Frontend publico recompilado apontando para a API estavel

## Testes rapidos

- `GET https://SEU-BACKEND/health`
- `GET https://SEU-BACKEND/api/v1/public/profiles/slug`
- `GET https://SEU-BACKEND/api/v1/public/looks/slug`
