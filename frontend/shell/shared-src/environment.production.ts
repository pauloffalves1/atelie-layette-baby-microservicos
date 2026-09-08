export const environment = {
  production: true,
  // Relative — assumes the Gateway ends up serving/proxying the frontend under the same origin in
  // a real deployment (out of scope for this learning exercise, see plano de arquitetura's "fora do
  // escopo": actual production migration is a separate future decision).
  apiUrl: '/api',
  siteUrl: '',
  analytics: {
    googleAnalyticsId: '',
    metaPixelId: '',
  },
};
