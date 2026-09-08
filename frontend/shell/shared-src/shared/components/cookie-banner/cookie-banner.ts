import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsService } from '../../../core/services/analytics.service';
import { CookieConsentService } from '../../../core/services/cookie-consent.service';

@Component({
  selector: 'app-cookie-banner',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './cookie-banner.html',
})
export class CookieBanner {
  private readonly analytics = inject(AnalyticsService);
  readonly consent = inject(CookieConsentService);

  accept(): void {
    this.consent.accept();
    this.analytics.initIfAccepted();
  }

  decline(): void {
    this.consent.decline();
  }
}
