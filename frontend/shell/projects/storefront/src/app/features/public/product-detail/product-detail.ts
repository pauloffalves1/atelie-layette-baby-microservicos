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
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

const MAX_EMBROIDERY_LENGTH = 30;

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
  imports: [CurrencyPipe, DatePipe, DecimalPipe, FormsModule, RouterLink, AssetUrlPipe],
  templateUrl: './product-detail.html',
})
export class ProductDetail implements OnInit {
  readonly alphabet = [..."ABCDEFGHIJKLMNOPQRSTUVWXYZ"];
  readonly threadColors = THREAD_COLORS;
  readonly threadColorSwatches = THREAD_COLOR_SWATCHES;

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
  readonly zoomOpen = signal(false);

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

  openZoom(): void {
    this.zoomOpen.set(true);
  }

  closeZoom(): void {
    this.zoomOpen.set(false);
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
        next: (review) => {
          this.reviews.update((list) => [review, ...list]);
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

  addToCart(): void {
    const product = this.product();
    if (!product) return;

    let blocked = false;
    if (!this.embroideryText().trim()) {
      this.embroideryTouched.set(true);
      blocked = true;
    }
    if (!this.threadColor()) {
      this.threadColorTouched.set(true);
      blocked = true;
    }
    if (blocked) return;

    this.cart.add(product, this.quantity(), this.embroideryText().trim(), this.threadColor());
    this.addedFeedback.set(true);
    this.embroideryText.set('');
    this.embroideryTouched.set(false);
    this.threadColor.set('');
    this.threadColorTouched.set(false);
    setTimeout(() => this.addedFeedback.set(false), 2500);
  }
}
