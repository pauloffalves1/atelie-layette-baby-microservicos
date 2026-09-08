import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { PagedResult } from '../models/pagination.model';
import {
  AdminProduct,
  BulkApplyPromotionRequest,
  CreateProductRequest,
  Product,
  SetPromotionRequest,
  UpdateProductRequest,
} from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly baseUrl = `${environment.apiUrl}/products`;
  private readonly adminUrl = `${environment.apiUrl}/admin/products`;

  constructor(private readonly http: HttpClient) {}

  list(category?: string, page = 1, pageSize = 12, search?: string): Observable<PagedResult<Product>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (category) params['category'] = category;
    if (search) params['search'] = search;
    return this.http.get<PagedResult<Product>>(this.baseUrl, { params });
  }

  listFeatured(): Observable<Product[]> {
    return this.http.get<Product[]>(`${this.baseUrl}/featured`);
  }

  listCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.baseUrl}/categories`);
  }

  getBySlug(slug: string): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${slug}`);
  }

  // ---- admin ----

  listAllForAdmin(page = 1, pageSize = 20): Observable<PagedResult<Product>> {
    return this.http.get<PagedResult<Product>>(this.adminUrl, { params: { page, pageSize } });
  }

  getById(id: string): Observable<AdminProduct> {
    return this.http.get<AdminProduct>(`${this.adminUrl}/${id}`);
  }

  setAllowedCustomers(id: string, customerIds: string[]): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.adminUrl}/${id}/customers`, { customerIds });
  }

  setImages(id: string, imageUrls: string[]): Observable<AdminProduct> {
    return this.http.put<AdminProduct>(`${this.adminUrl}/${id}/images`, { imageUrls });
  }

  setPromotion(id: string, request: SetPromotionRequest): Observable<AdminProduct> {
    return this.http.patch<AdminProduct>(`${this.adminUrl}/${id}/promotion`, request);
  }

  applyPromotionToMany(request: BulkApplyPromotionRequest): Observable<AdminProduct[]> {
    return this.http.post<AdminProduct[]>(`${this.adminUrl}/promotions/bulk`, request);
  }

  create(request: CreateProductRequest): Observable<Product> {
    return this.http.post<Product>(this.adminUrl, request);
  }

  update(id: string, request: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.adminUrl}/${id}`, request);
  }

  setActive(id: string, active: boolean): Observable<Product> {
    return this.http.patch<Product>(`${this.adminUrl}/${id}/active?active=${active}`, {});
  }

  uploadImage(file: File): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${this.adminUrl}/uploads`, formData);
  }
}
