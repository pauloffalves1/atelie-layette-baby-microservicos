export interface OrdersByStatus {
  status: string;
  count: number;
}

export interface RecentOrderSummary {
  id: string;
  customerName: string;
  status: string;
  total: number;
  createdAt: string;
}

export interface TopProduct {
  productName: string;
  quantitySold: number;
  revenue: number;
}

export interface SalesByDay {
  /** A calendar day in Brasília time, sent as local midnight without an offset ("2026-09-13T00:00:00"). */
  date: string;
  revenue: number;
  orderCount: number;
}

/** Every figure excludes cancelled orders; revenue counts any payment status (see the paid/pending breakdown). */
export interface Dashboard {
  totalOrders: number;
  openOrders: number;
  revenueTotal: number;
  revenueThisMonth: number;
  averageOrderValue: number;
  totalProducts: number;
  totalCustomers: number;
  /** Workflow order (Recebido → Entregue), statuses with no orders omitted. */
  ordersByStatus: OrdersByStatus[];
  recentOrders: RecentOrderSummary[];
  topProducts: TopProduct[];
  /** Exactly the last 30 local days ending today, including days without sales. */
  salesLast30Days: SalesByDay[];
  revenueThisMonthPaid: number;
  /** Same stretch of the previous month (day 1 up to now) — the basis for the month-over-month delta. */
  revenueSamePeriodLastMonth: number;
  pendingPaymentOrders: number;
  pendingPaymentAmount: number;
  /** Orders still to be produced with an embroidery text the AI pre-screen flagged. */
  flaggedOrdersCount: number;
  flaggedOrders: RecentOrderSummary[] | null;
}

/**
 * The test dashboard (RF40) runs the ateliê's own aggregation over test orders only, so it carries
 * every order-derived figure and none of the catalog/customer ones — test products are listed on
 * their own and there is no such thing as a "test customer count" worth a tile.
 */
export type TestDashboard = Omit<Dashboard, 'totalProducts' | 'totalCustomers'>;
