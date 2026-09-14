import { Component, OnDestroy, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';

/**
 * Catch-all for URLs that match no route. The storefront used to redirect them silently to the
 * home page, so a mistyped or outdated link (an old WhatsApp message, a removed page) just dropped
 * the visitor somewhere unexpected with no explanation.
 */
@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './not-found-page.html',
})
export class NotFoundPage implements OnDestroy {
  private readonly meta = inject(Meta);
  readonly path = inject(Router).url;

  constructor() {
    // Nginx still answers these URLs with 200 + index.html, so tell crawlers not to index them.
    this.meta.updateTag({ name: 'robots', content: 'noindex' });
  }

  ngOnDestroy(): void {
    this.meta.updateTag({ name: 'robots', content: 'index, follow, max-image-preview:large' });
  }
}
