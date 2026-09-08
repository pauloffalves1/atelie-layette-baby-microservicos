import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  CustomOrderDetails,
  Order,
  ORDER_STATUS_LABELS,
  OrderItemOptions,
  OrderStatus,
  PAYMENT_STATUS_LABELS,
  ShippingAddress,
} from '@shared/core/models/order.model';
import { OrderService } from '@shared/core/services/order.service';
import { CpfMaskPipe } from '@shared/shared/pipes/cpf-mask.pipe';

const ALLOWED_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
  Recebido: ['EmProducao', 'Cancelado'],
  EmProducao: ['Pronto', 'Cancelado'],
  Pronto: ['Enviado', 'Cancelado'],
  Enviado: ['Entregue'],
  Entregue: [],
  Cancelado: [],
};

@Component({
  selector: 'app-admin-order-detail',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink, CpfMaskPipe],
  templateUrl: './admin-order-detail.html',
})
export class AdminOrderDetail implements OnInit {
  readonly order = signal<Order | null>(null);
  readonly loading = signal(true);
  readonly updating = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly statusLabels = ORDER_STATUS_LABELS;
  readonly paymentStatusLabels = PAYMENT_STATUS_LABELS;

  readonly generatingPaymentLink = signal(false);
  readonly paymentLinkError = signal<string | null>(null);
  readonly paymentLink = signal<string | null>(null);
  readonly paymentLinkCopied = signal(false);

  readonly trackingCodeInput = signal('');
  readonly savingTrackingCode = signal(false);
  readonly trackingCodeError = signal<string | null>(null);
  readonly trackingCodeSaved = signal(false);

  private orderId!: string;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly orderService: OrderService,
  ) {}

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  get availableTransitions(): OrderStatus[] {
    const order = this.order();
    return order ? ALLOWED_TRANSITIONS[order.status] : [];
  }

  changeStatus(status: OrderStatus): void {
    this.updating.set(true);
    this.errorMessage.set(null);

    this.orderService.changeStatus(this.orderId, status).subscribe({
      next: (order) => {
        this.order.set(order);
        this.updating.set(false);
      },
      error: (err) => {
        this.updating.set(false);
        this.errorMessage.set(err?.error?.detail ?? 'Não foi possível atualizar o status.');
      },
    });
  }

  generatePaymentLink(): void {
    this.generatingPaymentLink.set(true);
    this.paymentLinkError.set(null);
    this.paymentLinkCopied.set(false);

    this.orderService.generatePaymentLink(this.orderId).subscribe({
      next: ({ paymentUrl }) => {
        this.paymentLink.set(paymentUrl);
        this.generatingPaymentLink.set(false);
        window.open(paymentUrl, '_blank', 'noopener');
      },
      error: (err) => {
        this.generatingPaymentLink.set(false);
        this.paymentLinkError.set(err?.error?.detail ?? 'Não foi possível gerar o link de pagamento.');
      },
    });
  }

  openPaymentLink(): void {
    const url = this.paymentLink();
    if (url) window.open(url, '_blank', 'noopener');
  }

  copyPaymentLink(): void {
    const url = this.paymentLink();
    if (!url) return;

    navigator.clipboard.writeText(url).then(
      () => {
        this.paymentLinkCopied.set(true);
        setTimeout(() => this.paymentLinkCopied.set(false), 2000);
      },
      () => this.paymentLinkError.set('Não foi possível copiar o link automaticamente — selecione e copie o texto do campo.'),
    );
  }

  saveTrackingCode(): void {
    this.savingTrackingCode.set(true);
    this.trackingCodeError.set(null);
    this.trackingCodeSaved.set(false);

    this.orderService.setTrackingCode(this.orderId, this.trackingCodeInput().trim() || null).subscribe({
      next: (order) => {
        this.order.set(order);
        this.savingTrackingCode.set(false);
        this.trackingCodeSaved.set(true);
        setTimeout(() => this.trackingCodeSaved.set(false), 2000);
      },
      error: (err) => {
        this.savingTrackingCode.set(false);
        this.trackingCodeError.set(err?.error?.detail ?? 'Não foi possível salvar o código de rastreio.');
      },
    });
  }

  parsedCustomDetails(): CustomOrderDetails | null {
    const json = this.order()?.customDetailsJson;
    if (!json) return null;
    try {
      return JSON.parse(json) as CustomOrderDetails;
    } catch {
      return null;
    }
  }

  parsedItemOptions(optionsJson: string | null): OrderItemOptions | null {
    if (!optionsJson) return null;
    try {
      return JSON.parse(optionsJson) as OrderItemOptions;
    } catch {
      return null;
    }
  }

  printPackingSlip(): void {
    window.print();
  }

  parsedShippingAddress(): ShippingAddress | null {
    const json = this.order()?.shippingAddressJson;
    if (!json) return null;
    try {
      return JSON.parse(json) as ShippingAddress;
    } catch {
      return null;
    }
  }

  private load(): void {
    this.loading.set(true);
    this.orderService.getById(this.orderId).subscribe({
      next: (order) => {
        this.order.set(order);
        this.trackingCodeInput.set(order.trackingCode ?? '');
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
