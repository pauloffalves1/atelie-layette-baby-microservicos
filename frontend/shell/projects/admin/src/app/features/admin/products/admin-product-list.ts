import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '@shared/core/models/product.model';
import { ProductService } from '@shared/core/services/product.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

@Component({
  selector: 'app-admin-product-list',
  standalone: true,
  imports: [CurrencyPipe, RouterLink, Pagination, AssetUrlPipe],
  templateUrl: './admin-product-list.html',
})
export class AdminProductList implements OnInit {
  readonly products = signal<Product[]>([]);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly loading = signal(true);

  readonly selectedIds = signal<string[]>([]);
  readonly bulkDiscount = signal<number | null>(null);
  readonly bulkStartsAt = signal('');
  readonly bulkEndsAt = signal('');
  readonly applyingBulkPromotion = signal(false);
  readonly bulkPromotionError = signal<string | null>(null);
  readonly bulkPromotionApplied = signal(false);

  constructor(private readonly productService: ProductService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.productService.listAllForAdmin(this.page()).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  toggleActive(product: Product): void {
    this.productService.setActive(product.id, !product.active).subscribe(() => this.load());
  }

  toggleSelected(productId: string, checked: boolean): void {
    const current = this.selectedIds();
    this.selectedIds.set(checked ? [...current, productId] : current.filter((id) => id !== productId));
  }

  clearSelection(): void {
    this.selectedIds.set([]);
  }

  applyBulkPromotion(): void {
    if (!this.bulkDiscount() || !this.bulkStartsAt() || !this.bulkEndsAt()) {
      this.bulkPromotionError.set('Preencha o desconto e o período (início e fim) da promoção.');
      return;
    }

    this.applyingBulkPromotion.set(true);
    this.bulkPromotionError.set(null);

    this.productService
      .applyPromotionToMany({
        productIds: this.selectedIds(),
        discountPercentage: this.bulkDiscount(),
        startsAt: new Date(this.bulkStartsAt()).toISOString(),
        endsAt: new Date(this.bulkEndsAt()).toISOString(),
      })
      .subscribe({
        next: () => {
          this.applyingBulkPromotion.set(false);
          this.bulkPromotionApplied.set(true);
          this.selectedIds.set([]);
          this.bulkDiscount.set(null);
          this.bulkStartsAt.set('');
          this.bulkEndsAt.set('');
          this.load();
          setTimeout(() => this.bulkPromotionApplied.set(false), 3000);
        },
        error: (err) => {
          this.applyingBulkPromotion.set(false);
          this.bulkPromotionError.set(err?.error?.detail ?? 'Não foi possível aplicar a promoção.');
        },
      });
  }
}
