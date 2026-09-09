import { Routes } from '@angular/router';
import { SITE_NAME } from '@shared/core/constants/site';
import { customerGuard } from '@shared/core/guards/customer.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/public/layout/public-layout').then((m) => m.PublicLayout),
    children: [
      {
        path: '',
        title: `${SITE_NAME} — Fraldas de ombro e boca bordadas`,
        loadComponent: () => import('./features/public/home/home').then((m) => m.Home),
      },
      {
        path: 'loja',
        title: `Loja — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/shop/shop').then((m) => m.Shop),
      },
      {
        path: 'produto/:slug',
        title: `Produto — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/product-detail/product-detail').then((m) => m.ProductDetail),
      },
      {
        path: 'carrinho',
        title: `Carrinho — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/cart/cart-page').then((m) => m.CartPage),
      },
      {
        path: 'checkout',
        title: `Finalizar compra — ${SITE_NAME}`,
        canActivate: [customerGuard],
        loadComponent: () => import('./features/public/checkout/checkout').then((m) => m.Checkout),
      },
      { path: 'encomenda-personalizada', redirectTo: 'contato', pathMatch: 'full' },
      {
        path: 'sobre',
        title: `Sobre o ateliê — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/about/about').then((m) => m.About),
      },
      {
        path: 'galeria',
        title: `Galeria — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/gallery/gallery').then((m) => m.Gallery),
      },
      {
        path: 'contato',
        title: `Contato e encomendas — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/contact/contact').then((m) => m.Contact),
      },
      {
        path: 'termos-de-uso',
        title: `Termos de Uso — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/legal/terms-page').then((m) => m.TermsPage),
      },
      {
        path: 'politica-de-privacidade',
        title: `Política de Privacidade — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/legal/privacy-page').then((m) => m.PrivacyPage),
      },
      {
        path: 'politica-de-envio',
        title: `Política de Produção e Envio — ${SITE_NAME}`,
        loadComponent: () =>
          import('./features/public/legal/shipping-policy-page').then((m) => m.ShippingPolicyPage),
      },
      {
        path: 'perguntas-frequentes',
        title: `Perguntas Frequentes — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/faq/faq-page').then((m) => m.FaqPage),
      },
      {
        path: 'entrar',
        title: `Entrar — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/auth/login-page').then((m) => m.LoginPage),
      },
      {
        path: 'cadastro',
        title: `Criar conta — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/auth/register-page').then((m) => m.RegisterPage),
      },
      {
        path: 'esqueci-senha',
        title: `Esqueci minha senha — ${SITE_NAME}`,
        loadComponent: () =>
          import('./features/public/auth/forgot-password-page').then((m) => m.ForgotPasswordPage),
      },
      {
        path: 'redefinir-senha',
        title: `Redefinir senha — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/auth/reset-password-page').then((m) => m.ResetPasswordPage),
      },
      {
        path: 'verificar-email',
        title: `Verificação de e-mail — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/auth/verify-email-page').then((m) => m.VerifyEmailPage),
      },
      {
        path: 'minha-conta',
        title: `Minha conta — ${SITE_NAME}`,
        canActivate: [customerGuard],
        loadComponent: () => import('./features/public/my-account/my-account').then((m) => m.MyAccount),
      },
      {
        path: 'favoritos',
        title: `Meus favoritos — ${SITE_NAME}`,
        canActivate: [customerGuard],
        loadComponent: () => import('./features/public/wishlist/wishlist-page').then((m) => m.WishlistPage),
      },
      {
        path: 'rastrear-pedido',
        title: `Rastrear pedido — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/track-order/track-order').then((m) => m.TrackOrder),
      },
      {
        path: 'pedido/:id',
        title: `Confirmação de pedido — ${SITE_NAME}`,
        loadComponent: () =>
          import('./features/public/order-confirmation/order-confirmation').then((m) => m.OrderConfirmation),
      },
      {
        path: 'pagamento-simulado/:orderId',
        title: `Pagamento (simulação) — ${SITE_NAME}`,
        loadComponent: () => import('./features/public/fake-payment/fake-payment').then((m) => m.FakePayment),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
