# Deploy do Backend no Railway

## Estrutura recomendada

- Crie um novo projeto no Railway
- Aponte o serviço para a pasta `backend`
- Deixe o Railway usar `Nixpacks`
- O arquivo `nixpacks.toml` desta pasta já define build e start

## Variáveis de ambiente

Configure estas variáveis no serviço:

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

Se depois você usar domínio próprio, troque as URLs acima para:

- `https://stylescan.app`
- `https://api.stylescan.app`

## Banco de dados

Você pode:

1. Criar um PostgreSQL no próprio Railway
2. Ou usar Neon/Supabase e colar a connection string em `ConnectionStrings__DefaultConnection`

## Checklist final

1. Deploy do backend concluído
2. URL pública do backend abrindo `/health`
3. `ExternalApis__MercadoPago__NotificationUrl` apontando para a URL pública final
4. Webhook do Mercado Pago configurado para a mesma URL
5. Frontend público recompilado apontando para a API estável

## Testes rápidos

- `GET https://SEU-BACKEND/health`
- `GET https://SEU-BACKEND/api/v1/public/profiles/slug`
- `GET https://SEU-BACKEND/api/v1/public/looks/slug`

Quando a URL estável existir, atualize também o frontend público para ela.
