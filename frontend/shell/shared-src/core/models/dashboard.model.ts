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
  date: string;
  revenue: number;
  orderCount: number;
}

export interface Dashboard {
  totalOrders: number;
  openOrders: number;
  revenueTotal: number;
  revenueThisMonth: number;
  averageOrderValue: number;
  totalProducts: number;
  totalCustomers: number;
  ordersByStatus: OrdersByStatus[];
  recentOrders: RecentOrderSummary[];
  topProducts: TopProduct[];
  salesLast30Days: SalesByDay[];
}
