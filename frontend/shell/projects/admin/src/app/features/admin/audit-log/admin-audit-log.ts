import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditLogEntry, AuditLogFilterOptions } from '@shared/core/models/audit-log.model';
import { AuditLogService } from '@shared/core/services/audit-log.service';
import { Pagination } from '@shared/shared/components/pagination/pagination';
import { LoadError } from '@shared/shared/components/load-error/load-error';

const ACTION_LABELS: Record<string, string> = {
  ProductCreated: 'Produto criado',
  ProductUpdated: 'Produto atualizado',
  ProductDeleted: 'Produto excluído',
  ProductActiveChanged: 'Produto ativado/desativado',
  ProductPromotionChanged: 'Promoção alterada',
  ProductTestChanged: 'Produto de teste marcado/desmarcado',
  OrderCreatedManually: 'Encomenda registrada no painel',
  OrderStatusChanged: 'Status do pedido alterado',
  OrderTrackingCodeSet: 'Código de rastreio definido',
  OrderRemoved: 'Pedido removido',
  TestOrderRemoved: 'Pedido de teste removido',
  CouponCreated: 'Cupom criado',
  CouponActiveChanged: 'Cupom ativado/desativado',
  CustomerUpdated: 'Cliente editado',
  CustomerRemoved: 'Cliente removido',
  CustomerEmailVerified: 'E-mail de cliente verificado',
  ReviewApproved: 'Avaliação aprovada',
  ReviewRejected: 'Avaliação rejeitada',
  AdminLogin: 'Login administrativo',
  AdminCreated: 'Administradora cadastrada',
  AdminRemoved: 'Administradora removida',
  AdminPermissionsUpdated: 'Permissões alteradas',
  AdminPasswordChanged: 'Senha alterada',
  AdminTwoFactorEnabled: '2FA ativado',
  AdminTwoFactorDisabled: '2FA desativado',
};

@Component({
  selector: 'app-admin-audit-log',
  standalone: true,
  imports: [DatePipe, FormsModule, Pagination, LoadError],
  templateUrl: './admin-audit-log.html',
})
export class AdminAuditLog implements OnInit {
  readonly entries = signal<AuditLogEntry[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalItems = signal(0);

  /** Answering "who changed this product's price?" used to mean paging through every login of every admin. */
  readonly options = signal<AuditLogFilterOptions>({ admins: [], actions: [] });
  readonly admin = signal('');
  readonly action = signal('');
  readonly from = signal('');
  readonly to = signal('');
  readonly search = signal('');
  readonly hasFilters = computed(() => !!(this.admin() || this.action() || this.from() || this.to() || this.search().trim()));

  private searchTimeout: ReturnType<typeof setTimeout> | undefined;

  constructor(private readonly auditLogService: AuditLogService) {}

  ngOnInit(): void {
    this.auditLogService.filterOptions().subscribe({ next: (options) => this.options.set(options), error: () => {} });
    this.load();
  }

  actionLabel(action: string): string {
    return ACTION_LABELS[action] ?? action;
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  onSearchInput(value: string): void {
    this.search.set(value);
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => this.applyFilters(), 400);
  }

  clearFilters(): void {
    this.admin.set('');
    this.action.set('');
    this.from.set('');
    this.to.set('');
    this.search.set('');
    this.applyFilters();
  }

  goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.auditLogService
      .list(this.page(), { admin: this.admin(), action: this.action(), from: this.from(), to: this.to(), search: this.search().trim() })
      .subscribe({
        next: (result) => {
          this.entries.set(result.items);
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
