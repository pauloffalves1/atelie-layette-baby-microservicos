import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Product } from '@shared/core/models/product.model';
import { ProductService } from '@shared/core/services/product.service';
import { SeoService } from '@shared/core/services/seo.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

const SEARCH_DEBOUNCE_MS = 400;

@Component({
  selector: 'app-shop',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, Pagination, AssetUrlPipe, FormsModule],
  templateUrl: './shop.html',
})
export class Shop implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  readonly products = signal<Product[]>([]);
  readonly categories = signal<string[]>([]);
  readonly activeCategory = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalItems = signal(0);
  readonly loading = signal(true);
  readonly smartSearchEnabled = signal(false);

  /** The query whose smart-search results are on screen — goToPage() re-runs it instead of the
   * regular listing, and the template labels the results as coming from it. */
  readonly activeSemanticQuery = signal<string | null>(null);

  private searchTimeout: ReturnType<typeof setTimeout> | undefined;

  /** Every listing/search request gets a number; a response is ignored unless it's still the
   * latest one. Without this a slow simple search could land after a smart search and replace
   * its results (or vice versa). */
  private requestSeq = 0;

  constructor(
    private readonly productService: ProductService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly seo: SeoService,
  ) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Loja',
      description: 'Fraldas de ombro e boca prontas para comprar, com opção de bordado personalizado — Kit Ombro e Boca, Fralda de Ombro e Fralda de Boca.',
      path: '/loja',
    });

    this.productService.listCategories().subscribe((categories) => this.categories.set(categories));

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const category = params.get('categoria');
      const search = params.get('busca') ?? '';
      const page = Number(params.get('pagina')) || 1;
      this.activeCategory.set(category);
      this.searchTerm.set(search);
      this.page.set(page);
      this.activeSemanticQuery.set(null);
      this.load(category, page, search);
    });
  }

  onSearchInput(value: string): void {
    this.searchTerm.set(value);
    this.cancelPendingSearch();

    // Smart search costs an AI call per query, so it only runs on Enter — and while it's on, typing
    // a sentence must not also fire the plain name search for that sentence in the background
    // (which used to flash "Nenhum produto encontrado" mid-typing).
    if (this.smartSearchEnabled()) return;

    this.searchTimeout = setTimeout(() => this.navigate({ busca: value.trim() || null }), SEARCH_DEBOUNCE_MS);
  }

  onSearchSubmit(): void {
    const term = this.searchTerm().trim();
    this.cancelPendingSearch();

    if (this.smartSearchEnabled()) {
      if (term) this.runSemanticSearch(term, 1);
      return;
    }
    this.navigate({ busca: term || null });
  }

  onSmartSearchToggle(enabled: boolean): void {
    this.smartSearchEnabled.set(enabled);
    this.cancelPendingSearch();
    // Turning it off with a sentence still typed goes back to the plain listing for that text.
    if (!enabled && this.activeSemanticQuery()) {
      this.activeSemanticQuery.set(null);
      this.load(this.activeCategory(), 1, this.searchTerm());
    }
  }

  selectCategory(category: string | null): void {
    this.cancelPendingSearch();
    this.navigate({ categoria: category, busca: this.searchTerm().trim() || null });
  }

  clearFilters(): void {
    this.cancelPendingSearch();
    this.searchTerm.set('');
    this.router.navigate([], { queryParams: {} });
    // Same URL (already unfiltered) wouldn't re-emit queryParamMap — reload explicitly.
    if (!this.activeCategory() && !this.route.snapshot.queryParamMap.get('busca')) this.load(null, 1, '');
  }

  goToPage(page: number): void {
    const semanticQuery = this.activeSemanticQuery();
    if (semanticQuery) {
      this.runSemanticSearch(semanticQuery, page);
      return;
    }
    this.navigate({ categoria: this.activeCategory(), busca: this.searchTerm().trim() || null, pagina: page > 1 ? page : null });
  }

  private navigate(params: { categoria?: string | null; busca?: string | null; pagina?: number | null }): void {
    const queryParams: Record<string, string | number> = {};
    const category = params.categoria !== undefined ? params.categoria : this.activeCategory();
    if (category) queryParams['categoria'] = category;
    if (params.busca) queryParams['busca'] = params.busca;
    if (params.pagina) queryParams['pagina'] = params.pagina;
    this.router.navigate([], { queryParams });
  }

  private cancelPendingSearch(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = undefined;
  }

  private load(category: string | null, page: number, search: string): void {
    const seq = ++this.requestSeq;
    this.loading.set(true);
    this.productService.list(category ?? undefined, page, 12, search || undefined).subscribe({
      next: (result) => {
        if (seq !== this.requestSeq) return;
        this.products.set(result.items);
        this.totalPages.set(result.totalPages);
        this.totalItems.set(result.totalItems);
        this.loading.set(false);
      },
      error: () => {
        if (seq === this.requestSeq) this.loading.set(false);
      },
    });
  }

  private runSemanticSearch(query: string, page: number): void {
    const seq = ++this.requestSeq;
    this.activeSemanticQuery.set(query);
    this.loading.set(true);
    this.productService.semanticSearch(query, page, 12).subscribe({
      next: (result) => {
        if (seq !== this.requestSeq) return;
        this.products.set(result.items);
        this.totalPages.set(result.totalPages);
        this.totalItems.set(result.totalItems);
        this.page.set(page);
        this.loading.set(false);
      },
      error: () => {
        if (seq === this.requestSeq) this.loading.set(false);
      },
    });
  }
}
