import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@shared/environment';
import { CreateCustomOrderRequest, CreateManualOrderRequest, CreateStoreOrderRequest, Order } from '../models/order.model';
import { PagedResult } from '../models/pagination.model';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly baseUrl = `${environment.apiUrl}/orders`;
  private readonly adminUrl = `${environment.apiUrl}/admin/orders`;
  private readonly testAdminUrl = `${environment.apiUrl}/admin/test-orders`;

  constructor(private readonly http: HttpClient) {}

  /** Admin: registers an order closed on WhatsApp or in person. */
  createManual(request: CreateManualOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/admin/orders`, request);
  }

  createStoreOrder(request: CreateStoreOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${this.baseUrl}/store`, request);
  }

  createCustomOrder(request: CreateCustomOrderRequest): Observable<Order> {
    return this.http.post<Order>(`${this.baseUrl}/custom`, request);
  }

  getById(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/${id}`);
  }

  lookup(orderNumber: string, email: string): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/lookup`, { params: { orderNumber, email } });
  }

  listMine(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.baseUrl}/mine`);
  }

  cancel(id: string): Observable<Order> {
    return this.http.post<Order>(`${this.baseUrl}/${id}/cancel`, {});
  }

  // ---- admin ----

  /** `search` matches customer name/e-mail/phone or the short order number (with or without "#"). */
  listAllForAdmin(status?: string, paymentStatus?: string, page = 1, pageSize = 20, search?: string): Observable<PagedResult<Order>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (status) params['status'] = status;
    if (paymentStatus) params['paymentStatus'] = paymentStatus;
    if (search) params['search'] = search;
    return this.http.get<PagedResult<Order>>(this.adminUrl, { params });
  }

  changeStatus(id: string, status: string): Observable<Order> {
    return this.http.patch<Order>(`${this.adminUrl}/${id}/status`, { status });
  }

  setTrackingCode(id: string, trackingCode: string | null): Observable<Order> {
    return this.http.patch<Order>(`${this.adminUrl}/${id}/tracking-code`, { trackingCode });
  }

  generatePixCharge(orderId: string): Observable<{ pixQrCodeText: string }> {
    return this.http.post<{ pixQrCodeText: string }>(`${this.adminUrl}/${orderId}/payment-link`, {});
  }

  /** Permanent deletion — backend restricts this to AdminManagement regardless of what the caller sends. */
  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${this.adminUrl}/${id}`);
  }

  /** Test screen (RF40) — the test purchases every other admin listing leaves out. Requires the Testing permission. */
  listTest(page = 1, pageSize = 20): Observable<PagedResult<Order>> {
    return this.http.get<PagedResult<Order>>(this.testAdminUrl, {
      params: { page: String(page), pageSize: String(pageSize) },
    });
  }

  /** Cleanup from the test screen — the backend refuses anything that isn't a test order. */
  removeTest(id: string): Observable<void> {
    return this.http.delete<void>(`${this.testAdminUrl}/${id}`);
  }

  /** True while PagBank still only has a sandbox token — checkout shows an "em construção" notice instead of a real payment form while this is true. */
  getPaymentStatus(): Observable<{ sandbox: boolean }> {
    return this.http.get<{ sandbox: boolean }>(`${environment.apiUrl}/payments/pagbank/status`);
  }

  getCardEncryptionPublicKey(): Observable<{ publicKey: string }> {
    return this.http.get<{ publicKey: string }>(`${environment.apiUrl}/payments/pagbank/public-key`);
  }

  getThreeDsSession(): Observable<{ session: string; environment: 'SANDBOX' | 'PROD' }> {
    return this.http.get<{ session: string; environment: 'SANDBOX' | 'PROD' }>(`${environment.apiUrl}/payments/pagbank/3ds-session`);
  }

  exportCsv(status?: string, paymentStatus?: string, search?: string): Observable<Blob> {
    const params: Record<string, string> = {};
    if (status) params['status'] = status;
    if (paymentStatus) params['paymentStatus'] = paymentStatus;
    if (search) params['search'] = search;
    return this.http.get(`${this.adminUrl}/export`, { params, responseType: 'blob' });
  }

  // ---- fake payment (dev-only, see FakePaymentGateway) ----

  simulatePayment(orderId: string, approved: boolean): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/payments/pagbank/simulate/${orderId}`, { approved });
  }
}
