# StyleScan Landing Deploy

## Vercel

1. Importe a pasta `frontend` no Vercel.
2. Framework preset: `Other` ou `Angular`.
3. Build command: `npm run build:public`
4. Output directory: `www`

O arquivo `vercel.json` ja cuida do fallback das rotas do app para `index.html`.
Esse build publico entrega apenas a landing page, a politica de privacidade e os termos de uso.

## Netlify

1. Crie um novo site a partir da pasta `frontend`.
2. Build command: `npm run build:public`
3. Publish directory: `www`

O arquivo `netlify.toml` ja cuida do fallback das rotas do app para `index.html`.
Esse build publico entrega apenas a landing page, a politica de privacidade e os termos de uso.

## URL publica

Depois do deploy, use a URL publica da landing:

- `https://seu-dominio/`

As paginas institucionais ficam em:

- `https://seu-dominio/privacy`
- `https://seu-dominio/terms`
