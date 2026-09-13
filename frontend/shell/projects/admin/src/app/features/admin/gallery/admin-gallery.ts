import { Component, OnInit, signal } from '@angular/core';
import { GalleryImage } from '@shared/core/models/gallery-image.model';
import { GalleryImageService } from '@shared/core/services/gallery-image.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';
import { LoadError } from '@shared/shared/components/load-error/load-error';

interface GalleryImageRow extends GalleryImage {
  displayUrl: string;
  deleting: boolean;
}

@Component({
  selector: 'app-admin-gallery',
  standalone: true,
  imports: [LoadError],
  templateUrl: './admin-gallery.html',
})
export class AdminGallery implements OnInit {
  readonly images = signal<GalleryImageRow[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly uploading = signal(false);
  readonly uploadProgress = signal<{ done: number; total: number } | null>(null);
  readonly error = signal<string | null>(null);

  constructor(private readonly galleryImageService: GalleryImageService) {}

  ngOnInit(): void {
    this.load();
  }

  /** Several photos can be picked at once; they upload one after another so the progress label
   * stays meaningful and a single failure doesn't stop the rest. */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';
    if (files.length === 0) return;

    this.uploading.set(true);
    this.error.set(null);
    const failed: string[] = [];

    const uploadNext = (i: number): void => {
      if (i >= files.length) {
        this.uploading.set(false);
        this.uploadProgress.set(null);
        if (failed.length > 0) {
          this.error.set(`Não foi possível enviar ${failed.length === 1 ? 'a foto' : 'as fotos'}: ${failed.join(', ')}.`);
        }
        return;
      }
      this.uploadProgress.set({ done: i, total: files.length });
      this.galleryImageService.upload(files[i]).subscribe({
        next: (image) => {
          this.images.update((rows) => [{ ...image, displayUrl: resolveAssetUrl(image.url), deleting: false }, ...rows]);
          uploadNext(i + 1);
        },
        error: () => {
          failed.push(files[i].name);
          uploadNext(i + 1);
        },
      });
    };
    uploadNext(0);
  }

  deleteImage(id: string): void {
    // The photo disappears from the public page immediately and can't be restored from here.
    if (!confirm('Remover esta foto da galeria? Ela sai da página pública na hora.')) return;
    this.patchRow(id, { deleting: true });

    this.galleryImageService.delete(id).subscribe({
      next: () => this.images.update((rows) => rows.filter((row) => row.id !== id)),
      error: (err) => {
        this.patchRow(id, { deleting: false });
        this.error.set(err?.error?.detail ?? 'Não foi possível remover a imagem.');
      },
    });
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.galleryImageService.list().subscribe({
      next: (images) => {
        this.images.set(images.map((i) => ({ ...i, displayUrl: resolveAssetUrl(i.url), deleting: false })));
        this.loading.set(false);
      },
      error: () => {
          this.loading.set(false);
          this.loadError.set(true);
        },
    });
  }

  private patchRow(id: string, patch: Partial<GalleryImageRow>): void {
    this.images.update((rows) => rows.map((row) => (row.id === id ? { ...row, ...patch } : row)));
  }
}
