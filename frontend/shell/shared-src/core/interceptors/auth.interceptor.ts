import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '@shared/environment';
import { AdminAuthService } from '../services/admin-auth.service';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Never attach our own bearer token to third-party requests (e.g. ViaCEP) — only to our API.
  if (!req.url.startsWith(environment.apiUrl)) return next(req);

  const authService = inject(AuthService);
  const adminAuthService = inject(AdminAuthService);

  const isAdminRequest = req.url.includes('/admin/');
  const token = isAdminRequest ? adminAuthService.getToken() : authService.getToken();

  if (!token) return next(req);

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
