import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AdminAuthService } from '../services/admin-auth.service';

export const adminGuard: CanActivateFn = () => {
  const adminAuthService = inject(AdminAuthService);
  const router = inject(Router);

  if (adminAuthService.isAuthenticated()) return true;

  return router.createUrlTree(['/admin/login']);
};
