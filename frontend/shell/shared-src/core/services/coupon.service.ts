import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { Coupon, CreateCouponRequest, ValidateCouponResponse } from '../models/coupon.model';

@Injectable({ providedIn: 'root' })
export class CouponService {
  constructor(private readonly http: HttpClient) {}

  validate(code: string, subtotal: number): Observable<ValidateCouponResponse> {
    return this.http.post<ValidateCouponResponse>(`${environment.apiUrl}/coupons/validate`, { code, subtotal });
  }

  // ---- admin ----

  list(): Observable<Coupon[]> {
    return this.http.get<Coupon[]>(`${environment.apiUrl}/admin/coupons`);
  }

  create(request: CreateCouponRequest): Observable<Coupon> {
    return this.http.post<Coupon>(`${environment.apiUrl}/admin/coupons`, request);
  }

  setActive(id: string, active: boolean): Observable<Coupon> {
    return this.http.patch<Coupon>(`${environment.apiUrl}/admin/coupons/${id}/active?active=${active}`, {});
  }
}
