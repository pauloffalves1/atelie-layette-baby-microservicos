import { CurrencyPipe } from '@angular/common';
import { Component, ElementRef, Injector, OnDestroy, OnInit, afterNextRender, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProductService } from '@shared/core/services/product.service';
import { ReviewService } from '@shared/core/services/review.service';
import { SeoService } from '@shared/core/services/seo.service';
import { SiteImageService } from '@shared/core/services/site-image.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';
import { Product } from '@shared/core/models/product.model';
import { FeaturedReview } from '@shared/core/models/review.model';
import { LoadError } from '@shared/shared/components/load-error/load-error';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

const AUTO_ADVANCE_MS = 6000;
const SWIPE_THRESHOLD_PX = 50;
const FEATURED_ADVANCE_MS = 4500;

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, AssetUrlPipe, LoadError],
  templateUrl: './home.html',
  host: { '(window:resize)': 'onFeaturedResize()' },
})
export class Home implements OnInit, OnDestroy {
  readonly featured = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly featuredError = signal(false);

  /** Featured products run as a one-row carousel (5 cards on desktop) instead of a grid that wrapped
   * into two rows. A native scroll-snap track: swipe works on touch, and the timer just scrolls it. */
  private readonly featuredTrack = viewChild<ElementRef<HTMLElement>>('featuredTrack');
  private readonly injector = inject(Injector);
  readonly featuredScrollable = signal(false);
  private featuredPaused = false;
  private featuredHandle: ReturnType<typeof setInterval> | null = null;
  // Empty until the site-images lookup resolves, so the template renders nothing rather than a
  // default image that then gets swapped for the real one (a visible "flash" on every load).
  // Admin can register one or several "home-hero" images — several render as a carousel.
  readonly heroImages = signal<string[]>([]);
  readonly activeHeroIndex = signal(0);
  /** Slides whose photo has been requested. All of them used to download on page load (seven
   * photos, one a 1.7 MB PNG) before the first was even visible — now the active slide and the
   * next one, so the crossfade never shows a blank. */
  readonly heroLoaded = signal<ReadonlySet<number>>(new Set([0]));
  private heroAutoAdvanceHandle: ReturnType<typeof setInterval> | null = null;

  // Empty until there's at least one approved review with a comment — hides the whole section
  // rather than showing it half-empty.
  readonly featuredReviews = signal<FeaturedReview[]>([]);
  // Carousel is driven entirely from here (no Bootstrap JS in this app — see public-layout.ts's
  // native dropdown/collapse toggles for the same reason): only carousel's CSS partial is
  // imported, this signal picks which .carousel-item gets the .active class.
  readonly activeReviewIndex = signal(0);
  private autoAdvanceHandle: ReturnType<typeof setInterval> | null = null;

  /** Auto-advance holds still while the visitor is hovering/focused on a carousel (reading a review,
   * about to click a dot) and never starts for people who asked the OS for reduced motion. */
  private heroPaused = false;
  private reviewsPaused = false;
  private readonly reducedMotion =
    typeof window !== 'undefined' && !!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
  private swipeStartX: number | null = null;

  constructor(
    private readonly productService: ProductService,
    private readonly siteImageService: SiteImageService,
    private readonly reviewService: ReviewService,
    private readonly seo: SeoService,
  ) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Fraldas de ombro e boca personalizadas',
      description: 'Fraldas de ombro e boca bordadas à mão para o enxoval do bebê — kits e encomendas personalizadas, feitas com carinho pelo Ateliê Layette Baby.',
      path: '/',
    });

    this.loadFeatured();

    this.siteImageService.list().subscribe({
      next: (images) => {
        const heroImages = images
          .filter((i) => i.key === 'home-hero')
          .sort((a, b) => a.sortOrder - b.sortOrder)
          .map((i) => resolveAssetUrl(i.url));
        this.heroImages.set(heroImages.length > 0 ? heroImages : ['/images/hero-fraldas.jpg']);
        this.markHeroLoaded(0);
        if (heroImages.length > 1) this.startHeroAutoAdvance();
      },
      error: () => this.heroImages.set(['/images/hero-fraldas.jpg']),
    });

    this.reviewService.listFeatured().subscribe({
      next: (reviews) => {
        this.featuredReviews.set(reviews);
        if (reviews.length > 1) this.startAutoAdvance();
      },
      error: () => this.featuredReviews.set([]),
    });
  }

  loadFeatured(): void {
    this.loading.set(true);
    this.featuredError.set(false);
    this.productService.listFeatured().subscribe({
      next: (products) => {
        this.featured.set(products);
        this.loading.set(false);
        afterNextRender(() => this.startFeaturedCarousel(), { injector: this.injector });
      },
      error: () => {
        this.loading.set(false);
        this.featuredError.set(true);
      },
    });
  }

  setHeroPaused(paused: boolean): void {
    this.heroPaused = paused;
  }

  setReviewsPaused(paused: boolean): void {
    this.reviewsPaused = paused;
  }

  onHeroPointerDown(event: PointerEvent): void {
    this.swipeStartX = event.clientX;
  }

  onHeroPointerUp(event: PointerEvent): void {
    if (this.swipeStartX === null) return;
    const deltaX = event.clientX - this.swipeStartX;
    this.swipeStartX = null;
    if (Math.abs(deltaX) < SWIPE_THRESHOLD_PX || this.heroImages().length < 2) return;
    if (deltaX < 0) this.nextHeroImage();
    else this.previousHeroImage();
  }

  ngOnDestroy(): void {
    this.stopAutoAdvance();
    this.stopHeroAutoAdvance();
    this.stopFeaturedCarousel();
  }

  setFeaturedPaused(paused: boolean): void {
    this.featuredPaused = paused;
  }

  /** One card per step; past the last card it goes back to the first (and vice versa). */
  scrollFeatured(step: 1 | -1): void {
    const track = this.featuredTrack()?.nativeElement;
    const card = track?.firstElementChild as HTMLElement | null;
    if (!track || !card) return;
    const gap = parseFloat(getComputedStyle(track).columnGap) || 0;
    const cardWidth = card.offsetWidth + gap;
    const atEnd = track.scrollLeft + track.clientWidth >= track.scrollWidth - 4;
    const atStart = track.scrollLeft <= 4;
    if (step > 0 && atEnd) track.scrollTo({ left: 0, behavior: 'smooth' });
    else if (step < 0 && atStart) track.scrollTo({ left: track.scrollWidth, behavior: 'smooth' });
    else track.scrollBy({ left: step * cardWidth, behavior: 'smooth' });
  }

  /** Manual arrows restart the timer, so a click isn't immediately followed by an automatic step. */
  onFeaturedArrow(step: 1 | -1): void {
    this.scrollFeatured(step);
    this.stopFeaturedCarousel();
    this.startFeaturedCarousel();
  }

  /** Rotating the phone or resizing the window changes how many cards fit — and whether there is anything to scroll. */
  onFeaturedResize(): void {
    const track = this.featuredTrack()?.nativeElement;
    if (!track) return;
    if (track.scrollWidth > track.clientWidth + 4) this.startFeaturedCarousel();
    else {
      this.featuredScrollable.set(false);
      this.stopFeaturedCarousel();
    }
  }

  private startFeaturedCarousel(): void {
    const track = this.featuredTrack()?.nativeElement;
    this.featuredScrollable.set(!!track && track.scrollWidth > track.clientWidth + 4);
    if (!this.featuredScrollable() || this.reducedMotion || this.featuredHandle) return;
    this.featuredHandle = setInterval(() => {
      if (!this.featuredPaused && !document.hidden) this.scrollFeatured(1);
    }, FEATURED_ADVANCE_MS);
  }

  private stopFeaturedCarousel(): void {
    if (this.featuredHandle) clearInterval(this.featuredHandle);
    this.featuredHandle = null;
  }

  nextHeroImage(): void {
    this.advanceHero(1);
    this.restartHeroAutoAdvance();
  }

  previousHeroImage(): void {
    this.advanceHero(-1);
    this.restartHeroAutoAdvance();
  }

  goToHeroImage(index: number): void {
    this.activeHeroIndex.set(index);
    this.markHeroLoaded(index);
    this.restartHeroAutoAdvance();
  }

  private advanceHero(step: 1 | -1): void {
    const count = this.heroImages().length;
    this.activeHeroIndex.set((this.activeHeroIndex() + step + count) % count);
    this.markHeroLoaded(this.activeHeroIndex());
  }

  private markHeroLoaded(index: number): void {
    const count = this.heroImages().length;
    if (count === 0) return;
    const next = new Set(this.heroLoaded());
    next.add(index);
    next.add((index + 1) % count);
    if (next.size !== this.heroLoaded().size) this.heroLoaded.set(next);
  }

  private startHeroAutoAdvance(): void {
    if (this.reducedMotion) return;
    this.heroAutoAdvanceHandle = setInterval(() => {
      if (!this.heroPaused && !document.hidden) this.advanceHero(1);
    }, AUTO_ADVANCE_MS);
  }

  private stopHeroAutoAdvance(): void {
    if (this.heroAutoAdvanceHandle) clearInterval(this.heroAutoAdvanceHandle);
    this.heroAutoAdvanceHandle = null;
  }

  private restartHeroAutoAdvance(): void {
    this.stopHeroAutoAdvance();
    if (this.heroImages().length > 1) this.startHeroAutoAdvance();
  }

  nextReview(): void {
    this.advance(1);
    this.restartAutoAdvance();
  }

  previousReview(): void {
    this.advance(-1);
    this.restartAutoAdvance();
  }

  goToReview(index: number): void {
    this.activeReviewIndex.set(index);
    this.restartAutoAdvance();
  }

  private advance(step: 1 | -1): void {
    const count = this.featuredReviews().length;
    this.activeReviewIndex.set((this.activeReviewIndex() + step + count) % count);
  }

  private startAutoAdvance(): void {
    if (this.reducedMotion) return;
    this.autoAdvanceHandle = setInterval(() => {
      if (!this.reviewsPaused && !document.hidden) this.advance(1);
    }, AUTO_ADVANCE_MS);
  }

  private stopAutoAdvance(): void {
    if (this.autoAdvanceHandle) clearInterval(this.autoAdvanceHandle);
    this.autoAdvanceHandle = null;
  }

  private restartAutoAdvance(): void {
    this.stopAutoAdvance();
    if (this.featuredReviews().length > 1) this.startAutoAdvance();
  }
}
