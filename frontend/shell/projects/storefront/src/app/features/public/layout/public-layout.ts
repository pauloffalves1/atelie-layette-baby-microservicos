import { Component, HostListener, effect, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { IDLE_TIMEOUT_MS, SITE_CNPJ } from '@shared/core/constants/site';
import { AnalyticsService } from '@shared/core/services/analytics.service';
import { AuthService } from '@shared/core/services/auth.service';
import { CartService } from '@shared/core/services/cart.service';
import { CheckoutModalService } from '@shared/core/services/checkout-modal.service';
import { IdleTimeoutService } from '@shared/core/services/idle-timeout.service';
import { NewsletterService } from '@shared/core/services/newsletter.service';
import { CheckoutModal } from '@shared/shared/components/checkout-modal/checkout-modal';
import { CookieBanner } from '@shared/shared/components/cookie-banner/cookie-banner';
import { WhatsappButton } from '@shared/shared/components/whatsapp-button/whatsapp-button';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, FormsModule, WhatsappButton, CookieBanner, CheckoutModal],
  templateUrl: './public-layout.html',
})
export class PublicLayout {
  readonly currentYear = new Date().getFullYear();
  readonly siteCnpj = SITE_CNPJ;

  readonly newsletterEmail = signal('');
  readonly newsletterSubmitting = signal(false);
  readonly newsletterSubscribed = signal(false);
  readonly newsletterError = signal<string | null>(null);

  // Native toggles for the account dropdown and the mobile nav — this app never loads Bootstrap's
  // JS bundle (only its SCSS partials, per client architecture), so the markup's old
  // `data-bs-toggle="dropdown"`/`data-bs-toggle="collapse"` never did anything; nothing here
  // actually wired either one up to open on click.
  readonly accountMenuOpen = signal(false);
  readonly mobileMenuOpen = signal(false);

  constructor(
    readonly cart: CartService,
    readonly auth: AuthService,
    readonly checkoutModal: CheckoutModalService,
    private readonly newsletterService: NewsletterService,
    private readonly analytics: AnalyticsService,
    private readonly idleTimeout: IdleTimeoutService,
    router: Router,
  ) {
    router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.mobileMenuOpen.set(false);
    });

    // No-op unless the visitor already accepted cookies on a previous visit — a fresh "accept"
    // click also calls this itself (see CookieBanner), this only covers returning visitors.
    this.analytics.initIfAccepted();

    // Only a logged-in customer has a session worth timing out — browsing anonymously never starts
    // the timer. PublicLayout is mounted once for the whole storefront lifetime, so this effect
    // naturally starts/stops as the customer logs in/out, no manual cleanup needed elsewhere.
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.idleTimeout.start(IDLE_TIMEOUT_MS, () => {
          this.auth.logout();
          router.navigate(['/entrar']);
        });
      } else {
        this.idleTimeout.stop();
      }
    });
  }

  toggleAccountMenu(): void {
    this.accountMenuOpen.update((open) => !open);
  }

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((open) => !open);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (this.accountMenuOpen() && !target.closest('.account-dropdown')) {
      this.accountMenuOpen.set(false);
    }
  }

  logout(): void {
    this.accountMenuOpen.set(false);
    this.auth.logout();
  }

  subscribeNewsletter(): void {
    if (this.newsletterSubmitting()) return;

    const email = this.newsletterEmail().trim();
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      this.newsletterError.set('Informe um e-mail válido.');
      return;
    }

    this.newsletterSubmitting.set(true);
    this.newsletterError.set(null);
    this.newsletterService.subscribe(email).subscribe({
      next: () => {
        this.newsletterSubmitting.set(false);
        this.newsletterSubscribed.set(true);
        this.newsletterEmail.set('');
      },
      error: (err) => {
        this.newsletterSubmitting.set(false);
        this.newsletterError.set(err?.error?.detail ?? 'Não foi possível concluir a inscrição agora. Tente de novo em instantes.');
      },
    });
  }
}
