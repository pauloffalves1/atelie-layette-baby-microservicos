import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { GalleryImageService } from '@shared/core/services/gallery-image.service';
import { ProductService } from '@shared/core/services/product.service';
import { SeoService } from '@shared/core/services/seo.service';
import { SiteImageService } from '@shared/core/services/site-image.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';
import { Product } from '@shared/core/models/product.model';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

const SHOWCASE_LIMIT = 6;

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, AssetUrlPipe],
  templateUrl: './home.html',
})
export class Home implements OnInit {
  readonly featured = signal<Product[]>([]);
  readonly loading = signal(true);
  // Null until the site-images lookup resolves, so the template renders nothing rather than a
  // default image that then gets swapped for the real one (a visible "flash" on every load).
  readonly heroImageUrl = signal<string | null>(null);
  // Real delivered-work photos for the trust/social-proof section — empty until the admin has
  // uploaded at least one (no fallback placeholders here, unlike the full /galeria page, since a
  // home section with obviously-fake stock photos would undermine the trust it's meant to build).
  readonly showcaseImages = signal<string[]>([]);

  constructor(
    private readonly productService: ProductService,
    private readonly siteImageService: SiteImageService,
    private readonly galleryImageService: GalleryImageService,
    private readonly seo: SeoService,
  ) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Fraldas de ombro e boca personalizados',
      description: 'Fraldas de ombro e boca bordadas à mão para o enxoval do bebê — kits e encomendas personalizadas, feitas com carinho pelo Ateliê Layette Baby.',
      path: '/',
    });

    this.productService.listFeatured().subscribe({
      next: (products) => {
        this.featured.set(products);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });

    this.siteImageService.list().subscribe({
      next: (images) => {
        const hero = images.find((i) => i.key === 'home-hero');
        this.heroImageUrl.set(hero ? resolveAssetUrl(hero.url) : '/images/hero-fraldas.jpg');
      },
      error: () => this.heroImageUrl.set('/images/hero-fraldas.jpg'),
    });

    this.galleryImageService.list().subscribe({
      next: (images) => {
        this.showcaseImages.set(images.slice(0, SHOWCASE_LIMIT).map((i) => resolveAssetUrl(i.url)));
      },
      error: () => this.showcaseImages.set([]),
    });
  }
}
