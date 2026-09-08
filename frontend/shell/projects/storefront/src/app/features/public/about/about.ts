import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '@shared/core/services/seo.service';
import { SiteImageService } from '@shared/core/services/site-image.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './about.html',
})
export class About implements OnInit {
  // Null until the site-images lookup resolves, so the template renders nothing rather than a
  // default image that then gets swapped for the real one (a visible "flash" on every load).
  readonly imageUrl = signal<string | null>(null);

  constructor(
    private readonly siteImageService: SiteImageService,
    private readonly seo: SeoService,
  ) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Sobre o ateliê',
      description: 'Conheça a história do Ateliê Layette Baby: fraldas de ombro e boca costuradas à mão, com tecidos selecionados e bordados feitos com carinho.',
      path: '/sobre',
    });

    this.siteImageService.list().subscribe({
      next: (images) => {
        const about = images.find((i) => i.key === 'about');
        this.imageUrl.set(about ? resolveAssetUrl(about.url) : '/images/sobre-fraldas.png');
      },
      error: () => this.imageUrl.set('/images/sobre-fraldas.png'),
    });
  }
}
