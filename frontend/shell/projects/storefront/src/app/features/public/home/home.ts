import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductService } from '@shared/core/services/product.service';
import { SeoService } from '@shared/core/services/seo.service';
import { SiteImageService } from '@shared/core/services/site-image.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';
import { Product } from '@shared/core/models/product.model';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

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

  constructor(
    private readonly productService: ProductService,
    private readonly siteImageService: SiteImageService,
    private readonly seo: SeoService,
  ) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Fraldas de ombro e boca bordadas',
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
  }
}
