import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, Injector, OnInit, afterNextRender, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, NavigationStart, Router, RouterLink } from '@angular/router';
import { filter } from 'rxjs';
import { Product } from '@shared/core/models/product.model';
import { AuthService } from '@shared/core/services/auth.service';
import { ProductService } from '@shared/core/services/product.service';
import { WishlistService } from '@shared/core/services/wishlist.service';
import { SeoService } from '@shared/core/services/seo.service';
import { LoadError } from '@shared/shared/components/load-error/load-error';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

const SEARCH_DEBOUNCE_MS = 400;
const SCROLL_KEY_PREFIX = 'atelie-bebe.shop-scroll:';

@Component({
  selector: 'app-shop',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, Pagination, AssetUrlPipe, FormsModule, LoadError],
  templateUrl: './shop.html',
})
export class Shop implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly auth = inject(AuthService);
  private readonly wishlist = inject(WishlistService);

  /** Favorites straight from the grid — before, a product had to be opened to be saved. */
  readonly favoriteIds = signal<ReadonlySet<string>>(new Set());
  readonly favoriteBusyId = signal<string | null>(null);
  readonly favoriteError = signal<string | null>(null);

  readonly products = signal<Product[]>([]);
  readonly categories = signal<string[]>([]);
  readonly activeCategory = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalItems = signal(0);
  readonly loading = signal(true);
  /** The listing (or smart search) request failed — shown as an error with a retry instead of the
   * "Nenhum produto encontrado" empty state, which read as "the shop has nothing". */
  readonly loadError = signal(false);
  readonly smartSearchEnabled = signal(false);

  /** The query whose smart-search results are on screen — goToPage() re-runs it instead of the
   * regular listing, and the template labels the results as coming from it. */
  readonly activeSemanticQuery = signal<string | null>(null);

  private searchTimeout: ReturnType<typeof setTimeout> | undefined;

  /** Every listing/search request gets a number; a response is ignored unless it's still the
   * latest one. Without this a slow simple search could land after a smart search and replace
   * its results (or vice versa). */
  private requestSeq = 0;

  /** Arrived here with the browser's back/forward button (typically back from a product page) —
   * the first listing that loads puts the visitor back where they were in it, instead of at the
   * top of page 1's hero. Router's own restoration can't: it runs before the products arrive.
   * Read here, while the component is being created: by ngOnInit the navigation has already ended. */
  private restoreScrollPending = inject(Router).currentNavigation()?.trigger === 'popstate';

  constructor(
    private readonly productService: ProductService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly seo: SeoService,
  ) {}

  ngOnInit(): void {
    // NavigationStart still has the listing's URL in router.url — remember how far down it was.
    this.router.events
      .pipe(filter((event) => event instanceof NavigationStart), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        try {
          sessionStorage.setItem(SCROLL_KEY_PREFIX + this.router.url, String(Math.round(window.scrollY)));
        } catch {
          // Storage blocked (private mode etc.) — back just lands at the top, as before.
        }
      });

    this.productService.listCategories().subscribe((categories) => this.categories.set(categories));

    if (this.auth.isAuthenticated()) {
      this.wishlist.list().subscribe({
        next: (items) => this.favoriteIds.set(new Set(items.map((item) => item.product.id))),
        error: () => {},
      });
    }

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const category = params.get('categoria');
      const search = params.get('busca') ?? '';
      const page = Number(params.get('pagina')) || 1;
      this.activeCategory.set(category);
      this.updateSeo(category);
      this.searchTerm.set(search);
      this.page.set(page);
      this.activeSemanticQuery.set(null);
      this.load(category, page, search);
    });
  }

  /** A category URL is its own indexable page ("Fralda de Boca" search intent) with its own title and canonical. */
  private updateSeo(category: string | null): void {
    // After render: the router's TitleStrategy re-applies the route's static title ("Loja") when the
    // navigation ends, which comes after this query-param emission and would overwrite ours.
    afterNextRender(() => this.applySeo(category), { injector: this.injector });
  }

  private applySeo(category: string | null): void {
    if (category) {
      this.seo.update({
        title: `${category} com bordado personalizado`,
        description: `${category} em algodão com bordado personalizado do nome do bebê — feita à mão sob encomenda pelo Ateliê Layette Baby, com entrega para todo o Brasil.`,
        path: `/loja?categoria=${encodeURIComponent(category)}`,
      });
      this.seo.setBreadcrumbs([
        { name: 'Início', path: '/' },
        { name: 'Loja', path: '/loja' },
        { name: category, path: `/loja?categoria=${encodeURIComponent(category)}` },
      ]);
      return;
    }
    this.seo.update({
      title: 'Loja — fraldas de ombro e boca bordadas',
      description: 'Fraldas de ombro e boca prontas para comprar, com opção de bordado personalizado — Kit Ombro e Boca, Fralda de Ombro e Fralda de Boca.',
      path: '/loja',
    });
  }

  isFavorite(productId: string): boolean {
    return this.favoriteIds().has(productId);
  }

  toggleFavorite(product: Product): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/entrar'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    if (this.favoriteBusyId()) return;

    const wasFavorite = this.isFavorite(product.id);
    const apply = (favorite: boolean) =>
      this.favoriteIds.update((ids) => {
        const next = new Set(ids);
        if (favorite) next.add(product.id);
        else next.delete(product.id);
        return next;
      });

    apply(!wasFavorite); // optimistic: the heart fills on tap, reverted if the request fails
    this.favoriteBusyId.set(product.id);
    this.favoriteError.set(null);
    (wasFavorite ? this.wishlist.remove(product.id) : this.wishlist.add(product.id)).subscribe({
      next: () => this.favoriteBusyId.set(null),
      error: () => {
        apply(wasFavorite);
        this.favoriteBusyId.set(null);
        this.favoriteError.set(`Não foi possível ${wasFavorite ? 'remover' : 'salvar'} "${product.name}" nos favoritos. Tente de novo.`);
      },
    });
  }

  /** Plain clicks filter in place; Ctrl/Cmd/middle-click keep the browser's "open in new tab". */
  onCategoryClick(event: MouseEvent, category: string | null): void {
    if (event.ctrlKey || event.metaKey || event.shiftKey || event.button !== 0) return;
    event.preventDefault();
    this.selectCategory(category);
  }

  /** Real hrefs so crawlers can follow each category; clicks still go through selectCategory(). */
  categoryHref(category: string | null): string {
    return category ? `/loja?categoria=${encodeURIComponent(category)}` : '/loja';
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
    this.router.navigate([], { queryParams: {}, scroll: 'manual' });
    // Same URL (already unfiltered) wouldn't re-emit queryParamMap — reload explicitly.
    if (!this.activeCategory() && !this.route.snapshot.queryParamMap.get('busca')) this.load(null, 1, '');
  }

  retry(): void {
    const semanticQuery = this.activeSemanticQuery();
    if (semanticQuery) this.runSemanticSearch(semanticQuery, this.page());
    else this.load(this.activeCategory(), this.page(), this.searchTerm().trim());
  }

  goToPage(page: number): void {
    // The current cards stay on screen (dimmed) while the next page loads, so this lands on the top
    // of the grid rather than all the way up at the page title.
    document.getElementById('shop-results')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
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
    // 'manual': filtering or typing a search must not yank the page back to the top on every
    // keystroke/chip click (the router's scroll-to-top is meant for moving between pages).
    this.router.navigate([], { queryParams, scroll: 'manual' });
  }

  private cancelPendingSearch(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = undefined;
  }

  private load(category: string | null, page: number, search: string): void {
    const seq = ++this.requestSeq;
    this.loading.set(true);
    this.loadError.set(false);
    this.productService.list(category ?? undefined, page, 12, search || undefined).subscribe({
      next: (result) => {
        if (seq !== this.requestSeq) return;
        this.products.set(result.items);
        this.totalPages.set(result.totalPages);
        this.totalItems.set(result.totalItems);
        this.loading.set(false);
        this.restoreScrollIfReturning();
      },
      error: () => this.fail(seq),
    });
  }

  private runSemanticSearch(query: string, page: number): void {
    const seq = ++this.requestSeq;
    this.activeSemanticQuery.set(query);
    this.loading.set(true);
    this.loadError.set(false);
    this.productService.semanticSearch(query, page, 12).subscribe({
      next: (result) => {
        if (seq !== this.requestSeq) return;
        this.products.set(result.items);
        this.totalPages.set(result.totalPages);
        this.totalItems.set(result.totalItems);
        this.page.set(page);
        this.loading.set(false);
      },
      error: () => this.fail(seq),
    });
  }

  private fail(seq: number): void {
    if (seq !== this.requestSeq) return;
    this.loading.set(false);
    this.loadError.set(true);
  }

  private restoreScrollIfReturning(): void {
    if (!this.restoreScrollPending) return;
    this.restoreScrollPending = false;
    let top = 0;
    try {
      top = Number(sessionStorage.getItem(SCROLL_KEY_PREFIX + this.router.url)) || 0;
    } catch {
      return;
    }
    if (top > 0) afterNextRender(() => window.scrollTo({ top }), { injector: this.injector });
  }
}
