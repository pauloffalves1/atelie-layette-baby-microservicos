import { Routes } from '@angular/router';
import { SITE_NAME } from '@shared/core/constants/site';
import { adminGuard } from '@shared/core/guards/admin.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: `Login administrativo — ${SITE_NAME}`,
    loadComponent: () => import('./features/admin/login/admin-login').then((m) => m.AdminLogin),
  },
  {
    path: '',
    loadComponent: () => import('./features/admin/layout/admin-layout').then((m) => m.AdminLayout),
    canActivate: [adminGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: `Dashboard — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/dashboard/admin-dashboard').then((m) => m.AdminDashboard),
      },
      {
        path: 'produtos',
        title: `Produtos — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/products/admin-product-list').then((m) => m.AdminProductList),
      },
      {
        path: 'produtos/novo',
        title: `Novo produto — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/products/admin-product-form').then((m) => m.AdminProductForm),
      },
      {
        path: 'produtos/:id/editar',
        title: `Editar produto — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/products/admin-product-form').then((m) => m.AdminProductForm),
      },
      {
        path: 'encomendas',
        title: `Encomendas — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/orders/admin-order-list').then((m) => m.AdminOrderList),
      },
      {
        path: 'encomendas/:id',
        title: `Detalhe da encomenda — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/orders/admin-order-detail').then((m) => m.AdminOrderDetail),
      },
      {
        path: 'cupons',
        title: `Cupons — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/coupons/admin-coupon-list').then((m) => m.AdminCouponList),
      },
      {
        path: 'mensagens',
        title: `Mensagens de contato — ${SITE_NAME}`,
        loadComponent: () =>
          import('./features/admin/contact-messages/admin-contact-messages').then((m) => m.AdminContactMessages),
      },
      {
        path: 'avaliacoes',
        title: `Avaliações — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/reviews/admin-review-list').then((m) => m.AdminReviewList),
      },
      {
        path: 'clientes',
        title: `Clientes — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/customers/admin-customer-list').then((m) => m.AdminCustomerList),
      },
      {
        path: 'clientes/:id/editar',
        title: `Editar cliente — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/customers/admin-customer-form').then((m) => m.AdminCustomerForm),
      },
      {
        path: 'imagens',
        title: `Imagens do site — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/site-images/admin-site-images').then((m) => m.AdminSiteImages),
      },
      {
        path: 'galeria',
        title: `Galeria — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/gallery/admin-gallery').then((m) => m.AdminGallery),
      },
      {
        path: 'newsletter',
        title: `Newsletter — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/newsletter/admin-newsletter').then((m) => m.AdminNewsletter),
      },
      {
        path: 'auditoria',
        title: `Auditoria — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/audit-log/admin-audit-log').then((m) => m.AdminAuditLog),
      },
      {
        path: 'seguranca',
        title: `Segurança — ${SITE_NAME}`,
        loadComponent: () => import('./features/admin/security/admin-security').then((m) => m.AdminSecurity),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
