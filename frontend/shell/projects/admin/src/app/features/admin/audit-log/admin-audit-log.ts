import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { AuditLogEntry } from '@shared/core/models/audit-log.model';
import { AuditLogService } from '@shared/core/services/audit-log.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';

const ACTION_LABELS: Record<string, string> = {
  ProductCreated: 'Produto criado',
  ProductUpdated: 'Produto atualizado',
  ProductActiveChanged: 'Produto ativado/desativado',
  ProductPromotionChanged: 'Promoção alterada',
  OrderStatusChanged: 'Status do pedido alterado',
  OrderTrackingCodeSet: 'Código de rastreio definido',
  CouponCreated: 'Cupom criado',
  CouponActiveChanged: 'Cupom ativado/desativado',
  CustomerUpdated: 'Cliente editado',
  AdminLogin: 'Login administrativo',
  AdminTwoFactorEnabled: '2FA ativado',
  AdminTwoFactorDisabled: '2FA desativado',
};

@Component({
  selector: 'app-admin-audit-log',
  standalone: true,
  imports: [DatePipe, Pagination],
  templateUrl: './admin-audit-log.html',
})
export class AdminAuditLog implements OnInit {
  readonly entries = signal<AuditLogEntry[]>([]);
  readonly loading = signal(true);
  readonly page = signal(1);
  readonly totalPages = signal(0);

  constructor(private readonly auditLogService: AuditLogService) {}

  ngOnInit(): void {
    this.load();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  actionLabel(action: string): string {
    return ACTION_LABELS[action] ?? action;
  }

  private load(): void {
    this.loading.set(true);
    this.auditLogService.list(this.page()).subscribe({
      next: (result) => {
        this.entries.set(result.items);
        this.totalPages.set(result.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
