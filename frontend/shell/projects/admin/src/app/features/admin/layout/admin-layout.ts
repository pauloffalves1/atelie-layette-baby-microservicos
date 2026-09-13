import { Component, DestroyRef, OnDestroy, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { IDLE_TIMEOUT_MS } from '@shared/core/constants/site';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';
import { IdleTimeoutService } from '@shared/core/services/idle-timeout.service';
import { OrderService } from '@shared/core/services/order.service';
import { ReviewService } from '@shared/core/services/review.service';

/** Matches Bootstrap's `lg` breakpoint, where the sidebar stops being an off-canvas drawer. */
const DESKTOP_QUERY = '(min-width: 992px)';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-layout.html',
  host: { '(document:keydown.escape)': 'closeMenu()' },
})
export class AdminLayout implements OnDestroy {
  readonly auth = inject(AdminAuthService);
  private readonly router = inject(Router);
  private readonly idleTimeout = inject(IdleTimeoutService);
  private readonly orderService = inject(OrderService);
  private readonly reviewService = inject(ReviewService);

  private readonly desktopMedia = window.matchMedia(DESKTOP_QUERY);
  readonly isDesktop = signal(this.desktopMedia.matches);
  readonly menuOpen = signal(false);

  /** Work waiting on the admin, shown as badges in the menu: orders nobody has started yet and
   * reviews awaiting moderation. Null = not loaded / no permission for that area. */
  readonly newOrdersCount = signal<number | null>(null);
  readonly pendingReviewsCount = signal<number | null>(null);

  private readonly onMediaChange = (event: MediaQueryListEvent) => {
    this.isDesktop.set(event.matches);
    if (event.matches) this.menuOpen.set(false);
  };

  constructor() {
    this.desktopMedia.addEventListener('change', this.onMediaChange);

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

    // Refreshing on every navigation keeps the badges honest right after the admin acts on
    // something (e.g. moves an order out of "Recebido", approves a review) without polling.
    this.refreshPendingCounts();
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe(() => {
        this.menuOpen.set(false);
        this.refreshPendingCounts();
      });
  }

  ngOnDestroy(): void {
    this.idleTimeout.stop();
    this.desktopMedia.removeEventListener('change', this.onMediaChange);
  }

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  closeMenu(): void {
    this.menuOpen.set(false);
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }

  private refreshPendingCounts(): void {
    if (this.auth.hasPermission('Orders')) {
      this.orderService.listAllForAdmin('Recebido', undefined, 1, 1).subscribe({
        next: (result) => this.newOrdersCount.set(result.totalItems),
        error: () => this.newOrdersCount.set(null),
      });
    }
    if (this.auth.hasPermission('Reviews')) {
      this.reviewService.listForAdmin(false, 1, 1).subscribe({
        next: (result) => this.pendingReviewsCount.set(result.totalItems),
        error: () => this.pendingReviewsCount.set(null),
      });
    }
  }
}
