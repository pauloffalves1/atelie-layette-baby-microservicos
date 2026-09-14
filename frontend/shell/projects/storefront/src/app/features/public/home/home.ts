import { CurrencyPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
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

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, AssetUrlPipe, LoadError],
  templateUrl: './home.html',
})
export class Home implements OnInit, OnDestroy {
  readonly featured = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly featuredError = signal(false);
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
