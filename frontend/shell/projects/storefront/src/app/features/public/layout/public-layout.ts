import { Component, HostListener, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '@shared/core/services/auth.service';
import { CartService } from '@shared/core/services/cart.service';
import { NewsletterService } from '@shared/core/services/newsletter.service';
import { WhatsappButton } from '@shared/shared/components/whatsapp-button/whatsapp-button';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, FormsModule, WhatsappButton],
  templateUrl: './public-layout.html',
})
export class PublicLayout {
  readonly currentYear = new Date().getFullYear();

  readonly newsletterEmail = signal('');
  readonly newsletterSubmitting = signal(false);
  readonly newsletterSubscribed = signal(false);

  // Native toggle for the account dropdown — this app never loads Bootstrap's JS bundle (only its
  // SCSS partials, per client architecture), so the markup's old `data-bs-toggle="dropdown"` never
  // did anything; nothing here actually wired it up to open on click.
  readonly accountMenuOpen = signal(false);

  constructor(
    readonly cart: CartService,
    readonly auth: AuthService,
    private readonly newsletterService: NewsletterService,
  ) {}

  toggleAccountMenu(): void {
    this.accountMenuOpen.update((open) => !open);
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
    if (!this.newsletterEmail() || this.newsletterSubmitting()) return;

    this.newsletterSubmitting.set(true);
    this.newsletterService.subscribe(this.newsletterEmail()).subscribe({
      next: () => {
        this.newsletterSubmitting.set(false);
        this.newsletterSubscribed.set(true);
        this.newsletterEmail.set('');
      },
      error: () => this.newsletterSubmitting.set(false),
    });
  }
}
