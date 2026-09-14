import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { environment } from '@shared/environment';
import { SITE_NAME } from '../constants/site';

export interface ProductStructuredData {
  name: string;
  description: string | null;
  /** Every photo of the product, main one first. */
  images: string[];
  url: string;
  sku: string;
  category: string;
  price: number;
  inStock: boolean;
  /** Promotion end — Google shows a struck-through price only with a validity date. */
  priceValidUntil?: string | null;
  ratingValue?: number;
  reviewCount?: number;
  reviews?: { author: string; rating: number; comment: string | null; date: string }[];
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

/** Search results cut descriptions around here; a longer one gets truncated mid-word by Google. */
const MAX_DESCRIPTION_LENGTH = 160;

/** JSON-LD blocks a page can add; all of them are dropped on the next page's update(). */
type StructuredDataKey = 'product' | 'breadcrumb' | 'faq';

/**
 * Sets per-page <title>, meta description, Open Graph and Twitter Card tags, the canonical link and
 * page-level Schema.org JSON-LD — Angular's router title strategy only covers <title>. Call from
 * each public page's ngOnInit; admin/account pages are excluded from indexing via robots.txt.
 * Search crawlers get these pages as prerendered snapshots (scripts/prerender.mjs, ops/seo), so whatever
 * this writes into <head> is exactly what Google indexes.
 */
@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly titleService = inject(Title);
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  update(data: SeoData): void {
    const fullTitle = `${data.title} — ${SITE_NAME}`;
    const url = `${environment.siteUrl}${data.path}`;
    const description = SeoService.trimDescription(data.description);
    // og:image is fetched directly by crawlers (WhatsApp, Facebook), never through the browser —
    // it must always be absolute, unlike <img src> which can stay relative via resolveAssetUrl().
    const image = data.image ? this.toAbsolute(data.image) : `${environment.siteUrl}/images/hero-fraldas.jpg`;

    this.titleService.setTitle(fullTitle);

    this.setTag({ name: 'description' }, description);
    this.setTag({ property: 'og:title' }, fullTitle);
    this.setTag({ property: 'og:description' }, description);
    this.setTag({ property: 'og:type' }, data.type ?? 'website');
    this.setTag({ property: 'og:url' }, url);
    this.setTag({ property: 'og:image' }, image);
    this.setTag({ property: 'og:site_name' }, SITE_NAME);
    this.setTag({ property: 'og:locale' }, 'pt_BR');
    this.setTag({ name: 'twitter:card' }, 'summary_large_image');
    this.setTag({ name: 'twitter:title' }, fullTitle);
    this.setTag({ name: 'twitter:description' }, description);
    this.setTag({ name: 'twitter:image' }, image);

    this.setCanonical(url);

    // Cleared on every navigation so a product's rich-snippet data never lingers on the next,
    // unrelated page visited in this same SPA session — each page re-adds its own right after.
    for (const key of ['product', 'breadcrumb', 'faq'] as const) this.removeJsonLd(key);
  }

  /** Schema.org Product — lets Google show price, availability, rating and photos in results. */
  setProductStructuredData(product: ProductStructuredData): void {
    const offer: Record<string, unknown> = {
      '@type': 'Offer',
      url: `${environment.siteUrl}${product.url}`,
      priceCurrency: 'BRL',
      price: product.price.toFixed(2),
      itemCondition: 'https://schema.org/NewCondition',
      availability: product.inStock ? 'https://schema.org/InStock' : 'https://schema.org/OutOfStock',
      seller: { '@type': 'Organization', name: SITE_NAME },
    };
    if (product.priceValidUntil) offer['priceValidUntil'] = product.priceValidUntil.slice(0, 10);

    const data: Record<string, unknown> = {
      '@context': 'https://schema.org/',
      '@type': 'Product',
      name: product.name,
      image: product.images.map((image) => this.toAbsolute(image)),
      description: product.description || product.name,
      sku: product.sku,
      category: product.category,
      brand: { '@type': 'Brand', name: SITE_NAME },
      offers: offer,
    };

    if (product.ratingValue && product.reviewCount) {
      data['aggregateRating'] = {
        '@type': 'AggregateRating',
        ratingValue: product.ratingValue.toFixed(1),
        reviewCount: product.reviewCount,
        bestRating: 5,
        worstRating: 1,
      };
    }
    if (product.reviews?.length) {
      data['review'] = product.reviews.map((review) => ({
        '@type': 'Review',
        author: { '@type': 'Person', name: review.author },
        datePublished: review.date.slice(0, 10),
        reviewRating: { '@type': 'Rating', ratingValue: review.rating, bestRating: 5, worstRating: 1 },
        ...(review.comment ? { reviewBody: review.comment } : {}),
      }));
    }

    this.setJsonLd('product', data);
  }

  /** Schema.org BreadcrumbList — Google shows "layettebaby.com.br › Loja › Produto" instead of the raw URL. */
  setBreadcrumbs(items: { name: string; path: string }[]): void {
    this.setJsonLd('breadcrumb', {
      '@context': 'https://schema.org/',
      '@type': 'BreadcrumbList',
      itemListElement: items.map((item, index) => ({
        '@type': 'ListItem',
        position: index + 1,
        name: item.name,
        item: `${environment.siteUrl}${item.path}`,
      })),
    });
  }

  /** Schema.org FAQPage from the questions actually shown on the page. */
  setFaq(entries: { question: string; answer: string }[]): void {
    if (entries.length === 0) return;
    this.setJsonLd('faq', {
      '@context': 'https://schema.org/',
      '@type': 'FAQPage',
      mainEntity: entries.map((entry) => ({
        '@type': 'Question',
        name: entry.question,
        acceptedAnswer: { '@type': 'Answer', text: entry.answer },
      })),
    });
  }

  /** Cuts at a word boundary with an ellipsis, collapsing whitespace/line breaks from admin-typed text. */
  static trimDescription(text: string): string {
    const clean = text.replace(/\s+/g, ' ').trim();
    if (clean.length <= MAX_DESCRIPTION_LENGTH) return clean;
    const cut = clean.slice(0, MAX_DESCRIPTION_LENGTH - 1);
    const lastSpace = cut.lastIndexOf(' ');
    return `${(lastSpace > 80 ? cut.slice(0, lastSpace) : cut).replace(/[\s,.;:—-]+$/, '')}…`;
  }

  private setJsonLd(key: StructuredDataKey, data: Record<string, unknown>): void {
    let script = this.document.querySelector<HTMLScriptElement>(`script[data-seo="${key}"]`);
    if (!script) {
      script = this.document.createElement('script');
      script.setAttribute('type', 'application/ld+json');
      script.setAttribute('data-seo', key);
      this.document.head.appendChild(script);
    }
    script.textContent = JSON.stringify(data);
  }

  /** Only page-level blocks — the sitewide Organization/WebSite JSON-LD in index.html is untouched. */
  private removeJsonLd(key: StructuredDataKey): void {
    this.document.querySelector(`script[data-seo="${key}"]`)?.remove();
  }

  private toAbsolute(url: string): string {
    return /^https?:\/\//i.test(url) ? url : `${environment.siteUrl}${url.startsWith('/') ? '' : '/'}${url}`;
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
