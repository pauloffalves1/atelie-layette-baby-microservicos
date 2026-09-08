import { Component, HostListener, OnInit, signal } from '@angular/core';
import { GalleryImageService } from '@shared/core/services/gallery-image.service';
import { SeoService } from '@shared/core/services/seo.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';

const FALLBACK_IMAGES = Array.from(
  { length: 12 },
  (_, i) => `https://picsum.photos/seed/atelie-bebe-galeria-${i + 1}/600/700`,
);

@Component({
  selector: 'app-gallery',
  standalone: true,
  templateUrl: './gallery.html',
})
export class Gallery implements OnInit {
  readonly images = signal<string[]>(FALLBACK_IMAGES);
  readonly selectedIndex = signal<number | null>(null);

  constructor(
    private readonly galleryImageService: GalleryImageService,
    private readonly seo: SeoService,
  ) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Galeria',
      description: 'Veja fotos de fraldas de ombro e boca bordadas já entregues pelo Ateliê Layette Baby — inspire-se para a sua encomenda personalizada.',
      path: '/galeria',
    });

    this.galleryImageService.list().subscribe({
      next: (images) => {
        if (images.length > 0) this.images.set(images.map((i) => resolveAssetUrl(i.url)));
      },
      error: () => {},
    });
  }

  open(index: number): void {
    this.selectedIndex.set(index);
  }

  close(): void {
    this.selectedIndex.set(null);
  }

  next(): void {
    const index = this.selectedIndex();
    if (index === null) return;
    this.selectedIndex.set((index + 1) % this.images().length);
  }

  previous(): void {
    const index = this.selectedIndex();
    if (index === null) return;
    this.selectedIndex.set((index - 1 + this.images().length) % this.images().length);
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (this.selectedIndex() === null) return;

    if (event.key === 'Escape') this.close();
    if (event.key === 'ArrowRight') this.next();
    if (event.key === 'ArrowLeft') this.previous();
  }
}
