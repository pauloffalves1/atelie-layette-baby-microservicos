import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { AdminAuthService } from '../services/admin-auth.service';
import { adminGuard } from './admin.guard';

describe('adminGuard', () => {
  function runGuard(isAuthenticated: boolean) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AdminAuthService, useValue: { isAuthenticated: () => isAuthenticated } },
      ],
    });

    return TestBed.runInInjectionContext(() => adminGuard({} as never, {} as never));
  }

  it('allows navigation when the admin is authenticated', () => {
    expect(runGuard(true)).toBe(true);
  });

  it('redirects to the admin login page when not authenticated', () => {
    const result = runGuard(false);

    expect(result).not.toBe(true);
    const router = TestBed.inject(Router);
    const expected = router.createUrlTree(['/admin/login']);
    expect((result as UrlTree).toString()).toBe(expected.toString());
  });
});
