import { CurrencyPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, input, output, signal } from '@angular/core';
import { jsPDF } from 'jspdf';
import { SITE_NAME } from '../../../core/constants/site';
import { Order, ORDER_STATUS_FLOW, ORDER_STATUS_LABELS, PAYMENT_STATUS_LABELS, ShippingAddress } from '../../../core/models/order.model';
import { OrderService } from '../../../core/services/order.service';
import { PixQrCode } from '../pix-qr-code/pix-qr-code';

/**
 * Shows a full order's status/PIX/items — the content of the post-checkout confirmation. Used
 * both by the standalone /pedido/:id route (bookmarkable, linked from WhatsApp/Minha Conta/admin)
 * and as the last step of the cart/checkout modal, so this logic (polling, PDF receipt, PIX copy)
 * only lives in one place.
 */
@Component({
  selector: 'app-order-confirmation-view',
  standalone: true,
  imports: [CurrencyPipe, PixQrCode],
  templateUrl: './order-confirmation-view.html',
})
export class OrderConfirmationView implements OnInit, OnDestroy {
  readonly orderId = input.required<string>();
  readonly continueShopping = output<void>();

  readonly order = signal<Order | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly pixCodeCopied = signal(false);
  readonly statusLabels = ORDER_STATUS_LABELS;
  readonly statusFlow = ORDER_STATUS_FLOW;
  readonly paymentStatusLabels = PAYMENT_STATUS_LABELS;

  private readonly orderService = inject(OrderService);
  private pollHandle: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.load(this.orderId());
  }

  ngOnDestroy(): void {
    if (this.pollHandle) clearInterval(this.pollHandle);
  }

  private load(id: string): void {
    this.orderService.getById(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);

        if (order.paymentStatus === 'Pendente' && order.pixQrCodeText) {
          this.pollHandle = setInterval(() => {
            this.orderService.getById(id).subscribe((refreshed) => {
              this.order.set(refreshed);
              if (refreshed.paymentStatus !== 'Pendente' && this.pollHandle) {
                clearInterval(this.pollHandle);
                this.pollHandle = null;
              }
            });
          }, 5000);
        }
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  copyPixCode(): void {
    const code = this.order()?.pixQrCodeText;
    if (!code) return;

    navigator.clipboard.writeText(code).then(() => {
      this.pixCodeCopied.set(true);
      setTimeout(() => this.pixCodeCopied.set(false), 2000);
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
