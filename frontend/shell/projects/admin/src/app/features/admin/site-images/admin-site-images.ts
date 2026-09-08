import { Component, OnInit, signal } from '@angular/core';
import { SiteImageService } from '@shared/core/services/site-image.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';

interface SiteImageSlot {
  key: string;
  label: string;
  defaultUrl: string;
}

/** Known image slots — mirrors the AllowedKeys set in the backend's SiteImageEndpoints. */
const SLOTS: SiteImageSlot[] = [
  { key: 'home-hero', label: 'Página inicial — imagem principal', defaultUrl: '/images/hero-fraldas.jpg' },
  { key: 'about', label: 'Sobre o ateliê — imagem', defaultUrl: '/images/sobre-fraldas.png' },
];

interface SiteImageRow {
  key: string;
  label: string;
  url: string;
  uploading: boolean;
  saved: boolean;
  error: string | null;
}

@Component({
  selector: 'app-admin-site-images',
  standalone: true,
  imports: [],
  templateUrl: './admin-site-images.html',
})
export class AdminSiteImages implements OnInit {
  readonly rows = signal<SiteImageRow[]>(
    SLOTS.map((slot) => ({ key: slot.key, label: slot.label, url: slot.defaultUrl, uploading: false, saved: false, error: null })),
  );

  constructor(private readonly siteImageService: SiteImageService) {}

  ngOnInit(): void {
    this.siteImageService.list().subscribe((images) => {
      this.rows.update((rows) =>
        rows.map((row) => {
          const match = images.find((i) => i.key === row.key);
          return match ? { ...row, url: resolveAssetUrl(match.url) } : row;
        }),
      );
    });
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

  private patchRow(key: string, patch: Partial<SiteImageRow>): void {
    this.rows.update((rows) => rows.map((row) => (row.key === key ? { ...row, ...patch } : row)));
  }
}
