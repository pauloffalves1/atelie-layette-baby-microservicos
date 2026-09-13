import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Dashboard, SalesByDay } from '@shared/core/models/dashboard.model';
import { ORDER_STATUS_LABELS } from '@shared/core/models/order.model';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';
import { DashboardService } from '@shared/core/services/dashboard.service';
import { ReviewService } from '@shared/core/services/review.service';

interface AttentionItem {
  key: string;
  icon: string;
  label: string;
  count: number;
  detail?: string;
  link: unknown[];
  queryParams?: Record<string, string>;
}

/** Rounds a chart maximum up to a clean tick (R$ 50, 100, 250, 500, 1.000, 2.500...). */
function niceCeiling(value: number): number {
  if (value <= 0) return 100;
  const magnitude = 10 ** Math.floor(Math.log10(value));
  const step = [1, 2, 2.5, 5, 10].find((m) => m * magnitude >= value) ?? 10;
  return step * magnitude;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, DecimalPipe, RouterLink],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard {
  private readonly dashboardService = inject(DashboardService);
  private readonly reviewService = inject(ReviewService);
  readonly auth = inject(AdminAuthService);

  readonly dashboard = signal<Dashboard | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly lastUpdated = signal<Date | null>(null);
  readonly pendingReviews = signal<number | null>(null);
  readonly statusLabels = ORDER_STATUS_LABELS;

  readonly generatingSummary = signal(false);
  readonly narrativeSummary = signal<string | null>(null);
  readonly summaryError = signal<string | null>(null);

  /** Index of the day under the pointer / keyboard focus in the sales chart (drives the tooltip). */
  readonly activeDayIndex = signal<number | null>(null);
  /** Roving tab stop: the chart is one Tab stop, arrow keys move between days. */
  readonly focusDayIndex = signal(29);
  readonly showSalesTable = signal(false);

  readonly canSeeOrders = computed(() => this.auth.hasPermission('Orders'));

  readonly salesDays = computed(() => this.dashboard()?.salesLast30Days ?? []);
  readonly salesMax = computed(() => niceCeiling(Math.max(0, ...this.salesDays().map((d) => d.revenue))));
  readonly salesTotal = computed(() => this.salesDays().reduce((sum, d) => sum + d.revenue, 0));
  readonly salesOrderCount = computed(() => this.salesDays().reduce((sum, d) => sum + d.orderCount, 0));
  readonly daysWithSales = computed(() => this.salesDays().filter((d) => d.orderCount > 0));
  readonly activeDay = computed(() => {
    const index = this.activeDayIndex();
    return index === null ? null : (this.salesDays()[index] ?? null);
  });

  /** Month-to-date vs the same stretch of last month, as a signed percentage — wrapped in an object so
   * an exact 0% change still renders; null when last month had nothing to compare against. */
  readonly monthDelta = computed(() => {
    const d = this.dashboard();
    if (!d || d.revenueSamePeriodLastMonth <= 0) return null;
    const percent = Math.round(((d.revenueThisMonth - d.revenueSamePeriodLastMonth) / d.revenueSamePeriodLastMonth) * 100);
    return { percent, up: percent >= 0, magnitude: Math.abs(percent) };
  });

  readonly statusTotal = computed(() => (this.dashboard()?.ordersByStatus ?? []).reduce((sum, s) => sum + s.count, 0));

  /** What's waiting on the admin right now — each tile opens the list already filtered to it. */
  readonly attentionItems = computed<AttentionItem[]>(() => {
    const d = this.dashboard();
    if (!d) return [];
    const countFor = (status: string) => d.ordersByStatus.find((s) => s.status === status)?.count ?? 0;
    const items: AttentionItem[] = [];

    if (this.canSeeOrders()) {
      items.push(
        { key: 'recebido', icon: 'bi-scissors', label: 'A produzir', count: countFor('Recebido'), link: ['/admin/encomendas'], queryParams: { status: 'Recebido' } },
        { key: 'pronto', icon: 'bi-box-seam', label: 'Prontas para enviar ou retirar', count: countFor('Pronto'), link: ['/admin/encomendas'], queryParams: { status: 'Pronto' } },
        {
          key: 'pagamento',
          icon: 'bi-hourglass-split',
          label: 'Pagamento pendente',
          count: d.pendingPaymentOrders,
          detail: d.pendingPaymentOrders > 0 ? this.formatBRL(d.pendingPaymentAmount) : undefined,
          link: ['/admin/encomendas'],
          queryParams: { pagamento: 'Pendente' },
        },
      );
    }
    if (this.auth.hasPermission('Reviews') && this.pendingReviews() !== null) {
      items.push({ key: 'avaliacoes', icon: 'bi-star', label: 'Avaliações para aprovar', count: this.pendingReviews()!, link: ['/admin/avaliacoes'] });
    }
    return items;
  });

  readonly nothingPending = computed(
    () => this.attentionItems().every((item) => item.count === 0) && (this.dashboard()?.flaggedOrdersCount ?? 0) === 0,
  );

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.dashboardService.getSummary().subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.lastUpdated.set(new Date());
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set(true);
        this.loading.set(false);
      },
    });

    if (this.auth.hasPermission('Reviews')) {
      this.reviewService.listForAdmin(false, 1, 1).subscribe({
        next: (result) => this.pendingReviews.set(result.totalItems),
        error: () => this.pendingReviews.set(null),
      });
    }
  }

  generateSummary(): void {
    const dashboard = this.dashboard();
    if (!dashboard) return;

    this.generatingSummary.set(true);
    this.summaryError.set(null);
    this.dashboardService.generateNarrativeSummary(dashboard).subscribe({
      next: ({ summary }) => {
        this.narrativeSummary.set(summary);
        this.generatingSummary.set(false);
      },
      error: () => {
        this.generatingSummary.set(false);
        this.summaryError.set('Não foi possível gerar o resumo agora. Tente de novo em instantes.');
      },
    });
  }

  statusShare(count: number): number {
    const total = this.statusTotal();
    return total === 0 ? 0 : Math.round((count / total) * 100);
  }

  /** Bar height as a % of the plot, against a rounded axis maximum. Days without sales draw nothing. */
  barHeight(day: SalesByDay): number {
    return day.revenue <= 0 ? 0 : Math.max(1.5, (day.revenue / this.salesMax()) * 100);
  }

  /** Horizontal tooltip anchor (% of the plot width), clamped so it never spills past either edge. */
  tooltipLeft(index: number): number {
    const center = ((index + 0.5) / 30) * 100;
    return Math.min(85, Math.max(15, center));
  }

  onChartKeydown(event: KeyboardEvent): void {
    const last = this.salesDays().length - 1;
    const moves: Record<string, number> = { ArrowLeft: -1, ArrowRight: 1, Home: -Infinity, End: Infinity };
    if (!(event.key in moves)) return;
    event.preventDefault();
    const next = Math.min(last, Math.max(0, this.focusDayIndex() + moves[event.key]));
    this.focusDayIndex.set(next);
    this.activeDayIndex.set(next);
    (document.getElementById(`sales-day-${next}`) as HTMLElement | null)?.focus();
  }

  private formatBRL(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }
}
