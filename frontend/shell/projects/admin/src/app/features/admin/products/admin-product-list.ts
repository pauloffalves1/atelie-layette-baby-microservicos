import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Product } from '@shared/core/models/product.model';
import { ProductService } from '@shared/core/services/product.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';
import { LoadError } from '@shared/shared/components/load-error/load-error';

const SEARCH_DEBOUNCE_MS = 350;

/** Search, category and page live in the URL (?busca=&categoria=&pagina=) — same reasoning as the
 * order list: coming back from editing a product lands on the same filtered page. */
@Component({
  selector: 'app-admin-product-list',
  standalone: true,
  imports: [CurrencyPipe, RouterLink, Pagination, AssetUrlPipe, LoadError],
  templateUrl: './admin-product-list.html',
})
export class AdminProductList {
  private readonly productService = inject(ProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly products = signal<Product[]>([]);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalItems = signal(0);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly search = signal('');
  readonly category = signal('');
  readonly categories = signal<string[]>([]);
  readonly actionError = signal<string | null>(null);

  readonly deletingId = signal<string | null>(null);
  readonly togglingId = signal<string | null>(null);

  readonly selectedIds = signal<string[]>([]);
  readonly allOnPageSelected = computed(
    () => this.products().length > 0 && this.products().every((p) => this.selectedIds().includes(p.id)),
  );
  readonly bulkDiscount = signal<number | null>(null);
  readonly bulkStartsAt = signal('');
  readonly bulkEndsAt = signal('');
  readonly applyingBulkPromotion = signal(false);
  readonly bulkPromotionError = signal<string | null>(null);
  readonly bulkPromotionApplied = signal(false);

  private searchTimeout: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.productService.listCategories().subscribe({ next: (categories) => this.categories.set(categories), error: () => {} });

    this.route.queryParamMap.pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe((params) => {
      this.search.set(params.get('busca') ?? '');
      this.category.set(params.get('categoria') ?? '');
      this.page.set(Math.max(1, Number(params.get('pagina')) || 1));
      this.load();
    });
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.productService
      .listAllForAdmin(this.page(), 20, this.search().trim() || undefined, this.category() || undefined)
      .subscribe({
        next: (result) => {
          this.products.set(result.items);
          this.totalPages.set(result.totalPages);
          this.totalItems.set(result.totalItems);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.loadError.set(true);
        },
      });
  }

  onSearchInput(value: string): void {
    this.search.set(value);
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => this.updateQuery({ busca: value.trim() || null, pagina: null }), SEARCH_DEBOUNCE_MS);
  }

  clearFilters(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.updateQuery({ busca: null, categoria: null, pagina: null });
  }

  filterByCategory(category: string): void {
    this.updateQuery({ categoria: category || null, pagina: null });
  }

  goToPage(page: number): void {
    this.updateQuery({ pagina: page > 1 ? page : null });
  }

  toggleActive(product: Product): void {
    if (this.togglingId()) return;
    this.togglingId.set(product.id);
    this.actionError.set(null);
    this.productService.setActive(product.id, !product.active).subscribe({
      next: () => {
        this.togglingId.set(null);
        this.load();
      },
      error: (err) => {
        this.togglingId.set(null);
        this.actionError.set(err?.error?.detail ?? `Não foi possível ${product.active ? 'inativar' : 'ativar'} "${product.name}".`);
      },
    });
  }

  deleteProduct(product: Product): void {
    if (this.deletingId()) return;
    const confirmed = confirm(
      `Excluir "${product.name}" definitivamente? Essa ação não pode ser desfeita. Se preferir só esconder da loja, use o botão de ativar/inativar.`,
    );
    if (!confirmed) return;

    this.deletingId.set(product.id);
    this.actionError.set(null);
    this.productService.delete(product.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.selectedIds.update((ids) => ids.filter((id) => id !== product.id));
        this.load();
      },
      error: (err) => {
        this.deletingId.set(null);
        this.actionError.set(err?.error?.detail ?? `Não foi possível excluir "${product.name}".`);
      },
    });
  }

  toggleSelected(productId: string, checked: boolean): void {
    const current = this.selectedIds();
    this.selectedIds.set(checked ? [...current, productId] : current.filter((id) => id !== productId));
  }

  toggleSelectAllOnPage(checked: boolean): void {
    const pageIds = this.products().map((p) => p.id);
    this.selectedIds.update((current) =>
      checked ? [...new Set([...current, ...pageIds])] : current.filter((id) => !pageIds.includes(id)),
    );
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

  private updateQuery(changes: Record<string, string | number | null>): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: changes, queryParamsHandling: 'merge', replaceUrl: true });
  }
}
