import { Component, ElementRef, OnDestroy, OnInit, computed, effect, input, model, output, signal, viewChild } from '@angular/core';

/** Horizontal swipe distance (px) that counts as "next/previous" instead of a tap. */
const SWIPE_THRESHOLD = 50;
const ZOOM_SCALE = 2.5;

/**
 * Full-screen photo viewer shared by the product page and the "Dicas para o casal" gallery:
 * arrows, keyboard (←/→/Esc), swipe on touch screens, "2 de 5" counter, and tap/click-to-zoom on
 * the point touched (drag to look around while zoomed). Locks page scroll while open and gives
 * focus back to whatever opened it on close.
 */
@Component({
  selector: 'app-image-lightbox',
  standalone: true,
  templateUrl: './image-lightbox.html',
  host: { '(document:keydown)': 'onKeydown($event)' },
})
export class ImageLightbox implements OnInit, OnDestroy {
  readonly images = input.required<string[]>();
  readonly index = model.required<number>();
  readonly alt = input('Foto');
  readonly closed = output<void>();

  readonly zoomed = signal(false);
  readonly origin = signal('50% 50%');
  readonly current = computed(() => this.images()[this.index()] ?? '');
  readonly hasMany = computed(() => this.images().length > 1);

  private readonly closeButton = viewChild<ElementRef<HTMLButtonElement>>('closeButton');
  private readonly previouslyFocused = document.activeElement as HTMLElement | null;
  private pointerStart: { x: number; y: number } | null = null;
  private dragged = false;

  constructor() {
    effect(() => this.closeButton()?.nativeElement.focus());
    // Leaving zoom when the photo changes, so the next one starts whole.
    effect(() => {
      this.index();
      this.zoomed.set(false);
    });
  }

  ngOnInit(): void {
    document.body.classList.add('overflow-hidden');
  }

  ngOnDestroy(): void {
    document.body.classList.remove('overflow-hidden');
    this.previouslyFocused?.focus?.();
  }

  close(): void {
    this.closed.emit();
  }

  next(): void {
    if (this.hasMany()) this.index.set((this.index() + 1) % this.images().length);
  }

  previous(): void {
    if (this.hasMany()) this.index.set((this.index() - 1 + this.images().length) % this.images().length);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') this.close();
    else if (event.key === 'ArrowRight') this.next();
    else if (event.key === 'ArrowLeft') this.previous();
  }

  onPointerDown(event: PointerEvent): void {
    this.pointerStart = { x: event.clientX, y: event.clientY };
    this.dragged = false;
  }

  onPointerMove(event: PointerEvent): void {
    if (!this.pointerStart) return;
    if (Math.abs(event.clientX - this.pointerStart.x) > 8 || Math.abs(event.clientY - this.pointerStart.y) > 8) this.dragged = true;
    // While zoomed, dragging pans: the zoom origin follows the pointer across the photo.
    if (this.zoomed()) this.setOrigin(event);
  }

  onPointerUp(event: PointerEvent): void {
    const start = this.pointerStart;
    this.pointerStart = null;
    if (!start) return;

    const dx = event.clientX - start.x;
    if (!this.zoomed() && Math.abs(dx) > SWIPE_THRESHOLD && Math.abs(dx) > Math.abs(event.clientY - start.y)) {
      if (dx < 0) this.next();
      else this.previous();
      return;
    }
    if (!this.dragged) this.toggleZoom(event);
  }

  private toggleZoom(event: PointerEvent): void {
    if (!this.zoomed()) this.setOrigin(event);
    this.zoomed.update((z) => !z);
  }

  private setOrigin(event: PointerEvent): void {
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
    const x = Math.min(100, Math.max(0, ((event.clientX - rect.left) / rect.width) * 100));
    const y = Math.min(100, Math.max(0, ((event.clientY - rect.top) / rect.height) * 100));
    this.origin.set(`${x}% ${y}%`);
  }

  readonly zoomScale = ZOOM_SCALE;
}
