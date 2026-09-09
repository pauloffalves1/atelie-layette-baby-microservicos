import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ProductZoomService {
  readonly imageUrl = signal<string | null>(null);
  readonly alt = signal<string>('');

  open(imageUrl: string, alt: string): void {
    this.imageUrl.set(imageUrl);
    this.alt.set(alt);
  }

  close(): void {
    this.imageUrl.set(null);
  }
}
