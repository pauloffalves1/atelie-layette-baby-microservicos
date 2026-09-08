# Ateliê Layette Baby — exercício de microsserviços

Decomposição de aprendizado do monólito em `server/`/`client/` — **não afeta a produção**
(layettebaby.com.br continua no monólito). Roda inteiramente aqui, em `microservices/`, com seu
próprio banco por serviço, mensageria e chaves.

## Arquitetura

| Serviço | Dono de | Porta interna |
|---|---|---|
| `identity` | Admin (+2FA), Customer (auth, CPF, verificação de e-mail) — assina os JWTs (RS256) | 8080 |
| `catalog` | Product, GalleryImage, SiteImage, ProductReview, WishlistItem, upload de arquivos | 8080 |
| `orders` | Order, Coupon, CartSnapshot (+lembrete de carrinho abandonado), pagamento (PagBank) | 8080 |
| `backoffice` | ContactMessage, NewsletterSubscriber, AuditLog, Dashboard (agregação), Sitemap | 8080 |
| `notifications` | Worker — consome eventos do RabbitMQ, envia WhatsApp/e-mail | 8080 (só health) |
| `gateway` | YARP — único ponto de entrada HTTP, roteia por prefixo de path | 8080 |

`shared/AtelieBebe.SharedKernel`: `Entity`/`ValueObject`/exceções, infraestrutura do outbox
transacional, cliente RabbitMQ (publish/subscribe), autenticação JWT compartilhada (RS256).

Cada serviço grava seus próprios eventos de domínio na própria tabela outbox (mesma transação da
mudança de estado) e um `OutboxPublisherService` os publica no RabbitMQ (exchange `atelie.events`,
routing key = nome do evento). `Notifications` e `Backoffice` (auditoria) consomem o que interessa.
Chamadas síncronas entre serviços existem só onde é inevitável (Orders→Catalog para validar preço,
Catalog→Orders para elegibilidade de review, Backoffice→Orders/Catalog/Identity para o dashboard) —
o resto é resolvido no frontend, chamando o Gateway por serviço.

## Rodando localmente com `dotnet run` (sem Docker)

Pré-requisito único: gerar o par de chaves RSA (uma vez):

```bash
cd keys
openssl genrsa -out jwt-private.pem 2048
openssl rsa -in jwt-private.pem -pubout -out jwt-public.pem
```

Cada serviço tem seu `appsettings.Development.json` já apontando pra `localhost` nas portas abaixo.
Rode cada um (na pasta `Api`/`Worker` do serviço) com `ASPNETCORE_ENVIRONMENT=Development`:

| Serviço | Porta sugerida |
|---|---|
| identity | 5199 |
| catalog | 5200 |
| orders | 5121 |
| backoffice | 5122 |
| notifications | 5123 |
| gateway | 5100 |

RabbitMQ precisa estar rodando (`docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:4-management`)
para o outbox/auditoria funcionar — sem ele, tudo continua funcionando (logins, pedidos, listagem),
só a publicação de eventos falha graciosamente (log + retry) e é descartada depois de 5 tentativas.

Depois de tudo no ar, `http://localhost:5100/api/...` é o único endereço que o frontend precisa
conhecer.

## Rodando com Docker Compose

```bash
docker compose build
docker compose up -d
```

Sobe RabbitMQ + os 6 serviços com os hostnames internos (`http://catalog:8080` etc.) já
configurados em cada `appsettings.json`. Gateway exposto em `http://localhost:5100`. Painel do
RabbitMQ em `http://localhost:15672` (guest/guest). Dados persistem em volumes nomeados
(`docker compose down` sem `-v` preserva os bancos).

## Rodando no Kubernetes (Docker Desktop)

Habilite o Kubernetes em Docker Desktop → Settings → Kubernetes (usa o mesmo storage de imagens do
`docker compose build`, sem precisar de um registry). Depois:

```bash
kubectl apply -f k8s/00-namespace.yaml
kubectl create secret generic jwt-keys -n atelie-bebe-dev \
  --from-file=jwt-private.pem=keys/jwt-private.pem \
  --from-file=jwt-public.pem=keys/jwt-public.pem
kubectl apply -f k8s/
kubectl get pods -n atelie-bebe-dev
```

O Service do Gateway é `NodePort` (porta 30500), mas o Kubernetes do Docker Desktop nem sempre expõe
NodePorts automaticamente no `localhost` do Windows — o jeito confiável de acessar é `kubectl
port-forward`:

```bash
kubectl port-forward -n atelie-bebe-dev svc/gateway 5100:8080
```

Gateway fica então em `http://localhost:5100`, igual ao Docker Compose. Sem Ingress Controller por
enquanto — cada outro serviço é só ClusterIP, alcançável de dentro do cluster.

### New Relic (opcional, precisa de `helm` e uma conta New Relic)

```bash
helm repo add newrelic https://newrelic.github.io/k8s-agents-operator
helm install newrelic-agents newrelic/k8s-agents-operator -n atelie-bebe-dev
```

Os Deployments já têm a anotação `newrelic.com/inject-dotnet: "true"` — o operador injeta o agente
.NET automaticamente, sem mexer em Dockerfile. Só falta preencher `NEW_RELIC_LICENSE_KEY` no Secret
`app-secrets` (`k8s/01-secrets.yaml`) com uma chave de uma conta New Relic (tem tier gratuito).

## Fora do escopo (deliberado)

- Migrar produção de verdade — decisão futura separada.
- Banco além de SQLite, múltiplas réplicas por serviço (incompatível com SQLite-por-arquivo).
- Ingress Controller / TLS no cluster local.
- CI/CD para esta estrutura.

## Status

- [x] SharedKernel + Identity + Catalog + Orders + Backoffice + Notifications + Gateway — testados
      ponta a ponta (local e Docker Compose): login, RS256, pedido com preço validado entre
      serviços, dashboard agregado, auditoria via RabbitMQ, rate limiting no Gateway.
- [x] Dockerfiles + `docker-compose.yml` — testado, os 7 containers sobem e funcionam.
- [x] Manifests de Kubernetes (`k8s/`) — aplicados e testados no Kubernetes do Docker Desktop: os 7
      pods (incluindo RabbitMQ) ficam `1/1 Running`, e o mesmo roteiro de curl (login, produto,
      pedido com preço validado entre serviços, dashboard agregado, auditoria via RabbitMQ) funciona
      através do Gateway via `kubectl port-forward`. Ajustes feitos durante o teste: probes de
      readiness/liveness tinham `initialDelaySeconds` curto demais pro cold start do .NET + migração
      EF Core (derrubava os pods antes de subirem); o probe do RabbitMQ trocou de
      `rabbitmq-diagnostics` (CLI Erlang pesada, estourava timeout) para um `tcpSocket` simples.
- [x] Microfrontend Angular (`frontend/shell` + remotes `storefront`/`admin`, Native Federation) —
      testado no navegador rodando os 3 `ng serve` em paralelo (portas 4210/4201/4202) contra o
      Gateway via `kubectl port-forward`: home com dados reais do Catalog, loja paginada, login admin
      com JWT anexado corretamente às chamadas seguintes, dashboard com dados agregados e formatação
      pt-BR (moeda/número) funcionando. Duas armadilhas de Native Federation valeram a pena registrar:
      (1) `app.config.ts` de um remote carregado via `loadChildren` nunca é usado — só o `Routes`; os
      providers (HttpClient/interceptor/LOCALE_ID) têm que estar no `app.config.ts` do shell; (2)
      `registerLocaleData` não pode importar `@angular/common/locales/pt` pelo specifier normal — não
      é coberto por `shareAll()` nem por `skip` no `federation.config.mjs`, e falha em runtime; a
      correção foi copiar o array de dados do locale para um arquivo local do projeto
      (`shell/src/locale-pt.ts`) e importá-lo por caminho relativo, o que contorna o import map do
      Native Federation por completo. Ainda faltam: Dockerfiles pros 3 apps, manifests de Kubernetes,
      e verificar as demais telas (produto, carrinho, checkout, outras telas de admin).
- [ ] New Relic — chart do Helm identificado e testado (`newrelic/k8s-agents-operator`), anotações já
      nos manifests; falta aplicar num cluster ativo e uma license key real.
