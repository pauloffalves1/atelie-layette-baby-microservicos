import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  Order,
  ORDER_STATUS_LABELS,
  OrderStatus,
  PAYMENT_STATUS_LABELS,
  PaymentStatus,
} from '@shared/core/models/order.model';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';
import { OrderService } from '@shared/core/services/order.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { LoadError } from '@shared/shared/components/load-error/load-error';

const STATUSES: OrderStatus[] = ['Recebido', 'EmProducao', 'Pronto', 'Enviado', 'Entregue', 'Cancelado'];
const PAYMENT_STATUSES: PaymentStatus[] = ['Pendente', 'Pago', 'Recusado'];
const SEARCH_DEBOUNCE_MS = 350;

/**
 * Filters, search and page live in the URL (?status=&pagamento=&busca=&pagina=) rather than only in
 * component state, so opening an order and coming back — or reloading, or sharing the link —
 * lands on the same filtered page instead of resetting to "Todas", page 1.
 */
@Component({
  selector: 'app-admin-order-list',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink, Pagination, LoadError],
  templateUrl: './admin-order-list.html',
})
export class AdminOrderList {
  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly activeStatus = signal<OrderStatus | null>(null);
  readonly activePaymentStatus = signal<PaymentStatus | null>(null);
  readonly search = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalItems = signal(0);
  readonly statusLabels = ORDER_STATUS_LABELS;
  readonly paymentStatusLabels = PAYMENT_STATUS_LABELS;
  readonly statuses = STATUSES;
  readonly paymentStatuses = PAYMENT_STATUSES;
  readonly exporting = signal(false);
  readonly removingId = signal<string | null>(null);

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly orderService = inject(OrderService);
  readonly auth = inject(AdminAuthService);
  private searchTimeout: ReturnType<typeof setTimeout> | undefined;

  constructor() {
    this.route.queryParamMap.pipe(takeUntilDestroyed(inject(DestroyRef))).subscribe((params) => {
      const status = params.get('status') as OrderStatus | null;
      const paymentStatus = params.get('pagamento') as PaymentStatus | null;
      this.activeStatus.set(status && STATUSES.includes(status) ? status : null);
      this.activePaymentStatus.set(paymentStatus && PAYMENT_STATUSES.includes(paymentStatus) ? paymentStatus : null);
      this.search.set(params.get('busca') ?? '');
      this.page.set(Math.max(1, Number(params.get('pagina')) || 1));
      this.load();
    });
  }

  filterByStatus(status: OrderStatus | null): void {
    this.updateQuery({ status, pagina: null });
  }

  filterByPaymentStatus(paymentStatus: PaymentStatus | null): void {
    this.updateQuery({ pagamento: paymentStatus, pagina: null });
  }

  onSearchInput(value: string): void {
    this.search.set(value);
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => this.updateQuery({ busca: value.trim() || null, pagina: null }), SEARCH_DEBOUNCE_MS);
  }

  clearSearch(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.updateQuery({ busca: null, pagina: null });
  }

  goToPage(page: number): void {
    this.updateQuery({ pagina: page > 1 ? page : null });
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.orderService
      .exportCsv(this.activeStatus() ?? undefined, this.activePaymentStatus() ?? undefined, this.search().trim() || undefined)
      .subscribe({
        next: (blob) => {
          this.exporting.set(false);
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `encomendas-${new Date().toISOString().slice(0, 10)}.csv`;
          link.click();
          URL.revokeObjectURL(url);
        },
        error: () => this.exporting.set(false),
      });
  }

  remove(order: Order): void {
    if (this.removingId()) return;
    const confirmed = confirm(`Excluir permanentemente o pedido #${order.id.slice(0, 8)}? Essa ação não pode ser desfeita.`);
    if (!confirmed) return;

    this.removingId.set(order.id);
    this.orderService.remove(order.id).subscribe({
      next: () => {
        this.removingId.set(null);
        this.load();
      },
      error: () => this.removingId.set(null),
    });
  }

  private updateQuery(changes: Record<string, string | number | null>): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: changes, queryParamsHandling: 'merge', replaceUrl: true });
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.orderService
      .listAllForAdmin(
        this.activeStatus() ?? undefined,
        this.activePaymentStatus() ?? undefined,
        this.page(),
        20,
        this.search().trim() || undefined,
      )
      .subscribe({
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
}
