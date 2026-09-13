import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Product } from '@shared/core/models/product.model';
import { ProductReview, ReviewEligibility } from '@shared/core/models/review.model';
import { AuthService } from '@shared/core/services/auth.service';
import { CartService } from '@shared/core/services/cart.service';
import { CheckoutModalService } from '@shared/core/services/checkout-modal.service';
import { ProductService } from '@shared/core/services/product.service';
import { ReviewService } from '@shared/core/services/review.service';
import { SeoService } from '@shared/core/services/seo.service';
import { WishlistService } from '@shared/core/services/wishlist.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';
import { ImageLightbox } from '@shared/shared/components/image-lightbox/image-lightbox';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

const MAX_EMBROIDERY_LENGTH = 30;

/** Shown when a product has no lead time of its own configured in admin. */
const DEFAULT_PRODUCTION_LEAD_TIME_DAYS = 7;

/** Thread colors too pale to read against the preview's light fabric background without an outline. */
const LIGHT_THREAD_COLORS = new Set(['Branco', 'Bege', 'Rosa Bebê', 'Amarelo']);

/** Standard embroidery thread color palette offered on every product. */
export const THREAD_COLORS = [
  'Branco',
  'Preto',
  'Rosa',
  'Rosa Bebê',
  'Azul',
  'Azul Bebê',
  'Amarelo',
  'Verde',
  'Vermelho',
  'Lilás',
  'Cinza',
  'Marrom',
  'Bege',
  'Vinho',
];

/** Swatch hex per thread color, for the visual dot on each selection button. */
export const THREAD_COLOR_SWATCHES: Record<string, string> = {
  Branco: '#FFFFFF',
  Preto: '#111111',
  Rosa: '#EC4899',
  'Rosa Bebê': '#F9C6D7',
  Azul: '#1D4ED8',
  'Azul Bebê': '#A9C6E8',
  Amarelo: '#F5C518',
  Verde: '#2F9E44',
  Vermelho: '#D62828',
  Lilás: '#B49FCC',
  Cinza: '#9CA3AF',
  Marrom: '#7B4B2A',
  Bege: '#E3D2B4',
  Vinho: '#722036',
};

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, DecimalPipe, FormsModule, RouterLink, AssetUrlPipe, ImageLightbox],
  templateUrl: './product-detail.html',
})
export class ProductDetail implements OnInit {
  readonly alphabet = [..."ABCDEFGHIJKLMNOPQRSTUVWXYZ"];
  readonly threadColors = THREAD_COLORS;
  readonly threadColorSwatches = THREAD_COLOR_SWATCHES;
  readonly maxEmbroideryLength = MAX_EMBROIDERY_LENGTH;
  readonly defaultLeadTimeDays = DEFAULT_PRODUCTION_LEAD_TIME_DAYS;

  readonly product = signal<Product | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly quantity = signal(1);
  readonly embroideryText = signal('');
  readonly embroideryTouched = signal(false);
  readonly threadColor = signal('');
  readonly threadColorTouched = signal(false);
  readonly addedFeedback = signal(false);
  readonly activeImageIndex = signal(0);
  readonly isLightThread = computed(() => LIGHT_THREAD_COLORS.has(this.threadColor()));

  readonly galleryUrls = computed(() => {
    const p = this.product();
    if (!p) return [];
    return [...(p.imageUrl ? [p.imageUrl] : []), ...p.imageUrls];
  });

  readonly relatedProducts = signal<Product[]>([]);

  readonly reviews = signal<ProductReview[]>([]);
  readonly eligibility = signal<ReviewEligibility | null>(null);
  readonly reviewRating = signal(5);
  readonly reviewComment = signal('');
  readonly reviewPhotoUrl = signal<string | null>(null);
  readonly uploadingReviewPhoto = signal(false);
  readonly submittingReview = signal(false);
  readonly reviewError = signal<string | null>(null);

  readonly averageRating = computed(() => {
    const list = this.reviews();
    return list.length ? list.reduce((sum, r) => sum + r.rating, 0) / list.length : 0;
  });

  private readonly seo = inject(SeoService);
  readonly auth = inject(AuthService);
  private readonly reviewService = inject(ReviewService);
  private readonly wishlistService = inject(WishlistService);

  readonly isFavorited = signal(false);
  readonly favoriteBusy = signal(false);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly productService: ProductService,
    readonly cart: CartService,
    readonly checkoutModal: CheckoutModalService,
  ) {}

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug')!;
    this.productService.getBySlug(slug).subscribe({
      next: (product) => {
        this.product.set(product);
        this.loading.set(false);
        this.activeImageIndex.set(0);
        const description = product.description || `${product.name} — peça bordada do Ateliê Layette Baby, feita sob medida com carinho.`;
        this.seo.update({
          title: product.name,
          description,
          path: `/produto/${product.slug}`,
          image: product.imageUrl ? resolveAssetUrl(product.imageUrl) : undefined,
          type: 'product',
        });
        this.seo.setProductStructuredData({
          name: product.name,
          description,
          image: product.imageUrl ? resolveAssetUrl(product.imageUrl) : '/images/hero-fraldas.jpg',
          url: `/produto/${product.slug}`,
          price: product.effectivePrice,
          inStock: product.active,
        });

        this.reviewService.listByProduct(product.id).subscribe((reviews) => {
          this.reviews.set(reviews);
          if (reviews.length > 0) {
            this.seo.setProductStructuredData({
              name: product.name,
              description,
              image: product.imageUrl ? resolveAssetUrl(product.imageUrl) : '/images/hero-fraldas.jpg',
              url: `/produto/${product.slug}`,
              price: product.effectivePrice,
              inStock: product.active,
              ratingValue: reviews.reduce((sum, r) => sum + r.rating, 0) / reviews.length,
              reviewCount: reviews.length,
            });
          }
        });

        if (this.auth.currentUser()) {
          this.reviewService.getEligibility(product.id).subscribe({
            next: (eligibility) => this.eligibility.set(eligibility),
            error: () => {},
          });
          this.wishlistService.getStatus(product.id).subscribe({
            next: (status) => this.isFavorited.set(status.isFavorited),
            error: () => {},
          });
        }

        this.productService.list(product.category, 1, 5).subscribe({
          next: (result) => this.relatedProducts.set(result.items.filter((p) => p.id !== product.id).slice(0, 4)),
          error: () => {},
        });
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  selectImage(index: number): void {
    this.activeImageIndex.set(index);
  }

  /** Index of the photo open in the full-screen viewer, or null when it's closed. */
  readonly lightboxIndex = signal<number | null>(null);
  readonly resolvedGalleryUrls = computed(() => this.galleryUrls().map((url) => resolveAssetUrl(url)));
  private photoPointerStartX: number | null = null;
  private suppressPhotoClick = false;

  openLightbox(): void {
    // A swipe ends with a click on the same element — don't open the viewer for it.
    if (this.suppressPhotoClick) {
      this.suppressPhotoClick = false;
      return;
    }
    this.lightboxIndex.set(this.activeImageIndex());
  }

  changePhoto(delta: number): void {
    const count = this.galleryUrls().length;
    if (count > 1) this.activeImageIndex.set((this.activeImageIndex() + delta + count) % count);
  }

  onPhotoPointerDown(event: PointerEvent): void {
    this.photoPointerStartX = event.clientX;
  }

  /** Swiping the main photo sideways changes it in place, like on any phone gallery. */
  onPhotoPointerUp(event: PointerEvent): void {
    const startX = this.photoPointerStartX;
    this.photoPointerStartX = null;
    if (startX === null || this.galleryUrls().length < 2) return;
    const dx = event.clientX - startX;
    if (Math.abs(dx) > 50) {
      this.suppressPhotoClick = true;
      this.changePhoto(dx < 0 ? 1 : -1);
    }
  }

  toggleFavorite(): void {
    const product = this.product();
    if (!product || this.favoriteBusy()) return;

    this.favoriteBusy.set(true);
    const wasFavorited = this.isFavorited();
    const request = wasFavorited ? this.wishlistService.remove(product.id) : this.wishlistService.add(product.id);

    request.subscribe({
      next: () => {
        this.isFavorited.set(!wasFavorited);
        this.favoriteBusy.set(false);
      },
      error: () => this.favoriteBusy.set(false),
    });
  }

  setReviewRating(rating: number): void {
    this.reviewRating.set(rating);
  }

  onReviewPhotoSelected(event: Event): void {
    const product = this.product();
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!product || !file) return;

    this.uploadingReviewPhoto.set(true);
    this.reviewService.uploadPhoto(product.id, file).subscribe({
      next: ({ url }) => {
        this.reviewPhotoUrl.set(url);
        this.uploadingReviewPhoto.set(false);
      },
      error: (err) => {
        this.uploadingReviewPhoto.set(false);
        this.reviewError.set(err?.error?.detail ?? 'Não foi possível enviar a foto.');
      },
    });
  }

  removeReviewPhoto(): void {
    this.reviewPhotoUrl.set(null);
  }

  submitReview(): void {
    const product = this.product();
    if (!product) return;

    this.submittingReview.set(true);
    this.reviewError.set(null);

    this.reviewService
      .create(product.id, { rating: this.reviewRating(), comment: this.reviewComment().trim() || null, photoUrl: this.reviewPhotoUrl() })
      .subscribe({
        next: () => {
          // Not added to `reviews` — it's pending admin approval and won't show on the storefront
          // until then; the "alreadyReviewed" state below covers the submitter's own confirmation.
          this.eligibility.update((current) => (current ? { ...current, alreadyReviewed: true } : current));
          this.reviewComment.set('');
          this.reviewRating.set(5);
          this.reviewPhotoUrl.set(null);
          this.submittingReview.set(false);
        },
        error: (err) => {
          this.submittingReview.set(false);
          this.reviewError.set(err?.error?.detail ?? 'Não foi possível enviar sua avaliação.');
        },
      });
  }

  appendLetter(letter: string): void {
    if (this.embroideryText().length >= MAX_EMBROIDERY_LENGTH) return;
    this.embroideryText.update((text) => text + letter);
  }

  appendSpace(): void {
    if (this.embroideryText().length >= MAX_EMBROIDERY_LENGTH || this.embroideryText().endsWith(' ')) return;
    this.embroideryText.update((text) => text + ' ');
  }

  removeLastLetter(): void {
    this.embroideryText.update((text) => text.slice(0, -1));
  }

  clearEmbroideryText(): void {
    this.embroideryText.set('');
  }

  selectThreadColor(color: string): void {
    this.threadColor.set(color);
  }

  changeQuantity(delta: number): void {
    this.quantity.update((current) => Math.max(1, current + delta));
  }

  addToCart(): void {
    const product = this.product();
    if (!product) return;

    const missingText = !this.embroideryText().trim();
    const missingColor = !this.threadColor();
    this.embroideryTouched.set(missingText);
    this.threadColorTouched.set(missingColor);

    // On mobile the add button sits well below both fields (the letter grid alone is several rows),
    // so the error messages would otherwise render off-screen and the tap would look like it did nothing.
    if (missingText) {
      this.revealField('embroidery-text', true);
      return;
    }
    if (missingColor) {
      this.revealField('thread-color-group', false);
      return;
    }

    this.cart.add(product, this.quantity(), this.embroideryText().trim(), this.threadColor());
    this.addedFeedback.set(true);
    this.embroideryText.set('');
    this.embroideryTouched.set(false);
    this.threadColor.set('');
    this.threadColorTouched.set(false);
    this.quantity.set(1);
    setTimeout(() => this.addedFeedback.set(false), 5000);
  }

  private revealField(elementId: string, focus: boolean): void {
    const element = document.getElementById(elementId);
    if (!element) return;
    element.scrollIntoView({ behavior: 'smooth', block: 'center' });
    if (focus) element.focus({ preventScroll: true });
  }
}
