import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@shared/environment';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  const emptyPage = { items: [], page: 1, pageSize: 12, totalItems: 0, totalPages: 0 };

  it('list() requests the public products endpoint without a category filter, defaulting to page 1 / pageSize 12', () => {
    service.list().subscribe();

    const req = httpMock.expectOne((r) => r.url === `${environment.apiUrl}/products`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.has('category')).toBe(false);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('12');
    req.flush(emptyPage);
  });

  it('list(category, page) sends category and page as query params', () => {
    service.list('Toalhas', 2).subscribe();

    const req = httpMock.expectOne((r) => r.url === `${environment.apiUrl}/products`);
    expect(req.request.params.get('category')).toBe('Toalhas');
    expect(req.request.params.get('page')).toBe('2');
    req.flush(emptyPage);
  });

  it('getBySlug() requests the product by slug', () => {
    service.getBySlug('body-manga-longa-nuvem').subscribe();

    httpMock.expectOne(`${environment.apiUrl}/products/body-manga-longa-nuvem`).flush({});
  });

  it('listAllForAdmin() hits the admin endpoint with page/pageSize params', () => {
    service.listAllForAdmin().subscribe();

    const req = httpMock.expectOne((r) => r.url === `${environment.apiUrl}/admin/products`);
    expect(req.request.params.get('pageSize')).toBe('20');
    req.flush({ ...emptyPage, pageSize: 20 });
  });
});
