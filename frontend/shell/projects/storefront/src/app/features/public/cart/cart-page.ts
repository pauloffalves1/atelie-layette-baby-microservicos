import { CurrencyPipe } from '@angular/common';
import { Component, computed, effect, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CartService } from '@shared/core/services/cart.service';
import { Product } from '@shared/core/models/product.model';
import { ProductService } from '@shared/core/services/product.service';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

const KIT_CATEGORY = 'Kit Ombro e Boca';

@Component({
  selector: 'app-cart-page',
  standalone: true,
  imports: [CurrencyPipe, RouterLink, AssetUrlPipe],
  templateUrl: './cart-page.html',
})
export class CartPage {
  readonly kitSuggestions = signal<Product[]>([]);

  /** Cart has a loose "Fralda de Ombro"/"Fralda de Boca" item but no Kit yet — worth suggesting one. */
  readonly showKitSuggestion = computed(() => {
    const categories = this.cart.items().map((i) => i.product.category);
    return categories.some((c) => c !== KIT_CATEGORY) && !categories.includes(KIT_CATEGORY);
  });

  constructor(
    readonly cart: CartService,
    private readonly productService: ProductService,
  ) {
    effect(() => {
      if (!this.showKitSuggestion() || this.kitSuggestions().length > 0) return;
      this.productService.list(KIT_CATEGORY, 1, 3).subscribe((result) => this.kitSuggestions.set(result.items));
    });
  }

  increment(productId: string, current: number, embroideryText?: string | null, threadColor?: string | null): void {
    this.cart.updateQuantity(productId, current + 1, embroideryText, threadColor);
  }

  decrement(productId: string, current: number, embroideryText?: string | null, threadColor?: string | null): void {
    this.cart.updateQuantity(productId, current - 1, embroideryText, threadColor);
  }
}
