import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { Order } from '@shared/core/models/order.model';
import { OrderService } from '@shared/core/services/order.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { LoadError } from '@shared/shared/components/load-error/load-error';

/**
 * The only place test purchases (RF40) are visible: orders that contain a test product, which every
 * other listing, export and dashboard figure leaves out. Exists so the real checkout — PagBank card
 * with 3DS, PIX, boleto — can be exercised in production without polluting the ateliê's numbers.
 */
@Component({
  selector: 'app-admin-test-orders',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, Pagination, LoadError],
  templateUrl: './admin-test-orders.html',
})
export class AdminTestOrders implements OnInit {
  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalItems = signal(0);
  readonly removingId = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  constructor(private readonly orderService: OrderService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.orderService.listTest(this.page()).subscribe({
      next: (result) => {
        this.orders.set(result.items);
        this.totalPages.set(result.totalPages);
        this.totalItems.set(result.totalItems);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      },
    });
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  shortId(id: string): string {
    return id.slice(0, 8);
  }

  paymentLabel(order: Order): string {
    if (order.pixQrCodeText) return 'PIX';
    if (order.boletoUrl || order.boletoBarcode) return 'Boleto';
    return order.externalPaymentId ? 'Cartão' : '—';
  }

  remove(order: Order): void {
    // Deliberately no confirm dialog fallback beyond this flag: the backend refuses to delete
    // anything that isn't a test order, so the worst case here is losing a test purchase.
    this.removingId.set(order.id);
    this.errorMessage.set(null);
    this.orderService.removeTest(order.id).subscribe({
      next: () => {
        this.removingId.set(null);
        this.orders.update((orders) => orders.filter((o) => o.id !== order.id));
        this.totalItems.update((total) => Math.max(0, total - 1));
      },
      error: () => {
        this.removingId.set(null);
        this.errorMessage.set('Não foi possível excluir este pedido de teste.');
      },
    });
  }
}
