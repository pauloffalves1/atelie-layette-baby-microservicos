import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { environment } from '@shared/environment';
import { SITE_NAME } from '../constants/site';

export interface ProductStructuredData {
  name: string;
  description: string | null;
  image: string;
  url: string;
  price: number;
  inStock: boolean;
  ratingValue?: number;
  reviewCount?: number;
}

export interface SeoData {
  /** Page title, without the site name suffix — added automatically. */
  title: string;
  description: string;
  /** Route path (e.g. '/produto/fralda-boca-florzinha'), used to build the canonical/og:url. */
  path: string;
  /** Absolute image URL for social share previews. Falls back to the default site preview image. */
  image?: string;
  /** og:type — 'product' for product pages, 'website' for everything else. */
  type?: string;
}

/**
 * Sets per-page <title>, meta description, Open Graph and Twitter Card tags, plus the canonical
 * link — Angular's router title strategy only covers <title>, so everything else needs this.
 * Call from each public page's ngOnInit; admin/account pages are excluded from indexing via
 * robots.txt instead, so they don't need to call this.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly titleService = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  update(data: SeoData): void {
    const fullTitle = `${data.title} — ${SITE_NAME}`;
    const url = `${environment.siteUrl}${data.path}`;
    // og:image is fetched directly by crawlers (WhatsApp, Facebook), never through the browser —
    // it must always be absolute, unlike <img src> which can stay relative via resolveAssetUrl().
    const image = data.image ? this.toAbsolute(data.image) : `${environment.siteUrl}/images/hero-fraldas.jpg`;

    this.titleService.setTitle(fullTitle);

    this.setTag({ name: 'description' }, data.description);
    this.setTag({ property: 'og:title' }, fullTitle);
    this.setTag({ property: 'og:description' }, data.description);
    this.setTag({ property: 'og:type' }, data.type ?? 'website');
    this.setTag({ property: 'og:url' }, url);
    this.setTag({ property: 'og:image' }, image);
    this.setTag({ property: 'og:site_name' }, SITE_NAME);
    this.setTag({ name: 'twitter:card' }, 'summary_large_image');
    this.setTag({ name: 'twitter:title' }, fullTitle);
    this.setTag({ name: 'twitter:description' }, data.description);
    this.setTag({ name: 'twitter:image' }, image);

    this.setCanonical(url);

    // Cleared on every navigation so a product's rich-snippet data never lingers on the next,
    // unrelated page visited in this same SPA session — product-detail re-adds it right after.
    this.clearStructuredData();
  }

  /** Schema.org Product markup — lets Google show price/availability/rating directly in search results. */
  setProductStructuredData(product: ProductStructuredData): void {
    const data: Record<string, unknown> = {
      '@context': 'https://schema.org/',
      '@type': 'Product',
      name: product.name,
      image: [this.toAbsolute(product.image)],
      description: product.description ?? product.name,
      offers: {
        '@type': 'Offer',
        url: `${environment.siteUrl}${product.url}`,
        priceCurrency: 'BRL',
        price: product.price.toFixed(2),
        availability: product.inStock ? 'https://schema.org/InStock' : 'https://schema.org/OutOfStock',
      },
    };

    if (product.ratingValue && product.reviewCount) {
      data['aggregateRating'] = {
        '@type': 'AggregateRating',
        ratingValue: product.ratingValue.toFixed(1),
        reviewCount: product.reviewCount,
      };
    }

    let script = this.document.querySelector<HTMLScriptElement>('script[type="application/ld+json"]');
    if (!script) {
      script = this.document.createElement('script');
      script.setAttribute('type', 'application/ld+json');
      this.document.head.appendChild(script);
    }
    script.textContent = JSON.stringify(data);
  }

  private clearStructuredData(): void {
    this.document.querySelector('script[type="application/ld+json"]')?.remove();
  }

  private toAbsolute(url: string): string {
    return /^https?:\/\//i.test(url) ? url : `${environment.siteUrl}${url}`;
  }

  private setTag(attrs: { name?: string; property?: string }, content: string): void {
    const selector = attrs.name ? `name="${attrs.name}"` : `property="${attrs.property}"`;
    this.meta.updateTag({ ...attrs, content }, selector);
  }

  private setCanonical(url: string): void {
    let link = this.document.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.document.head.appendChild(link);
    }
    link.setAttribute('href', url);
  }
}
