import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { PagedResult } from '../models/pagination.model';
import { AdminProductReview, CreateReviewRequest, ProductReview, ReviewEligibility } from '../models/review.model';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly adminUrl = `${environment.apiUrl}/admin/reviews`;

  constructor(private readonly http: HttpClient) {}

  listByProduct(productId: string): Observable<ProductReview[]> {
    return this.http.get<ProductReview[]>(`${environment.apiUrl}/products/${productId}/reviews`);
  }

  getEligibility(productId: string): Observable<ReviewEligibility> {
    return this.http.get<ReviewEligibility>(`${environment.apiUrl}/products/${productId}/reviews/eligibility`);
  }

  create(productId: string, request: CreateReviewRequest): Observable<ProductReview> {
    return this.http.post<ProductReview>(`${environment.apiUrl}/products/${productId}/reviews`, request);
  }

  uploadPhoto(productId: string, file: File): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${environment.apiUrl}/products/${productId}/reviews/photo`, formData);
  }

  listForAdmin(approved: boolean | null, page: number, pageSize = 20): Observable<PagedResult<AdminProductReview>> {
    const approvedParam = approved === null ? '' : `&approved=${approved}`;
    return this.http.get<PagedResult<AdminProductReview>>(`${this.adminUrl}?page=${page}&pageSize=${pageSize}${approvedParam}`);
  }

  approve(id: string): Observable<AdminProductReview> {
    return this.http.patch<AdminProductReview>(`${this.adminUrl}/${id}/approve`, {});
  }

  reject(id: string): Observable<void> {
    return this.http.delete<void>(`${this.adminUrl}/${id}`);
  }
}
