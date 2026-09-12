import { Component, OnInit, signal } from '@angular/core';
import { SiteImage } from '@shared/core/models/site-image.model';
import { SiteImageService } from '@shared/core/services/site-image.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';

interface SiteImageSlot {
  key: string;
  label: string;
  defaultUrl: string;
  /** Multi-image slots render as a reorderable carousel on the public site instead of a single photo. */
  multi: boolean;
}

/** Known image slots — mirrors the AllowedKeys set in the backend's SiteImageEndpoints. */
const SLOTS: SiteImageSlot[] = [
  { key: 'home-hero', label: 'Página inicial — imagem principal (carrossel)', defaultUrl: '/images/hero-fraldas.jpg', multi: true },
  { key: 'about', label: 'Sobre o ateliê — imagem', defaultUrl: '/images/sobre-fraldas.png', multi: false },
];

interface SiteImageItemRow {
  id: string;
  url: string;
  moving: boolean;
}

interface SiteImageRow {
  key: string;
  label: string;
  multi: boolean;
  // Single-slot fields
  url: string;
  uploading: boolean;
  saved: boolean;
  error: string | null;
  // Multi-slot fields
  items: SiteImageItemRow[];
}

@Component({
  selector: 'app-admin-site-images',
  standalone: true,
  imports: [],
  templateUrl: './admin-site-images.html',
})
export class AdminSiteImages implements OnInit {
  readonly rows = signal<SiteImageRow[]>(
    SLOTS.map((slot) => ({
      key: slot.key,
      label: slot.label,
      multi: slot.multi,
      url: slot.defaultUrl,
      uploading: false,
      saved: false,
      error: null,
      items: [],
    })),
  );

  constructor(private readonly siteImageService: SiteImageService) {}

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.siteImageService.list().subscribe((images) => {
      this.rows.update((rows) =>
        rows.map((row) => {
          const matches = images
            .filter((i) => i.key === row.key)
            .sort((a, b) => a.sortOrder - b.sortOrder);

          if (row.multi) {
            return { ...row, items: matches.map((m) => this.toItemRow(m)) };
          }

          const match = matches[0];
          return match ? { ...row, url: resolveAssetUrl(match.url) } : row;
        }),
      );
    });
  }

  private toItemRow(image: SiteImage): SiteImageItemRow {
    return { id: image.id, url: resolveAssetUrl(image.url), moving: false };
  }

  onFileSelected(key: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.patchRow(key, { uploading: true, error: null, saved: false });

    this.siteImageService.upload(key, file).subscribe({
      next: (image) => {
        this.patchRow(key, { url: resolveAssetUrl(image.url), uploading: false, saved: true });
        setTimeout(() => this.patchRow(key, { saved: false }), 2500);
      },
      error: (err) => {
        this.patchRow(key, { uploading: false, error: err?.error?.detail ?? 'Não foi possível enviar a imagem.' });
      },
    });

    input.value = '';
  }

  onItemFileSelected(key: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.patchRow(key, { uploading: true, error: null });

    this.siteImageService.addItem(key, file).subscribe({
      next: () => {
        this.patchRow(key, { uploading: false });
        this.loadAll();
      },
      error: (err) => {
        this.patchRow(key, { uploading: false, error: err?.error?.detail ?? 'Não foi possível enviar a imagem.' });
      },
    });

    input.value = '';
  }

  deleteItem(key: string, itemId: string): void {
    this.siteImageService.deleteItem(itemId).subscribe({
      next: () => this.loadAll(),
      error: (err) => this.patchRow(key, { error: err?.error?.detail ?? 'Não foi possível remover a imagem.' }),
    });
  }

  moveItem(key: string, itemId: string, direction: 'Up' | 'Down'): void {
    this.patchItemRow(key, itemId, { moving: true });
    this.siteImageService.moveItem(itemId, direction).subscribe({
      next: () => this.loadAll(),
      error: (err) => {
        this.patchItemRow(key, itemId, { moving: false });
        this.patchRow(key, { error: err?.error?.detail ?? 'Não foi possível reordenar a imagem.' });
      },
    });
  }

  private patchRow(key: string, patch: Partial<SiteImageRow>): void {
    this.rows.update((rows) => rows.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  }

  private patchItemRow(key: string, itemId: string, patch: Partial<SiteImageItemRow>): void {
    this.rows.update((rows) =>
      rows.map((row) =>
        row.key === key
          ? { ...row, items: row.items.map((item) => (item.id === itemId ? { ...item, ...patch } : item)) }
          : row,
      ),
    );
  }
}
