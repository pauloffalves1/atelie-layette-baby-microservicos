import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  Order,
  ORDER_STATUS_LABELS,
  OrderStatus,
  PAYMENT_STATUS_LABELS,
  PaymentStatus,
} from '@shared/core/models/order.model';
import { OrderService } from '@shared/core/services/order.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';

@Component({
  selector: 'app-admin-order-list',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink, Pagination],
  templateUrl: './admin-order-list.html',
})
export class AdminOrderList implements OnInit {
  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly activeStatus = signal<OrderStatus | null>(null);
  readonly activePaymentStatus = signal<PaymentStatus | null>(null);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly statusLabels = ORDER_STATUS_LABELS;
  readonly paymentStatusLabels = PAYMENT_STATUS_LABELS;
  readonly statuses: OrderStatus[] = ['Recebido', 'EmProducao', 'Pronto', 'Enviado', 'Entregue', 'Cancelado'];
  readonly paymentStatuses: PaymentStatus[] = ['Pendente', 'Pago', 'Recusado'];
  readonly exporting = signal(false);

  constructor(private readonly orderService: OrderService) {}

  ngOnInit(): void {
    this.load();
  }

  filterByStatus(status: OrderStatus | null): void {
    this.activeStatus.set(status);
    this.page.set(1);
    this.load();
  }

  filterByPaymentStatus(paymentStatus: PaymentStatus | null): void {
    this.activePaymentStatus.set(paymentStatus);
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  exportCsv(): void {
    this.exporting.set(true);
    this.orderService.exportCsv(this.activeStatus() ?? undefined, this.activePaymentStatus() ?? undefined).subscribe({
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

  private load(): void {
    this.loading.set(true);
    this.orderService
      .listAllForAdmin(this.activeStatus() ?? undefined, this.activePaymentStatus() ?? undefined, this.page())
      .subscribe({
        next: (result) => {
          this.orders.set(result.items);
          this.totalPages.set(result.totalPages);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }
}
