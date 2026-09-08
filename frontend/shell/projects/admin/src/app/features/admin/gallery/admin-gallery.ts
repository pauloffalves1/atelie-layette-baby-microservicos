import { Component, OnInit, signal } from '@angular/core';
import { GalleryImage } from '@shared/core/models/gallery-image.model';
import { GalleryImageService } from '@shared/core/services/gallery-image.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';

interface GalleryImageRow extends GalleryImage {
  displayUrl: string;
  deleting: boolean;
}

@Component({
  selector: 'app-admin-gallery',
  standalone: true,
  imports: [],
  templateUrl: './admin-gallery.html',
})
export class AdminGallery implements OnInit {
  readonly images = signal<GalleryImageRow[]>([]);
  readonly loading = signal(true);
  readonly uploading = signal(false);
  readonly error = signal<string | null>(null);

  constructor(private readonly galleryImageService: GalleryImageService) {}

  ngOnInit(): void {
    this.load();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);
    this.error.set(null);

    this.galleryImageService.upload(file).subscribe({
      next: (image) => {
        this.images.update((rows) => [{ ...image, displayUrl: resolveAssetUrl(image.url), deleting: false }, ...rows]);
        this.uploading.set(false);
      },
      error: (err) => {
        this.uploading.set(false);
        this.error.set(err?.error?.detail ?? 'Não foi possível enviar a imagem.');
      },
    });

    input.value = '';
  }

  deleteImage(id: string): void {
    this.patchRow(id, { deleting: true });

    this.galleryImageService.delete(id).subscribe({
      next: () => this.images.update((rows) => rows.filter((row) => row.id !== id)),
      error: (err) => {
        this.patchRow(id, { deleting: false });
        this.error.set(err?.error?.detail ?? 'Não foi possível remover a imagem.');
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.galleryImageService.list().subscribe({
      next: (images) => {
        this.images.set(images.map((i) => ({ ...i, displayUrl: resolveAssetUrl(i.url), deleting: false })));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private patchRow(id: string, patch: Partial<GalleryImageRow>): void {
    this.images.update((rows) => rows.map((row) => (row.id === id ? { ...row, ...patch } : row)));
  }
}
