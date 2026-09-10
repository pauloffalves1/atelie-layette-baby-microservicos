// Load/smoke test for the Gateway — the single HTTP entry point the frontend talks to.
// Run locally against `dotnet run`/`docker compose up` (Gateway on :5100) or the k8s
// `kubectl port-forward` setup documented in microservices/README.md:
//
//   k6 run load-tests/gateway-smoke.js
//   k6 run -e BASE_URL=https://layettebaby.com.br load-tests/gateway-smoke.js   # read-only endpoints only, see below
//
// Exercises three read paths that together cover Gateway routing + two downstream services
// (Catalog, Identity) under concurrent load. Order creation is deliberately excluded from the
// default scenario — hammering CreateStoreOrder would write real rows and email/WhatsApp
// notifications for every iteration, which is a data-safety and third-party-cost concern for a
// load test. Pass -e INCLUDE_ORDER_CREATION=true to opt into it against a disposable environment
// only (never against layettebaby.com.br).
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5100';
const INCLUDE_ORDER_CREATION = (__ENV.INCLUDE_ORDER_CREATION || 'false') === 'true';

const loginFailureRate = new Rate('login_failed');
const productListDuration = new Trend('product_list_duration', true);

export const options = {
  scenarios: {
    browsing: {
      executor: 'ramping-vus',
      exec: 'browseCatalog',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 20 }, // ramp up
        { duration: '1m', target: 20 },  // sustained load
        { duration: '15s', target: 0 },  // ramp down
      ],
    },
    login: {
      executor: 'constant-vus',
      exec: 'login',
      vus: 5,
      duration: '1m45s',
    },
  },
  thresholds: {
    // p95 request duration under 500ms for the catalog listing — it's the highest-traffic
    // endpoint (every storefront page load hits it) and has no auth/DB-write overhead.
    'http_req_duration{endpoint:list_products}': ['p(95)<500'],
    // The Gateway + Identity round trip does a BCrypt hash comparison, which is intentionally
    // slow (cost factor tuned against brute-forcing, not raw throughput) — a looser budget here.
    'http_req_duration{endpoint:login}': ['p(95)<1500'],
    http_req_failed: ['rate<0.01'],
    login_failed: ['rate<0.05'],
  },
};

export function browseCatalog() {
  const listRes = http.get(`${BASE_URL}/api/products?page=1&pageSize=12`, {
    tags: { endpoint: 'list_products' },
  });
  productListDuration.add(listRes.timings.duration);
  check(listRes, {
    'lista de produtos retornou 200': (r) => r.status === 200,
    'lista de produtos não está vazia': (r) => {
      try {
        return JSON.parse(r.body).items?.length > 0;
      } catch {
        return false;
      }
    },
  });

  const products = safeJson(listRes)?.items ?? [];
  if (products.length > 0) {
    const slug = products[Math.floor(Math.random() * products.length)].slug;
    const detailRes = http.get(`${BASE_URL}/api/products/${slug}`, {
      tags: { endpoint: 'product_detail' },
    });
    check(detailRes, { 'detalhe do produto retornou 200': (r) => r.status === 200 });
  }

  sleep(1);
}

export function login() {
  const email = __ENV.LOAD_TEST_CUSTOMER_EMAIL;
  const password = __ENV.LOAD_TEST_CUSTOMER_PASSWORD;
  if (!email || !password) {
    // No disposable test account configured — skip rather than fail the whole run, since a
    // missing credential is a setup issue, not a system-under-test problem.
    sleep(1);
    return;
  }

  const res = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ email, password }),
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'login' } },
  );

  const ok = check(res, { 'login retornou 200': (r) => r.status === 200 });
  loginFailureRate.add(!ok);

  sleep(1);
}

export function createOrder() {
  if (!INCLUDE_ORDER_CREATION) return;

  const payload = JSON.stringify({
    customerName: 'Carga de Teste',
    customerEmail: `carga-${__VU}-${__ITER}@teste.local`,
    customerPhone: '11999990000',
    customerCpf: '11144477735',
    deliveryMethod: 'Retirada',
    shippingCost: 0,
    items: [], // populate with a real seeded productId before enabling against a real environment
  });

  const res = http.post(`${BASE_URL}/api/orders/store`, payload, {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'create_order' },
  });
  check(res, { 'criação de pedido retornou 200': (r) => r.status === 200 });
}

function safeJson(res) {
  try {
    return JSON.parse(res.body);
  } catch {
    return null;
  }
}
