import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { WishlistItem, WishlistStatus } from '../models/wishlist.model';

@Injectable({ providedIn: 'root' })
export class WishlistService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<WishlistItem[]> {
    return this.http.get<WishlistItem[]>(`${environment.apiUrl}/wishlist`);
  }

  getStatus(productId: string): Observable<WishlistStatus> {
    return this.http.get<WishlistStatus>(`${environment.apiUrl}/wishlist/${productId}`);
  }

  add(productId: string): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/wishlist/${productId}`, {});
  }

  remove(productId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/wishlist/${productId}`);
  }
}
