import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '@shared/environment';
import { AdminAuthService } from '../services/admin-auth.service';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Never attach our own bearer token to third-party requests (e.g. ViaCEP) — only to our API.
  if (!req.url.startsWith(environment.apiUrl)) return next(req);

  const authService = inject(AuthService);
  const adminAuthService = inject(AdminAuthService);

  const isAdminRequest = req.url.includes('/admin/');
  const session = isAdminRequest ? adminAuthService : authService;
  const token = session.getToken();

  if (!token) return next(req);

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })).pipe(
    catchError((error: unknown) => {
      // A stale/expired token otherwise leaves the user looking logged in (name/email cached in
      // localStorage) while every authenticated call quietly 401s underneath — e.g. checkout's
      // profile/address prefill just silently doesn't fill in. Clearing the session here makes
      // the UI immediately reflect reality (logged out) instead of failing invisibly.
      if (error instanceof HttpErrorResponse && error.status === 401) session.logout();
      return throwError(() => error);
    }),
  );
};
