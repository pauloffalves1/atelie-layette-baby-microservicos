import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { environment } from '@shared/environment';
import { CookieConsentService } from './cookie-consent.service';

declare global {
  interface Window {
    dataLayer?: unknown[];
    gtag?: (...args: unknown[]) => void;
    fbq?: ((...args: unknown[]) => void) & { callMethod?: (...args: unknown[]) => void; queue?: unknown[] };
  }
}

/**
 * Loads Google Analytics (GA4) and/or Meta Pixel only when their IDs are set in environment.ts
 * AND the visitor has accepted cookies. Both IDs are blank until the ateliê creates the accounts
 * — with no IDs configured this is a complete no-op (no script tags injected, no third-party
 * requests), same "degrade gracefully" pattern used for WhatsApp/PagBank when their credentials
 * aren't configured yet.
 */
@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly consent = inject(CookieConsentService);

  private started = false;

  /** Safe to call anytime — loads the scripts only if consent is already accepted; otherwise a no-op until initIfAccepted() runs again. */
  initIfAccepted(): void {
    if (this.started || !this.consent.isAccepted) return;

    const { googleAnalyticsId, metaPixelId } = environment.analytics;
    if (!googleAnalyticsId && !metaPixelId) return;

    this.started = true;

    if (googleAnalyticsId) this.loadGoogleAnalytics(googleAnalyticsId);
    if (metaPixelId) this.loadMetaPixel(metaPixelId);

    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe((event) => {
      const url = (event as NavigationEnd).urlAfterRedirects;
      window.gtag?.('event', 'page_view', { page_path: url });
      window.fbq?.('track', 'PageView');
    });
  }

  private loadGoogleAnalytics(id: string): void {
    const script = this.document.createElement('script');
    script.async = true;
    script.src = `https://www.googletagmanager.com/gtag/js?id=${id}`;
    this.document.head.appendChild(script);

    window.dataLayer = window.dataLayer ?? [];
    window.gtag = (...args: unknown[]) => window.dataLayer!.push(args);
    window.gtag('js', new Date());
    window.gtag('config', id);
  }

  private loadMetaPixel(id: string): void {
    if (window.fbq) return;

    const fbq: Window['fbq'] = (...args: unknown[]) => {
      if (fbq!.callMethod) fbq!.callMethod(...args);
      else fbq!.queue!.push(args);
    };
    fbq!.queue = [];
    window.fbq = fbq;

    const script = this.document.createElement('script');
    script.async = true;
    script.src = 'https://connect.facebook.net/en_US/fbevents.js';
    this.document.head.appendChild(script);

    window.fbq('init', id);
    window.fbq('track', 'PageView');
  }
}
