export type OrderStatus = 'Recebido' | 'EmProducao' | 'Pronto' | 'Enviado' | 'Entregue' | 'Cancelado';
export type OrderType = 'Loja' | 'Personalizada';
export type PaymentStatus = 'Pendente' | 'Pago' | 'Recusado';

export interface OrderItem {
  id: string;
  productId: string | null;
  productName: string;
  unitPrice: number;
  quantity: number;
  subtotal: number;
  optionsJson: string | null;
  moderationFlag: string | null;
}

export interface Order {
  id: string;
  customerId: string | null;
  customerName: string;
  customerEmail: string;
  customerPhone: string | null;
  customerCpf: string | null;
  type: OrderType;
  status: OrderStatus;
  itemsTotal: number;
  shippingCost: number;
  total: number;
  notes: string | null;
  giftMessage: string | null;
  recipientName: string | null;
  customDetailsJson: string | null;
  shippingAddressJson: string | null;
  deliveryMethod: DeliveryMethod;
  createdAt: string;
  updatedAt: string;
  items: OrderItem[];
  paymentStatus: PaymentStatus;
  externalPaymentId: string | null;
  trackingCode: string | null;
  couponCode: string | null;
  couponDiscountAmount: number;
  paymentDeclineReason: string | null;
  pixQrCodeText: string | null;
  pixQrCodeImageUrl: string | null;
  boletoBarcode: string | null;
  boletoUrl: string | null;
  /** A purchase of a test product (RF40) — only the admin's test screen lists these. */
  isTest?: boolean;
}

export interface CreateOrderItemRequest {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
  optionsJson: string | null;
}

/** Order agreed outside the site (WhatsApp, in person) and typed in the admin panel — prices as agreed. */
export interface CreateManualOrderRequest {
  customerId: string | null;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  customerCpf: string;
  deliveryMethod: 'Entrega' | 'Retirada';
  shippingAddressJson: string | null;
  shippingCost: number;
  items: { productId: string | null; productName: string; unitPrice: number; quantity: number; optionsJson: string | null }[];
  paymentReceived: boolean;
  notifyCustomer: boolean;
  notes: string | null;
  giftMessage: string | null;
  recipientName: string | null;
}

export type PaymentMethod = 'CREDIT_CARD' | 'PIX' | 'BOLETO';
export type DeliveryMethod = 'Entrega' | 'Retirada';

export interface CreateStoreOrderRequest {
  customerName: string;
  customerEmail: string;
  customerPhone: string | null;
  customerCpf: string;
  notes: string | null;
  shippingAddressJson: string | null;
  shippingCost: number;
  items: CreateOrderItemRequest[];
  couponCode?: string | null;
  paymentMethod: PaymentMethod;
  encryptedCard?: string | null;
  installments?: number;
  giftMessage?: string | null;
  threeDsAuthenticationId?: string | null;
  deliveryMethod?: DeliveryMethod;
  recipientName?: string | null;
}

export interface CustomOrderDetails {
  tipoPeca: string;
  tamanho: string;
  tecido: string;
  cor: string;
  nomeBordado: string;
  observacoes: string;
}

export interface OrderItemOptions {
  embroideryText?: string;
  threadColor?: string;
}

export interface ShippingAddress {
  street: string;
  number: string;
  complement: string | null;
  neighborhood: string;
  city: string;
  state: string;
  zipCode: string;
}

export interface CreateCustomOrderRequest {
  customerName: string;
  customerEmail: string;
  customerPhone: string | null;
  notes: string | null;
  customDetailsJson: string;
  estimatedPrice: number;
}

export const ORDER_STATUS_LABELS: Record<string, string> = {
  Recebido: 'Recebido',
  EmProducao: 'Em produção',
  Pronto: 'Pronto',
  Enviado: 'Enviado',
  Entregue: 'Entregue',
  Cancelado: 'Cancelado',
};

export const ORDER_STATUS_FLOW: OrderStatus[] = ['Recebido', 'EmProducao', 'Pronto', 'Enviado', 'Entregue'];

export const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
  Pendente: 'Pagamento pendente',
  Pago: 'Pagamento aprovado',
  Recusado: 'Pagamento recusado',
};
