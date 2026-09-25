import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SeoService } from '@shared/core/services/seo.service';

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
export class NotFoundPage {
  readonly path = inject(Router).url;

  constructor() {
    inject(SeoService).markNotFound();
  }
}
