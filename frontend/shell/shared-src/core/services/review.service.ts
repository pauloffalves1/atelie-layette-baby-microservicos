import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { CreateReviewRequest, ProductReview, ReviewEligibility } from '../models/review.model';

@Injectable({ providedIn: 'root' })
export class ReviewService {
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
}
