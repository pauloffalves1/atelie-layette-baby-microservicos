import { Component, OnDestroy, effect } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { IDLE_TIMEOUT_MS } from '@shared/core/constants/site';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';
import { IdleTimeoutService } from '@shared/core/services/idle-timeout.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-layout.html',
})
export class AdminLayout implements OnDestroy {
  constructor(
    readonly auth: AdminAuthService,
    private readonly router: Router,
    private readonly idleTimeout: IdleTimeoutService,
  ) {
    // adminGuard only lets an authenticated admin render this layout at all, but the effect still
    // guards on isAuthenticated() so the timer stops immediately on logout instead of firing once
    // more after the fact.
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.idleTimeout.start(IDLE_TIMEOUT_MS, () => {
          this.auth.logout();
          this.router.navigate(['/admin/login']);
        });
      } else {
        this.idleTimeout.stop();
      }
    });
  }

  ngOnDestroy(): void {
    this.idleTimeout.stop();
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }
}
