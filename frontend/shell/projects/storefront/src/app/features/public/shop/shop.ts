import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Product } from '@shared/core/models/product.model';
import { ProductService } from '@shared/core/services/product.service';
import { SeoService } from '@shared/core/services/seo.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { AssetUrlPipe } from '@shared/shared/pipes/asset-url.pipe';

@Component({
  selector: 'app-shop',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, Pagination, AssetUrlPipe, FormsModule],
  templateUrl: './shop.html',
})
export class Shop implements OnInit {
  readonly products = signal<Product[]>([]);
  readonly categories = signal<string[]>([]);
  readonly activeCategory = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly loading = signal(true);
  readonly smartSearchEnabled = signal(false);

  // Set only while showing results from a semantic-search call, so goToPage() knows to re-run
  // that search instead of the regular category/name listing.
  private activeSemanticQuery: string | null = null;

  private readonly searchInput$ = new Subject<string>();

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

    this.route.queryParamMap.subscribe((params) => {
      const category = params.get('categoria');
      const search = params.get('busca') ?? '';
      const page = Number(params.get('pagina')) || 1;
      this.activeCategory.set(category);
      this.searchTerm.set(search);
      this.page.set(page);
      this.load(category, page, search);
    });

    // Debounced so we don't hit the API on every keystroke; changing the search term resets to
    // page 1 the same way changing category does — by simply omitting "pagina" from navigation.
    this.searchInput$.pipe(debounceTime(400), distinctUntilChanged()).subscribe((term) => {
      const queryParams: Record<string, string> = {};
      if (this.activeCategory()) queryParams['categoria'] = this.activeCategory()!;
      if (term) queryParams['busca'] = term;
      this.router.navigate([], { queryParams });
    });
  }

  onSearchInput(value: string): void {
    this.activeSemanticQuery = null;
    this.searchInput$.next(value);
  }

  // Busca inteligente é disparada só ao confirmar (Enter), nunca a cada tecla — cada chamada
  // custa uma requisição à API da Claude, diferente da busca simples (grátis, por substring).
  onSearchSubmit(): void {
    if (!this.smartSearchEnabled() || !this.searchTerm().trim()) return;
    this.runSemanticSearch(this.searchTerm(), 1);
  }

  selectCategory(category: string | null): void {
    this.activeSemanticQuery = null;
    const queryParams: Record<string, string> = {};
    if (category) queryParams['categoria'] = category;
    if (this.searchTerm()) queryParams['busca'] = this.searchTerm();
    this.router.navigate([], { queryParams });
  }

  goToPage(page: number): void {
    if (this.activeSemanticQuery) {
      this.runSemanticSearch(this.activeSemanticQuery, page);
      return;
    }

    const queryParams: Record<string, string | number> = { pagina: page };
    if (this.activeCategory()) queryParams['categoria'] = this.activeCategory()!;
    if (this.searchTerm()) queryParams['busca'] = this.searchTerm();
    this.router.navigate([], { queryParams });
  }

  private load(category: string | null, page: number, search: string): void {
    this.loading.set(true);
    this.productService.list(category ?? undefined, page, 12, search || undefined).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private runSemanticSearch(query: string, page: number): void {
    this.activeSemanticQuery = query;
    this.loading.set(true);
    this.productService.semanticSearch(query, page, 12).subscribe({
      next: (result) => {
        this.products.set(result.items);
        this.totalPages.set(result.totalPages);
        this.page.set(page);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
