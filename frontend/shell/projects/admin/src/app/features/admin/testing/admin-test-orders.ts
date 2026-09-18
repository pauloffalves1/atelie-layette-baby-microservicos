import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { TestDashboard } from '@shared/core/models/dashboard.model';
import { Order } from '@shared/core/models/order.model';
import { AdminProduct } from '@shared/core/models/product.model';
import { OrderService } from '@shared/core/services/order.service';
import { ProductService } from '@shared/core/services/product.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { LoadError } from '@shared/shared/components/load-error/load-error';

/**
 * The test dashboard (RF40): the ateliê's own figures, products and orders — restricted to what was
 * created for testing. It is the only place test purchases are visible at all; every other listing,
 * export and dashboard in the panel leaves them out on purpose, which is what makes testing the real
 * checkout in production safe.
 */
@Component({
  selector: 'app-admin-test-orders',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, Pagination, LoadError],
  templateUrl: './admin-test-orders.html',
})
export class AdminTestOrders implements OnInit {
  readonly dashboard = signal<TestDashboard | null>(null);
  readonly products = signal<AdminProduct[]>([]);
  readonly orders = signal<Order[]>([]);

  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalItems = signal(0);
  readonly removingId = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly lastUpdated = signal<Date | null>(null);

  /** Nothing has been created for testing yet — the screen explains how to start instead of showing zeros. */
  readonly isEmpty = computed(() => this.products().length === 0 && this.totalItems() === 0);

  readonly paidOrders = computed(() => this.orders().filter((o) => o.paymentStatus === 'Pago').length);

  constructor(
    private readonly orderService: OrderService,
    private readonly productService: ProductService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);

    forkJoin({
      dashboard: this.orderService.getTestDashboard(),
      products: this.productService.listTest(),
      orders: this.orderService.listTest(this.page()),
    }).subscribe({
      next: ({ dashboard, products, orders }) => {
        this.dashboard.set(dashboard);
        this.products.set(products);
        this.orders.set(orders.items);
        this.totalPages.set(orders.totalPages);
        this.totalItems.set(orders.totalItems);
        this.lastUpdated.set(new Date());
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
    // Deliberately no confirm dialog: the backend refuses to delete anything that isn't a test
    // order, so the worst case here is losing a test purchase.
    this.removingId.set(order.id);
    this.errorMessage.set(null);
    this.orderService.removeTest(order.id).subscribe({
      next: () => {
        this.removingId.set(null);
        this.load();
      },
      error: () => {
        this.removingId.set(null);
        this.errorMessage.set('Não foi possível excluir este pedido de teste.');
      },
    });
  }
}
