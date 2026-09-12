# Requirements Document — Ateliê Layette Baby (Microsserviços)

## Introdução

Este documento formaliza, no padrão *Spec-Driven Development* (user story + critérios de aceite em
EARS — Easy Approach to Requirements Syntax), os requisitos da arquitetura de microsserviços que
roda em produção (`layettebaby.com.br`) desde 2026-09-08. Ele complementa (não substitui) o
`README.md` deste repositório, que mantém a tabela de requisitos numerados (RF01–RF27, RNF01–RNF06)
usada como referência rápida — cada requisito abaixo cita o(s) RF/RNF correspondente(s) para
rastreabilidade.

Este repositório nasceu de um `git subtree split` do monólito original
(`pauloffalves1/atelie-bebe`), que mantém seu próprio `spec/` cobrindo um catálogo de requisitos
mais amplo (RF01–RF57) construído ao longo de várias sessões antes da migração para microsserviços.
Nem todo requisito do monólito foi portado — este documento cobre apenas o que está de fato
implementado e testado nesta arquitetura; onde os dois se sobrepõem (catálogo, carrinho, checkout,
autenticação), o comportamento é equivalente, mas a numeração RF/RNF é própria desta árvore.

Quatro atores participam do sistema: **Visitante** (não autenticado), **Cliente** (autenticado com
papel `customer`, emitido pelo Identity), **Administradora** (autenticado com papel `admin`, com um
subconjunto de permissões granulares — ver Requisito 14) e **Sistema** (comportamentos automáticos,
sem ator humano direto, incluindo o processamento assíncrono via outbox/RabbitMQ).

---

## Requisito 1: Navegação e busca no catálogo público

**User Story:** Como visitante, quero navegar e buscar produtos no catálogo, para encontrar as peças
que me interessam antes de decidir comprar.

**Rastreamento:** RF01, RF02

**Acceptance Criteria**
1. QUANDO um visitante acessa a loja sem filtro, O SISTEMA (Catalog) DEVE listar produtos ativos e
   públicos, paginados.
2. QUANDO um visitante filtra por categoria ou busca por nome, O SISTEMA DEVE combinar os dois
   filtros e reiniciar a paginação para a página 1.
3. QUANDO uma cliente autenticada tem acesso a um produto marcado como exclusivo
   (`ProductCustomerAccessEntry`), O SISTEMA DEVE incluí-lo na listagem misturado aos públicos.
4. SE um produto for exclusivo E o visitante/cliente não tiver acesso a ele, ENTÃO O SISTEMA DEVE
   tratá-lo como inexistente (404) — nunca revelar que existe.

---

## Requisito 2: Carrinho e personalização de bordado

**User Story:** Como cliente, quero informar o texto e a cor do bordado ao adicionar um produto ao
carrinho, para receber a peça personalizada como pedida — toda peça do ateliê é bordada sob
encomenda.

**Rastreamento:** RF03

**Acceptance Criteria**
1. QUANDO uma cliente tenta adicionar um item ao carrinho sem texto de bordado OU sem cor de linha
   escolhida, O SISTEMA (frontend) DEVE bloquear a adição e sinalizar os campos pendentes.
2. O carrinho é mantido inteiramente em `localStorage` no navegador — nenhuma chamada ao backend
   ocorre até a finalização do checkout.

---

## Requisito 3: Checkout e criação de pedido

**User Story:** Como cliente, quero finalizar a compra com um preço confiável e pagar via PIX, para
concluir o pedido sem depender de combinar valores por fora.

**Rastreamento:** RF04, RF05

**Acceptance Criteria**
1. QUANDO o checkout é confirmado, O SISTEMA (Orders) DEVE revalidar o preço de cada item consultando
   o Catalog no momento da criação do pedido — nunca confiar no preço enviado pelo cliente.
2. QUANDO um pedido é criado, O SISTEMA DEVE gerar uma cobrança PIX e persistir o QR code, de forma
   que ele sobreviva a um reload da página de confirmação.
3. A gravação do pedido e o registro do evento de domínio correspondente na tabela de outbox DEVEM
   ocorrer na mesma transação de banco de dados (ver Requisito 18, RNF03).

---

## Requisito 4: Ciclo de vida do pedido

**User Story:** Como cliente, quero acompanhar e, se necessário, cancelar meu pedido; como
administradora, quero conduzir o pedido pelo fluxo de produção e excluí-lo quando necessário.

**Rastreamento:** RF06, RF07, RF22, RF25

**Acceptance Criteria**
1. QUANDO uma cliente tenta cancelar um pedido, O SISTEMA DEVE permitir somente enquanto o status for
   `Recebido`, recusando com orientação de contato nos demais casos.
2. QUANDO uma administradora muda o status de um pedido, O SISTEMA DEVE validar a transição contra a
   máquina de estados (`Recebido → EmProducao → Pronto → Enviado → Entregue`, com `Cancelado`
   acessível a partir dos três primeiros).
3. QUANDO uma transição de status é aceita — **incluindo para `Cancelado`** —, O SISTEMA DEVE enviar
   um e-mail de notificação à cliente. (Corrigido em 2026-09-10: um bug de serialização do outbox
   fazia esse e-mail falhar silenciosamente em toda transição — ver README, seção Status.)
4. QUANDO uma administradora exclui um pedido, O SISTEMA DEVE exigir tanto a permissão `Orders`
   quanto `AdminManagement` — ação restrita à administradora geral, não a qualquer administradora com
   acesso a encomendas.

---

## Requisito 5: Avaliações de produtos

**User Story:** Como cliente que já comprou um produto, quero avaliá-lo com nota e comentário; como
ateliê, quero moderar essas avaliações antes de publicá-las e destacá-las na home.

**Rastreamento:** RF08, RF09, RF24

**Acceptance Criteria**
1. QUANDO uma cliente envia uma avaliação, O SISTEMA DEVE exigir que ela já tenha comprado o produto
   (checagem cross-service com Orders) e criá-la como pendente, não visível publicamente ainda.
2. QUANDO uma administradora aprova uma avaliação, O SISTEMA DEVE torná-la visível na página do
   produto e elegível para aparecer nos destaques da home.
3. QUANDO a home carrega avaliações aprovadas com comentário, O SISTEMA DEVE exibi-las num carrossel
   com avanço automático, navegável manualmente pela cliente.

---

## Requisito 6: Cupons de desconto

**User Story:** Como ateliê, quero criar cupons de desconto com regras de validade e uso, para
promover campanhas controladas.

**Rastreamento:** RF10

**Acceptance Criteria**
1. QUANDO um cupom é aplicado no checkout, O SISTEMA DEVE recusá-lo SE estiver expirado, desativado,
   com o limite de usos já atingido, ou com um código em formato inválido (apenas letras/números).

---

## Requisito 7: Promoções por produto com período determinado

**User Story:** Como ateliê, quero aplicar descontos por tempo limitado a um produto, para fazer
promoções sazonais sem precisar mexer no preço base nem em jobs agendados.

**Rastreamento:** RF11

**Acceptance Criteria**
1. QUANDO um produto tem uma promoção configurada com janela de tempo ativa, O SISTEMA DEVE calcular
   o preço efetivo automaticamente, por comparação de horário on-demand — sem nenhum job em segundo
   plano para ativar/desativar a promoção.

---

## Requisito 8: Favoritos e aviso de reposição

**User Story:** Como cliente, quero favoritar um produto esgotado/inativo para saber quando ele
voltar; como ateliê, quero avisar automaticamente quem favoritou quando eu reativar o produto.

**Rastreamento:** RF12

**Acceptance Criteria**
1. QUANDO uma administradora reativa um produto que estava inativo, O SISTEMA DEVE emitir um aviso de
   reposição de estoque para toda cliente que tiver esse produto na lista de favoritos
   (`WishlistItem`).

---

## Requisito 9: Lembrete de carrinho abandonado

**User Story:** Como ateliê, quero lembrar a cliente que deixou um carrinho montado sem finalizar,
para recuperar vendas que quase aconteceram.

**Rastreamento:** RF13

**Acceptance Criteria**
1. QUANDO um carrinho fica montado sem finalizar por tempo demais, O SISTEMA DEVE emitir um lembrete
   de carrinho abandonado à cliente.

---

## Requisito 10: Prévia de compartilhamento para bots (SEO)

**User Story:** Como ateliê, quero que um link de produto compartilhado no WhatsApp/Instagram mostre
uma prévia rica (imagem, título, descrição), para gerar mais cliques.

**Rastreamento:** RF14

**Acceptance Criteria**
1. QUANDO uma visitante compartilha o link de um produto num app de mensagens, O SISTEMA DEVE servir
   um preview OG/Twitter Card server-renderizado no lugar da SPA — bots de link-unfurling não
   executam JavaScript, então a versão client-side nunca seria vista por eles.
2. Navegadores reais e crawlers que executam JavaScript (Googlebot) NÃO DEVEM ser afetados por essa
   distinção — apenas o roteamento nginx baseado em `$is_bot` direciona bots para a rota renderizada
   no servidor.

---

## Requisito 11: Autenticação em duas etapas (2FA) para administradoras

**User Story:** Como administradora, quero proteger minha conta com um segundo fator de autenticação,
para reduzir o risco de acesso indevido ao painel.

**Rastreamento:** RF15

**Acceptance Criteria**
1. QUANDO uma administradora ativa 2FA, O SISTEMA DEVE exigir a verificação de um código TOTP contra
   o segredo gerado antes de marcar a conta como protegida.
2. Ativar/desativar 2FA é uma ação de autoatendimento sobre a própria conta (ver Requisito 14,
   critério 4) — não depende de nenhuma permissão administrativa especial.

---

## Requisito 12: Exclusão de conta pela cliente (LGPD)

**User Story:** Como cliente, quero poder solicitar a exclusão da minha conta, para exercer meu
direito de remoção de dados pessoais.

**Rastreamento:** RF16

**Acceptance Criteria**
1. QUANDO uma cliente pede exclusão de conta, O SISTEMA DEVE anonimizar os dados pessoais (nome,
   e-mail, telefone, CPF) e invalidar o login, preservando o histórico de pedidos — que já guarda sua
   própria cópia dos dados no momento da compra, então não depende da conta permanecer íntegra.
2. Uma conta anonimizada NUNCA DEVE conseguir autenticar novamente, mesmo com a senha antiga.

---

## Requisito 13: Integridade do catálogo na exclusão de produtos

**User Story:** Como ateliê, quero impedir a exclusão de um produto que já foi vendido, para nunca
perder a rastreabilidade de um pedido histórico.

**Rastreamento:** RF17

**Acceptance Criteria**
1. QUANDO uma administradora tenta excluir um produto que aparece em algum pedido (qualquer status),
   O SISTEMA DEVE recusar a exclusão, para preservar a integridade do histórico.

---

## Requisito 14: Permissões granulares por administradora

**User Story:** Como ateliê, quero cadastrar mais de uma conta administrativa, cada uma com acesso só
às áreas que ela realmente precisa operar, para reduzir o risco de uma conta comprometida ou de um
erro operacional afetar áreas fora da responsabilidade dessa pessoa.

**Rastreamento:** RF18, RF19, RF20, RF21

**Acceptance Criteria**
1. QUANDO uma administradora com a permissão `AdminManagement` cadastra uma nova conta administrativa,
   O SISTEMA DEVE exigir a escolha explícita de quais áreas (`Products`, `Orders`, `Coupons`,
   `Reviews`, `ContactMessages`, `Newsletter`, `Dashboard`, `SiteContent`, `AdminManagement`) essa
   conta poderá acessar — nenhuma permissão é concedida por padrão (`AdminPermission.None` é o valor
   inicial de uma conta recém-criada sem seleção).
2. QUANDO uma administradora tenta acessar uma área do painel sem a permissão correspondente, O
   SISTEMA DEVE recusar com 403 tanto na API do serviço dono daquela área quanto ocultar o item de
   menu correspondente no painel.
3. QUANDO a última administradora com a permissão `AdminManagement` tenta perder essa permissão —
   seja por edição de outra conta com esse efeito, seja pela exclusão da própria conta —, O SISTEMA
   DEVE recusar, para nunca ficar sem ninguém capaz de gerenciar administradoras.
4. Qualquer administradora DEVE poder alterar a própria senha e ativar/desativar 2FA sem precisar da
   permissão `AdminManagement` — são ações de autoatendimento sobre a própria conta, não sobre outras
   contas.

---

## Requisito 15: Recibo de pedido em PDF

**User Story:** Como cliente, quero baixar um recibo do meu pedido com a identidade visual e os dados
fiscais do ateliê, para ter um comprovante apresentável da compra.

**Rastreamento:** RF23

**Acceptance Criteria**
1. QUANDO uma cliente baixa o recibo em PDF de um pedido, O SISTEMA (frontend, via `jsPDF` +
   `jspdf-autotable`) DEVE incluir a logo do ateliê, o endereço/CNPJ (o mesmo endereço usado no
   cálculo de frete) e uma tabela com os itens do pedido.

---

## Requisito 16: Pagamento online em construção (PagBank)

**User Story:** Como ateliê, quero deixar claro que o pagamento online ainda não está disponível
enquanto eu não tiver as credenciais de produção do PagBank, para não expor um checkout que promete
uma cobrança que não vai acontecer.

**Rastreamento:** RF26

**Acceptance Criteria**
1. ENQUANTO as credenciais de produção do PagBank não estiverem configuradas (`GET
   /api/payments/pagbank/status` retorna `sandbox: true`), O SISTEMA (frontend) DEVE bloquear a
   finalização de pagamento no checkout e orientar a cliente a entrar em contato pelo WhatsApp em vez
   de tentar processar a cobrança.
2. QUANDO as credenciais de produção forem configuradas, o mesmo endpoint DEVE refletir `sandbox:
   false` e o checkout deixa de bloquear automaticamente — não há um segundo lugar no código a
   atualizar.

---

## Requisito 17: Encerramento de sessão por inatividade

**User Story:** Como ateliê, quero que sessões de cliente e de administradora sejam encerradas
automaticamente após um período sem atividade, para reduzir o risco de uso indevido de uma sessão
esquecida aberta num dispositivo compartilhado.

**Rastreamento:** RF27

**Acceptance Criteria**
1. QUANDO uma cliente ou administradora fica 15 minutos sem interagir com a página (sem
   mouse/teclado/toque/scroll/clique), O SISTEMA (frontend, `IdleTimeoutService`) DEVE encerrar a
   sessão e redirecionar para a tela de login correspondente (`/entrar` ou `/admin/login`).
2. Qualquer uma das interações listadas acima DEVE reiniciar o temporizador — a contagem é de
   inatividade contínua, não de tempo total de sessão.

---

## Requisito 18: Requisitos não funcionais transversais

**Rastreamento:** RNF01, RNF02, RNF03, RNF04, RNF05, RNF06

**Acceptance Criteria**
1. O SISTEMA DEVE assinar tokens JWT com RSA (RS256): Identity assina com a chave privada, os demais
   serviços validam apenas com a pública — nenhum serviço além de Identity pode forjar um token.
2. O Gateway DEVE aplicar rate limiting nas rotas de autenticação, independente do rate limiting que
   cada serviço já aplica na própria borda.
3. SE a conexão com o RabbitMQ for perdida, ENTÃO operações síncronas (login, criação de pedido,
   listagem) NÃO DEVEM ser afetadas — apenas a publicação de eventos falha graciosamente (log + retry
   via outbox, descartada após 5 tentativas).
4. Os bancos de dados DEVEM ter backup automatizado diário (`ops/backup-dbs.sh`, `BACKUP DATABASE`
   nativo do SQL Server) com sincronização para armazenamento externo e retenção das 10 cópias mais
   recentes.
5. Toda a interface, mensagens de erro e dados semeados DEVEM estar em português do Brasil (`pt-BR`).
6. Nenhum serviço DEVE ler segredos (senha do banco, credenciais do RabbitMQ, tokens de API) fora de
   variáveis de ambiente (`.env`, nunca commitado).

---

## Requisito 19: Busca semântica no catálogo público

**User Story:** Como visitante, quero descrever o que procuro em texto livre ("body de algodão até
80 reais"), para encontrar produtos sem precisar adivinhar categorias ou nomes exatos.

**Rastreamento:** RF28

**Acceptance Criteria**
1. QUANDO uma visitante ativa a busca inteligente e digita uma consulta em linguagem natural, O
   SISTEMA (Catalog) DEVE traduzi-la via IA (Claude Haiku) em filtros estruturados (categoria, faixa
   de preço, palavras-chave, só promoção) e retornar os produtos compatíveis.
2. SE a chamada à IA falhar (limite de taxa, erro 5xx, indisponibilidade), ENTÃO O SISTEMA DEVE
   degradar para um filtro simples por palavras-chave — a busca nunca deve falhar por completo só
   porque a tradução semântica falhou.

---

## Requisito 20: Pré-triagem de avaliações por IA

**User Story:** Como ateliê, quero que comentários potencialmente impróprios em avaliações sejam
sinalizados automaticamente, para agilizar a moderação humana sem depender só da revisão manual.

**Rastreamento:** RF29

**Acceptance Criteria**
1. QUANDO uma cliente envia uma avaliação, O SISTEMA (Catalog) DEVE pré-triar o comentário via IA e
   gravar um sinalizador de moderação (`ModerationFlag`) na avaliação quando algo for identificado.
2. O sinalizador DEVE ser visível apenas para a administradora (nunca exposto publicamente) e NÃO
   DEVE, por si só, aprovar nem rejeitar a avaliação — a decisão final continua sendo humana.
3. SE a chamada à IA falhar, ENTÃO O SISTEMA DEVE continuar aceitando a avaliação normalmente, sem
   sinalização — a pré-triagem nunca pode bloquear o envio.

---

## Requisito 21: Pré-triagem de texto de bordado por IA

**User Story:** Como ateliê, quero que textos de bordado potencialmente impróprios sejam sinalizados
automaticamente ao criar o pedido, para revisar antes de produzir a peça.

**Rastreamento:** RF30

**Acceptance Criteria**
1. QUANDO uma cliente informa o texto do bordado ao finalizar o checkout, O SISTEMA (Orders) DEVE
   pré-triar esse texto via IA e gravar um sinalizador de moderação no item do pedido quando algo for
   identificado, visível para a administradora no detalhe do pedido.
2. SE a chamada à IA falhar, ENTÃO O SISTEMA DEVE continuar criando o pedido normalmente, sem
   sinalização — a pré-triagem nunca pode bloquear a criação do pedido.

---

## Requisito 22: Ferramentas administrativas assistidas por IA

**User Story:** Como administradora, quero apoio de IA para tarefas repetitivas de redação (responder
contatos, descrever produtos, resumir a semana), para ganhar tempo sem perder a decisão final sobre o
que é publicado ou enviado.

**Rastreamento:** RF31, RF32, RF33

**Acceptance Criteria**
1. QUANDO uma administradora aciona a sugestão de resposta numa mensagem de contato, O SISTEMA
   (Backoffice) DEVE gerar um rascunho via IA a partir do conteúdo da mensagem, sem enviá-lo
   automaticamente — a administradora edita e envia manualmente.
2. QUANDO uma administradora aciona a geração de descrição ao cadastrar/editar um produto, O SISTEMA
   (Catalog) DEVE gerar um texto via IA a partir do nome e da categoria informados; SE a chamada à IA
   falhar, ENTÃO O SISTEMA DEVE reportar o erro à administradora (ação explícita, sem um texto
   padrão seguro para usar como fallback).
3. QUANDO uma administradora aciona o resumo da semana no dashboard, O SISTEMA (Backoffice) DEVE
   gerar um resumo narrativo em português dos indicadores agregados (pedidos, receita, produtos mais
   vendidos) via IA.

---

## Requisito 23: Contenção de custo nas rotas que chamam a API de IA

**User Story:** Como ateliê, quero limitar o uso das rotas que chamam a API da Anthropic, para não
ter uma conta de API inflada por abuso ou tráfego anômalo.

**Rastreamento:** RNF07

**Acceptance Criteria**
1. O Gateway DEVE aplicar uma política de rate limiting dedicada (`ai-cost`) às rotas que acionam
   chamadas pagas à API da Anthropic, independente do rate limiting geral aplicado às demais rotas.

---

## Requisito 24: Carrossel de imagens na home

**User Story:** Como ateliê, quero cadastrar mais de uma foto para a imagem principal da página
inicial, para que ela alterne automaticamente em vez de ficar travada numa única imagem.

**Rastreamento:** RF34

**Acceptance Criteria**
1. QUANDO uma administradora cadastra mais de uma imagem para a chave `home-hero`, O SISTEMA
   (frontend da home) DEVE exibi-las como um carrossel com troca automática, setas de
   navegação e indicadores de posição.
2. QUANDO há zero ou uma imagem cadastrada para `home-hero`, O SISTEMA DEVE se comportar como antes
   (uma foto fixa, sem nenhum controle de carrossel visível) — zero imagens cai no fallback padrão
   embutido no frontend.
3. Uma administradora DEVE poder adicionar, remover e reordenar (mover para cima/para baixo) as
   imagens de `home-hero` pelo painel administrativo, sem precisar mexer no código.
