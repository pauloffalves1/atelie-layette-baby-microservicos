import { Component, computed, input } from '@angular/core';
import qrcode from 'qrcode-generator';

/**
 * Renders a PIX copy-paste code as a scannable QR square, generated entirely client-side (no
 * third-party image service) — PagBank's own QR PNG link is only returned once, at charge
 * creation, and isn't persisted, so any later page load (a reload, or viewing the order in
 * "Minha conta"/admin) would otherwise have nothing to render.
 */
@Component({
  selector: 'app-pix-qr-code',
  standalone: true,
  templateUrl: './pix-qr-code.html',
})
export class PixQrCode {
  readonly text = input.required<string>();
  readonly size = input<number>(200);

  readonly dataUrl = computed(() => {
    const value = this.text();
    if (!value) return null;

    const qr = qrcode(0, 'M');
    qr.addData(value);
    qr.make();

    // Quiet zone of 2 modules on each side, sized so total width/height lands on `size()`.
    const quietZoneModules = 2;
    const cellSize = Math.max(1, Math.floor(this.size() / (qr.getModuleCount() + quietZoneModules * 2)));
    return qr.createDataURL(cellSize, cellSize * quietZoneModules);
  });
}
