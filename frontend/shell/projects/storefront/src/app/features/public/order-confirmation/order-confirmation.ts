import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { jsPDF } from 'jspdf';
import { SITE_NAME } from '@shared/core/constants/site';
import {
  Order,
  ORDER_STATUS_FLOW,
  ORDER_STATUS_LABELS,
  PAYMENT_STATUS_LABELS,
  ShippingAddress,
} from '@shared/core/models/order.model';
import { OrderService } from '@shared/core/services/order.service';

@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './order-confirmation.html',
})
export class OrderConfirmation implements OnInit {
  readonly order = signal<Order | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly statusLabels = ORDER_STATUS_LABELS;
  readonly statusFlow = ORDER_STATUS_FLOW;
  readonly paymentStatusLabels = PAYMENT_STATUS_LABELS;

  private readonly title = inject(Title);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly orderService: OrderService,
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.orderService.getById(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
        this.title.setTitle(`Pedido #${order.id.slice(0, 8)} — ${SITE_NAME}`);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  stepIndex(status: string): number {
    return this.statusFlow.indexOf(status as never);
  }

  downloadReceipt(): void {
    const o = this.order();
    if (!o) return;

    const doc = new jsPDF();
    const marginX = 15;
    let y = 20;

    doc.setFontSize(16);
    doc.text(SITE_NAME, marginX, y);
    y += 8;
    doc.setFontSize(10);
    doc.text(`Comprovante do pedido #${o.id.slice(0, 8)}`, marginX, y);
    y += 6;
    doc.text(`Data: ${new Date(o.createdAt).toLocaleDateString('pt-BR')}`, marginX, y);
    y += 6;
    doc.text(`Status: ${this.statusLabels[o.status]} — Pagamento: ${this.paymentStatusLabels[o.paymentStatus]}`, marginX, y);
    y += 10;

    doc.setFontSize(12);
    doc.text('Cliente', marginX, y);
    y += 6;
    doc.setFontSize(10);
    doc.text(o.customerName, marginX, y);
    y += 5;
    doc.text(o.customerEmail, marginX, y);
    y += 5;
    if (o.customerPhone) {
      doc.text(o.customerPhone, marginX, y);
      y += 5;
    }

    const address = this.parsedShippingAddress(o.shippingAddressJson);
    if (address) {
      y += 5;
      doc.setFontSize(12);
      doc.text('Endereço de entrega', marginX, y);
      y += 6;
      doc.setFontSize(10);
      const complement = address.complement ? ` — ${address.complement}` : '';
      doc.text(`${address.street}, ${address.number}${complement}`, marginX, y);
      y += 5;
      doc.text(`${address.neighborhood} — ${address.city}/${address.state}`, marginX, y);
      y += 5;
      doc.text(`CEP ${address.zipCode}`, marginX, y);
      y += 5;
    }

    y += 5;
    doc.setFontSize(12);
    doc.text('Itens', marginX, y);
    y += 6;
    doc.setFontSize(10);
    for (const item of o.items) {
      doc.text(`${item.quantity}x ${item.productName}`, marginX, y);
      doc.text(item.subtotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), 195, y, { align: 'right' });
      y += 6;
    }

    y += 4;
    doc.text('Subtotal', marginX, y);
    doc.text(o.itemsTotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), 195, y, { align: 'right' });
    y += 6;
    doc.text('Frete', marginX, y);
    doc.text(o.shippingCost.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), 195, y, { align: 'right' });
    y += 6;
    if (o.couponDiscountAmount > 0) {
      doc.text(`Cupom ${o.couponCode}`, marginX, y);
      doc.text(`-${o.couponDiscountAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}`, 195, y, { align: 'right' });
      y += 6;
    }
    doc.setFontSize(12);
    doc.text('Total', marginX, y);
    doc.text(o.total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), 195, y, { align: 'right' });

    doc.save(`comprovante-pedido-${o.id.slice(0, 8)}.pdf`);
  }

  private parsedShippingAddress(json: string | null): ShippingAddress | null {
    if (!json) return null;
    try {
      return JSON.parse(json) as ShippingAddress;
    } catch {
      return null;
    }
  }
}
