# Design Document — Ateliê Layette Baby (Microsserviços)

## Overview

Este documento descreve a arquitetura técnica que atende aos requisitos de `requirements.md`. Ele
complementa (não repete) as seções "Arquitetura", "Domain Storytelling" e "Event Storming" do
`README.md`, que já cobrem os diagramas Mermaid e a narrativa de negócio — aqui o foco é a
implementação por trás de cada requisito: camadas, contratos entre serviços e decisões de design que
não são óbvias a partir do código.

## Architecture

Seis serviços .NET 10, cada um com sua própria árvore Clean Architecture
(`AtelieBebe.<Serviço>.Core` = Domain+Application+Infrastructure num único projeto, mais um
`AtelieBebe.<Serviço>.Api` de Minimal APIs), mais um Gateway YARP na frente de todos:

| Serviço | Responsabilidade | Dados próprios |
|---|---|---|
| **Identity** | Autenticação (cliente e admin), permissões granulares, 2FA, redefinição de senha | `IdentityDb`: `Admin`, `Customer`, `CustomerAddress`, `EmailVerificationToken`, `PasswordResetToken` |
| **Catalog** | Produtos, avaliações, cupons\*, imagens do site, galeria, favoritos | `CatalogDb`: `Product`, `ProductImage`, `ProductCustomerAccessEntry`, `ProductReview`, `SiteImage`, `GalleryImage`, `WishlistItem` |
| **Orders** | Pedidos, itens, cupons, snapshot de carrinho, pagamento | `OrdersDb`: `Order`, `OrderItem`, `Coupon`, `CartSnapshot` |
| **Backoffice** | Dashboard agregado, mensagens de contato, newsletter, auditoria, sitemap | `BackofficeDb`: `AuditLog`, `ContactMessage`, `NewsletterSubscriber` |
| **Notifications** | Worker que consome eventos e despacha e-mail | sem banco próprio — só consome RabbitMQ |
| **Gateway** | Roteamento YARP + rate limiting nas rotas de auth | sem banco |

\* Cupons vivem fisicamente em `OrdersDb` (aplicados no momento do checkout), não em Catalog.

Comunicação síncrona entre serviços (ex.: Orders revalidando preço contra Catalog no Requisito 3) é
HTTP direto serviço-a-serviço, nunca através do Gateway — o Gateway só atende tráfego externo
(frontend → backend). Comunicação assíncrona é 100% via RabbitMQ, nunca chamada HTTP direta entre um
serviço e o Notifications.

### Frontend

Angular 22 com Native Federation: um host (`shell`) e dois remotes carregados em runtime
(`storefront`, `admin`), publicados como três apps estáticos separados atrás do mesmo Nginx (ver
"Rodando com Docker Compose" no README). `shared-src/` dentro de `frontend/shell` contém código
compartilhado entre os três (models, services, componentes) via um alias TypeScript — não é um
pacote npm publicado, é resolvido por caminho relativo no build de cada projeto.

## Components and Interfaces

### Backend — padrão por serviço

Cada `AtelieBebe.<Serviço>.Core` segue: `Domain/Entities` (agregados, sem dependência de framework),
`Domain/Repositories` (interfaces), `Application/<Feature>/I<Nome>Service` + implementação + DTOs,
`Infrastructure/Persistence` (EF Core, `IEntityTypeConfiguration` por entidade), `Infrastructure/*`
(o que for específico do serviço: geração de JWT só existe em Identity, por exemplo).
`Api/Endpoints/Map<Feature>Endpoints` mapeia rotas Minimal API, sem controllers.

### Autorização entre serviços (Requisito 14)

`AdminPermission` é um `[Flags] enum` definido em `AtelieBebe.SharedKernel` (não duplicado por
serviço) — cada valor é uma área do painel (`Products`, `Orders`, `Coupons`, `Reviews`,
`ContactMessages`, `Newsletter`, `Dashboard`, `SiteContent`, `AdminManagement`). O JWT emitido pelo
Identity carrega uma claim `permission` por flag concedida; cada serviço registra uma policy de
autorização por flag (`JwtTokenGenerator`/`JwtAuthenticationExtensions.PermissionPolicyName()`) e
cada grupo de endpoints troca `RequireAuthorization("AdminOnly")` por
`RequireAuthorization(AdminPermission.X.PermissionPolicyName())`. Um endpoint que exige duas
permissões ao mesmo tempo (excluir pedido, Requisito 4) encadeia duas chamadas
`.RequireAuthorization()` — ambas precisam passar.

**Armadilha de EF Core evitada:** `Admin.Permissions` não tem `HasDefaultValue` no nível do modelo —
isso faria EF Core aplicar `AdminPermission.All` sempre que uma nova conta fosse criada com
`AdminPermission.None` (que é o mesmo valor que o sentinel/default do CLR, `0`), silenciosamente
concedendo permissão total a toda conta nova. O backfill de `AdminPermission.All` para contas
pré-existentes foi feito uma única vez, direto na migration (`defaultValue: 1023L` no `AddColumn`),
não como comportamento contínuo do EF Core.

### Pagamento (Requisito 16)

`GET /api/payments/pagbank/status` (Orders) expõe `{ sandbox: bool }` lido de `PagBank:Sandbox` na
configuração. O frontend (`checkout.ts`/`checkout-modal.ts`) consulta esse endpoint no momento em que
o componente é criado e, em caso de falha de rede, assume `sandbox: true` (fail-safe: bloquear o
pagamento é sempre a opção segura, nunca deixar passar por omissão).

### Recibo em PDF (Requisito 15)

Gerado inteiramente no navegador (`order-confirmation-view.ts`, `buildAndSaveReceipt()`), sem
endpoint de backend dedicado — usa os dados já carregados da tela de confirmação do pedido. A logo é
buscada como `/images/logo-atelie.png` e convertida para data URL antes de ser inserida no PDF via
`jsPDF`; a tabela de itens usa o plugin `jspdf-autotable`.

### Encerramento de sessão por inatividade (Requisito 17)

`IdleTimeoutService` (`shared-src/core/services/idle-timeout.service.ts`) registra listeners de
`mousemove`/`mousedown`/`keydown`/`scroll`/`touchstart`/`click` via `NgZone.runOutsideAngular` (para
não disparar detecção de mudanças do Angular a cada movimento do mouse) e reinicia um `setTimeout` a
cada evento. Ao expirar, chama de volta dentro de `NgZone.run` para que a navegação/logout disparem a
detecção de mudanças normalmente. Cada layout (`public-layout.ts`, `admin-layout.ts`) instancia o
serviço com o timeout correspondente e o próprio logout+redirect daquele contexto (cliente → `/entrar`,
admin → `/admin/login`), observando `auth.isAuthenticated()` via `effect()` para só contar
inatividade enquanto há uma sessão de fato.

**Limitação conhecida, não é bug:** o `setTimeout` do navegador é pausado/throttled em abas sem foco
— uma sessão inativa numa aba em segundo plano só encerra quando a aba volta ao foco (ou quando o
navegador decide rodar o timer atrasado), não exatamente aos 15 minutos de relógio.

### Preview OG/Twitter para bots (Requisito 10)

O roteamento é feito no Nginx (`conf.d/bot-detect.conf` mapeia User-Agent para `$is_bot`), não no
Angular nem no backend — uma requisição de bot para `/produto/:slug` é reescrita para
`/api/seo/product/:slug` (Backoffice), que responde HTML estático com as meta tags já preenchidas.
Navegadores reais recebem a SPA normalmente e preenchem as mesmas tags client-side via `SeoService`.

## Data Models

Ver `README.md` → "Domain Storytelling" e "Event Storming" para os fluxos completos com diagramas.
Os agregados relevantes por serviço estão listados na tabela de "Architecture" acima; relações
que cruzam serviços (ex.: `Order.CustomerId` referenciando um `Customer` que vive em outro banco) são
sempre por ID solto, nunca por foreign key de banco — a integridade é garantida por chamada
síncrona no momento da escrita (Requisito 3, criação de pedido) ou aceita como eventual consistency
quando não é crítica (ex.: nome do cliente exibido no admin, atualizado só na próxima leitura).

## Padrão Outbox (Requisito 3, RNF03)

`DomainEventsToOutboxInterceptor` (em `AtelieBebe.SharedKernel`, reaproveitado pelos 4 serviços com
banco) intercepta o `SaveChanges` do EF Core, lê os eventos de domínio acumulados na entidade e grava
cada um como uma linha de `OutboxMessage` na mesma transação da mudança de estado. `OutboxPublisherService`
(`BackgroundService`) publica as mensagens pendentes no RabbitMQ; `NotificationsEventConsumer` no
serviço Notifications consome e despacha e-mail.

**Bug corrigido em 2026-09-10 (Requisito 4, critério 3):** o serializer padrão do interceptor
serializava enums (como `OrderStatus`) como número; o record de evento no lado do consumidor
(`OrderStatusChangedDomainEvent`) esperava strings, gerando uma `JsonException` engolida
silenciosamente em toda mudança de status — nenhum e-mail saía, sem nenhum erro visível na resposta
HTTP nem no fluxo principal. Corrigido adicionando `JsonStringEnumConverter()` às
`JsonSerializerOptions` do interceptor.

## Testing Strategy (adendo)

Ver README.md → "Estratégia de testes" para a cobertura completa (unitários, TDD, BDD, e2e,
carga). Adendo específico deste `spec/`: os cenários de aceite listados em `requirements.md` mapeiam
1:1 para os arquivos de teste unitário por serviço (`*.Core.Tests`) sempre que a regra é uma invariante
de domínio (ex.: máquina de estados do pedido, Requisito 4) — quando o requisito é puramente de
infraestrutura/integração (ex.: outbox, Requisito 3/RNF03), a cobertura correspondente é o e2e
Playwright ou verificação manual documentada no README, não um teste unitário isolado.

## Security

- JWT RS256 (RNF01): chave privada só em Identity; demais serviços validam com a pública, distribuída
  via `keys/jwt-public.pem` montado em cada container.
- Segredos (RNF06): `.env` na raiz do checkout, nunca commitado; cada serviço lê via
  `IConfiguration`/variáveis de ambiente do Compose.
- Rate limiting (RNF02): aplicado no Gateway (YARP) especificamente nas rotas de autenticação, além
  do rate limiting que cada serviço já aplica na própria borda — duas camadas independentes.
- Ver `requirements.md` → Requisito 14 para o modelo de permissões granulares por administradora.
