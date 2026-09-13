# Implementation Plan — Ateliê Layette Baby (Microsserviços)

Checklist rastreável por requisito, refletindo o que já está implementado, testado e (onde aplicável)
em produção. Ver `README.md` → "Status" para o histórico narrativo completo de cada item, incluindo
armadilhas encontradas e como foram corrigidas — este arquivo é o índice rastreável, não a
substituição daquele relato.

## Infraestrutura base

- [x] SharedKernel + Identity + Catalog + Orders + Backoffice + Notifications + Gateway — testados
      ponta a ponta (local e Docker Compose).
- [x] Dockerfiles + `docker-compose.yml` — 7 containers sobem e funcionam (`-p microservices`).
- [x] Manifests de Kubernetes (`k8s/`) — aplicados e testados no Docker Desktop.
- [x] Microfrontend Angular (`frontend/shell` + remotes `storefront`/`admin`, Native Federation).
- [x] Migração SQLite → SQL Server (Identity, Catalog, Orders, Backoffice).
- [x] `.slnx` único + CI (`unit-tests` + `e2e` jobs no GitHub Actions).
- [x] Estratégia de testes: 5 projetos xUnit, TDD, BDD (Reqnroll), e2e (Playwright), carga (k6).
- [x] Extraído para repositório próprio (`pauloffalves1/atelie-layette-baby-microservicos`) via
      `git subtree split`.
- [x] Migração de produção real (`layettebaby.com.br` na VPS Hostinger, Docker Compose).
- [x] Vulnerabilidades de dependências corrigidas (`Microsoft.OpenApi`, `System.Security.Cryptography.Xml`,
      `SQLitePCLRaw.lib.e_sqlite3`).
- [x] Dev local com `dotnet run` corrigido (conn strings SQL Server) + dados de produção restaurados
      localmente para desenvolvimento.
- [ ] New Relic — chart do Helm identificado e testado, falta cluster ativo + license key real (não
      provisionável no momento).

## Requisito 1 — Navegação e busca no catálogo público (RF01, RF02)

- [x] Listagem paginada com filtro por categoria e busca por nome.
- [x] Produtos exclusivos (`ProductCustomerAccessEntry`) tratados como 404 para quem não tem acesso.

## Requisito 2 — Carrinho e personalização de bordado (RF03)

- [x] Bloqueio client-side de adicionar ao carrinho sem texto/cor de bordado.

## Requisito 3 — Checkout e criação de pedido (RF04, RF05)

- [x] Revalidação de preço contra Catalog no momento da criação do pedido.
- [x] Cobrança PIX + QR code persistido, sobrevivendo a reload.
- [x] Outbox: gravação do evento na mesma transação da criação do pedido.

## Requisito 4 — Ciclo de vida do pedido (RF06, RF07, RF22, RF25)

- [x] Cancelamento pela cliente restrito ao status `Recebido`.
- [x] Máquina de estados validada nas transições de status pela administradora.
- [x] **E-mail em toda mudança de status, incluindo `Cancelado`** (2026-09-10) — bug de serialização
      de enum no outbox corrigido (`JsonStringEnumConverter`). Verificado via logs de produção
      antes/depois.
- [x] **Exclusão de encomenda restrita à administradora geral** (2026-09-10) — endpoint exige
      `Orders` + `AdminManagement`; botão só aparece no painel para quem tem `AdminManagement`.

## Requisito 5 — Avaliações de produtos (RF08, RF09, RF24)

- [x] Avaliação exige compra prévia, criada como pendente.
- [x] Aprovação pela administradora publica a avaliação e a torna elegível para destaque.
- [x] **Carrossel de avaliações na home** (2026-09-10) — testado no navegador; precisou do partial
      `bootstrap/scss/carousel` tanto no `storefront` quanto no `shell` (o app composto usa a folha
      de estilos do shell, não a do remote isoladamente).

## Requisito 6 — Cupons de desconto (RF10)

- [x] Cupom recusado se expirado, desativado, limite atingido, ou formato inválido.

## Requisito 7 — Promoções por período (RF11)

- [x] Preço promocional calculado on-demand por janela de tempo, sem job agendado.

## Requisito 8 — Favoritos e aviso de reposição (RF12)

- [x] Reativação de produto emite aviso de reposição para quem favoritou.

## Requisito 9 — Lembrete de carrinho abandonado (RF13)

- [x] Lembrete emitido quando um carrinho fica montado sem finalizar por tempo demais.

## Requisito 10 — Prévia de compartilhamento para bots / SEO (RF14)

- [x] Preview OG/Twitter server-renderizado via roteamento nginx `$is_bot` → `/api/seo/*`.

## Requisito 11 — 2FA para administradoras (RF15)

- [x] Ativação de 2FA exige verificação de código TOTP antes de marcar a conta como protegida.

## Requisito 12 — Exclusão de conta pela cliente / LGPD (RF16)

- [x] Anonimização de dados pessoais preservando o histórico de pedidos.

## Requisito 13 — Integridade do catálogo (RF17)

- [x] Exclusão de produto recusada quando ele aparece em algum pedido.

## Requisito 14 — Permissões granulares por administradora (RF18, RF19, RF20, RF21)

- [x] Cadastro de nova administradora exige escolha explícita de permissões (`AdminPermission.None`
      por padrão).
- [x] 403 + menu oculto para área sem permissão correspondente.
- [x] Guarda contra a última conta com `AdminManagement` perder essa permissão.
- [x] Troca de senha e 2FA como autoatendimento, sem exigir `AdminManagement`.
- [x] Validado de ponta a ponta contra a stack real: criação de admin limitado, 403/200 corretos,
      troca de senha, as duas travas de segurança.

## Requisito 15 — Recibo de pedido em PDF (RF23)

- [x] **Redesenhado** (2026-09-10) — logo, endereço/CNPJ e tabela de itens via `jsPDF` +
      `jspdf-autotable`, gerado no navegador.

## Requisito 16 — Pagamento em construção / PagBank (RF26)

- [x] **Checkout bloqueado até credenciais de produção** (2026-09-10) — `GET
      /api/payments/pagbank/status` consultado pelo frontend; enquanto `sandbox: true`, bloqueia e
      orienta contato via WhatsApp.

## Requisito 17 — Encerramento de sessão por inatividade (RF27)

- [x] **Logout automático (15 min)** (2026-09-10) — `IdleTimeoutService` testado de ponta a ponta com
      timeout temporariamente reduzido para validação (aba mantida em foco); revertido para 15 min
      reais depois, confirmado via `git diff` vazio.

## Requisito 18 — Requisitos não funcionais transversais (RNF01–RNF06)

- [x] JWT RS256 (chave privada só em Identity).
- [x] Rate limiting no Gateway nas rotas de autenticação.
- [x] Outbox com retry (máx. 5 tentativas) isolando falha de publicação de operação síncrona.
- [x] Backup automatizado diário (`ops/backup-dbs.sh`) com retenção de 10 cópias + sync externo.
- [x] `ops/test-restore.sh` executado de verdade contra a VPS (2026-09-12) — não só escrito e lido,
      rodado de ponta a ponta duas vezes: a primeira encontrou três bugs reais (permissão do `.bak`
      copiado para dentro do container, `sqlcmd` sem `-b` mascarando a falha de restore, e a checagem
      de linhas tratando uma mensagem de erro como sucesso), todos corrigidos; a segunda rodada
      restaurou os 4 bancos de verdade, com contagens de linha plausíveis em cada um. Ver README →
      Status para o relato completo.
- [x] Interface e dados semeados 100% em pt-BR.
- [x] **Datas em UTC de ponta a ponta** (2026-09-13) — `UseUtcDateTimes()` nos 4 `DbContext`s faz as APIs
      emitirem `...Z` (antes as datas apareciam 3h adiantadas no navegador e o formulário de promoção
      somava 3h a cada salvamento); CSVs convertem para Brasília. Ver README → Status.
- [x] Segredos só via `.env`/variáveis de ambiente, nunca commitados.

## Requisito 19 — Busca semântica no catálogo público (RF28)

- [x] `ISemanticSearchTranslator`/`AnthropicSemanticSearchTranslator` (Claude Haiku) traduz consulta
      em linguagem natural em `ProductSearchFilters` (categoria, faixa de preço, palavras-chave, só
      promoção) via saída JSON estruturada (schema com `anyOf` para categoria nula/enum).
- [x] `GET /api/products/search/semantic` (Catalog) + checkbox "Busca inteligente" na loja.
- [x] Fallback para filtro simples por palavras-chave se a chamada à IA falhar (rate limit, 5xx,
      exceção de I/O) — testado de ponta a ponta contra a API real da Anthropic.
- [x] **Testes unitários** (2026-09-12) — `ProductServiceSemanticSearchTests` cobre a orquestração
      em `ProductService.SearchAsync` (categorias conhecidas repassadas ao tradutor, filtros
      aplicados ao repositório, página vazia sem exceção) com `ISemanticSearchTranslator` mockado via
      NSubstitute; o comportamento de fallback em si (dentro do tradutor concreto) permanece coberto
      só pela validação manual contra a API real.

## Requisito 20 — Pré-triagem de avaliações por IA (RF29)

- [x] `IReviewModerationScreener`/`AnthropicReviewModerationScreener` grava `ModerationFlag` em
      `ProductReview` quando a IA identifica conteúdo impróprio; nunca bloqueia o envio da avaliação
      se a chamada falhar.
- [x] `ModerationFlag` exposto só no DTO administrativo (`AdminProductReviewDto`), com badge na
      listagem de avaliações do painel.
- [x] **Testes unitários** (2026-09-12) — `ReviewServiceModerationTests` cobre `ReviewService.CreateAsync`
      com `IReviewModerationScreener` mockado: sinalização gravada na avaliação quando a triagem
      encontra algo, `ModerationFlag` nulo quando não encontra ou quando o comentário é vazio (nesse
      caso o screener nem é chamado), e uma checagem por reflexão de que `ProductReviewDto` (o DTO
      público) nunca ganha uma propriedade `ModerationFlag`.

## Requisito 21 — Pré-triagem de texto de bordado por IA (RF30)

- [x] `IEmbroideryModerationScreener`/`AnthropicEmbroideryModerationScreener` grava `ModerationFlag`
      em `OrderItem` a partir do texto de bordado informado no checkout; nunca bloqueia a criação do
      pedido se a chamada falhar.
- [x] Badge de sinalização no detalhe do pedido no painel administrativo.
- [x] **Testes unitários** (2026-09-12) — `OrderServiceEmbroideryModerationTests` cobre
      `OrderService.CreateStoreOrderAsync` com `IEmbroideryModerationScreener` mockado: sinalização
      gravada no item quando a triagem encontra algo, nula quando não encontra, e o screener nunca é
      chamado quando não há `EmbroideryText` no `OptionsJson`, quando o JSON é malformado, ou quando
      `OptionsJson` é nulo — o pedido é criado normalmente em todos os casos.

## Requisito 22 — Ferramentas administrativas assistidas por IA (RF31, RF32, RF33)

- [x] `IContactReplyDrafter`/`AnthropicContactReplyDrafter` — botão "Sugerir resposta" nas mensagens
      de contato do painel, gera rascunho editável, nunca envia sozinho.
- [x] `IProductDescriptionGenerator`/`AnthropicProductDescriptionGenerator` — botão "Gerar com IA" no
      formulário de produto; erro da IA é reportado à administradora (ação explícita, sem fallback
      de texto padrão).
- [x] `IDashboardSummaryGenerator`/`AnthropicDashboardSummaryGenerator` — botão "Gerar resumo da
      semana" no dashboard, resume os indicadores agregados em português.
- [x] Testado de ponta a ponta contra a API real da Anthropic (modelo `claude-haiku-4-5`) nos três
      fluxos.

## Requisito 23 — Contenção de custo nas rotas que chamam a API de IA (RNF07)

- [x] Política `ai-cost` no Gateway (`RateLimitPartition.GetFixedWindowLimiter`, 20 requisições/min
      por IP+rota) aplicada só à rota `catalog-semantic-search-route`.
- [x] **Cobertas as demais rotas que chamam a IA** (2026-09-12) — três novas rotas específicas no
      Gateway, cada uma antes do catch-all genérico do mesmo recurso para não afetar as demais
      operações admin: `catalog-admin-generate-description-route`
      (`/api/admin/products/generate-description`), `backoffice-admin-suggest-reply-route`
      (`/api/admin/contact-messages/suggest-reply`) e `backoffice-dashboard-summary-route`
      (`/api/admin/dashboard/summary`), todas com `RateLimiterPolicy: ai-cost`. Confirmado ao vivo
      contra o Gateway local: 20 requisições passam, a partir da 21ª vem `429`; as rotas irmãs sem IA
      (`/api/admin/products`, `/api/admin/contact-messages`, `/api/admin/dashboard`) continuam sem
      limite específico, só a política geral do Gateway.
- [x] **Bug encontrado durante essa checagem, corrigido** (2026-09-12) — a rota
      `backoffice-dashboard-route` só casava com o caminho exato `/api/admin/dashboard` (sem
      `{**catch-all}`, diferente de todas as outras rotas admin do Gateway), então
      `POST /api/admin/dashboard/summary` nunca chegava ao Backoffice: o Gateway respondia 404 antes
      mesmo de aplicar autenticação ou rate limiting. Corrigido trocando o `Match.Path` para
      `/api/admin/dashboard/{**catch-all}` (confirmado que isso ainda casa com o caminho vazio,
      mesmo padrão usado em `catalog-admin-products-route` etc.). Confirmado ao vivo: 404 → 401 depois
      do rebuild do container do Gateway.

## Requisito 24 — Carrossel de imagens na home (RF34)

- [x] `SiteImage` ganhou `SortOrder` e o índice em `Key` deixou de ser único (migration
      `AddSiteImageSortOrder`) — uma chave como `home-hero` agora pode ter 0..N linhas ordenadas.
- [x] Endpoints admin novos: `POST .../{key}/items` (adiciona sem substituir), `DELETE
      .../items/{id}`, `POST .../items/{id}/move` (troca `SortOrder` com o vizinho na direção
      pedida). O upsert antigo (`POST .../{key}`) continua intocado para chaves de imagem única
      (`about`).
- [x] Painel "Imagens do site": chaves marcadas como `multi` (só `home-hero` por enquanto) ganham
      uma lista com miniatura + mover para cima/baixo + excluir + "adicionar foto ao carrossel";
      chaves de imagem única mantêm o botão simples de "trocar imagem".
- [x] Home renderiza um carrossel próprio (crossfade via CSS, sem JS do Bootstrap — mesmo padrão do
      carrossel de avaliações) quando há mais de uma imagem; cai para foto única sem controles com
      zero ou uma imagem.
- [x] Testado ao vivo: upload de 2 fotos, reordenar (miniaturas trocam de posição), navegar entre
      as duas no carrossel da home, excluir uma, excluir a última (volta ao fallback padrão).

## Requisito 25 — Busca nas listas do painel administrativo (RF35)

- [x] Orders: `GET /api/admin/orders` e `/export` aceitam `search` — `OrderRepository.FilteredQuery`
      aplica `LIKE` em nome, e-mail (`(string)(object)` para atravessar o value converter de `Email`),
      telefone e prefixo de `CONVERT(varchar(36), Id)`; `#` inicial ignorado. Teste de serviço
      `OrderServiceAdminSearchTests` fixa o repasse do termo.
- [x] Catalog: `GET /api/admin/products` passou a repassar `search` e `category` ao `ListAsync` que já
      suportava os dois (a chave de cache já incluía ambos).
- [x] Clientes: filtro em memória no frontend (a API de clientes do admin já devolve a lista inteira,
      sem paginação), ignorando acentos e comparando telefone/CPF só pelos dígitos.
- [x] Frontend: listas de encomendas e produtos guardam filtros/busca/página na URL
      (`?status=&pagamento=&busca=&pagina=`, `?busca=&categoria=&pagina=`), com debounce na digitação.
- [x] Validado contra o SQL Server real (stack local): busca por `#84ed` e `5D6165` (prefixo, sem
      diferenciar caixa), por nome e por e-mail, termo inexistente (0), busca + filtro de status,
      exportação CSV filtrada e busca/categoria de produtos — SQL gerado conferido no log do EF.

## Requisito 26 — Dashboard administrativo confiável e orientado a ação (RF36)

- [x] Agregação movida do endpoint interno de Orders para `OrdersDashboardStatsCalculator` (Core,
      puro sobre `DashboardOrderSnapshot`) — testável com horários arbitrários; 8 testes cobrem
      fronteira de dia/mês em Brasília, série de 30 dias sem lacunas, ordem do fluxo, pago/pendente,
      comparação com o mesmo período do mês anterior (inclusive mês anterior mais curto) e bordados
      sinalizados.
- [x] Fuso: `America/Sao_Paulo` com fallback para UTC-3 fixo (sem horário de verão desde 2019) quando
      a imagem não tem tz database.
- [x] Contrato `OrdersDashboardStatsDto`/`DashboardDto` estendido com campos novos no fim e valores
      padrão, para Orders e Backoffice seguirem compatíveis durante o deploy.
- [x] Admin: bloco "Precisa de atenção" com links para listas filtradas, variação mensal, gráfico de
      30 dias com tooltip por toque/teclado (tabindex móvel + setas), escala e visão em tabela, status
      clicáveis, estado de erro/atualização sem piscar. Cor da série validada pelo validador de paleta
      (`#c9727f`; o rosa da marca reprovou em luminosidade/croma/contraste).

## Fora do escopo deste `spec/` (documentado apenas no `README.md`)

- Renomeação "Galeria" → "Dicas para o casal" (2026-09-10) — mudança de rótulo/URL sem alterar
  comportamento; não gera um requisito novo, ver README → Status.
- Ajustes visuais pontuais (rodapé, FAQ, tooltips) sem regra de negócio associada.
- Segunda leva de polimento visual (2026-09-12) — badges, cards com hover, CTAs de fechamento e
  chevrons do FAQ em Home/Sobre/Loja/Contato/Produção e Envio/FAQ; ver README → Status.
- Rodada de UX do funil de compra (2026-09-13) — prévia do bordado, desfazer remoção no carrinho,
  validação/rolagem e autocomplete no checkout, detalhes de bordado/endereço/rastreio no pós-compra;
  sem regra de negócio nova, ver README → Status.
- Rodada de UX do painel admin (2026-09-13) — menu recolhível no celular com contadores de
  pendências, confirmação antes de mudar status de encomenda, WhatsApp/e-mail clicáveis, recado de
  presente na etiqueta, galeria/acesso exclusivo salvos junto com o produto e aviso de alterações não
  salvas; sem regra de negócio nova além do Requisito 25, ver README → Status.
- Rodada de UX de loja, autenticação, 2FA e cupons (2026-09-13) — corrida da busca inteligente,
  mostrar/ocultar senha, mensagens de erro por tipo de falha, QR code do 2FA, situação dos cupons, e
  o interceptor parando de deslogar em 401 de regra de negócio; sem regra de negócio nova, ver
  README → Status.
- Falhas de carregamento honestas, página 404 e contato (2026-09-13) — `LoadError` com "Tentar de
  novo" em 16 telas (inclusive impedindo salvar formulário de edição carregado vazio), página "não
  encontrada" na loja e no admin, confirmação com link de reserva no contato; sem regra de negócio
  nova, ver README → Status.
- Layout responsivo, fotos ampliáveis e admin restante (2026-09-13) — menu/carrinho entre 992 e
  1400px, rodapé e botão do WhatsApp, alinhamento do hero, `ImageLightbox` no produto e na galeria,
  permissões de novo administrador, confirmações de remoção e envio múltiplo na galeria; sem regra
  de negócio nova, ver README → Status.
