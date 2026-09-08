import { Component, signal } from '@angular/core';
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

  constructor(
    readonly cart: CartService,
    readonly auth: AuthService,
    private readonly newsletterService: NewsletterService,
  ) {}

  logout(): void {
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
