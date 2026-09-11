import { CurrencyPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, input, output, signal } from '@angular/core';
import { jsPDF } from 'jspdf';
import { autoTable } from 'jspdf-autotable';
import { SITE_ADDRESS, SITE_CNPJ, SITE_NAME } from '../../../core/constants/site';
import { Order, OrderItem, ORDER_STATUS_FLOW, ORDER_STATUS_LABELS, PAYMENT_STATUS_LABELS, ShippingAddress } from '../../../core/models/order.model';
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
  readonly boletoBarcodeCopied = signal(false);
  readonly canceling = signal(false);
  readonly cancelError = signal<string | null>(null);
  readonly generatingReceipt = signal(false);
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

        if (order.paymentStatus === 'Pendente' && (order.pixQrCodeText || order.boletoBarcode)) {
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

  copyBoletoBarcode(): void {
    const barcode = this.order()?.boletoBarcode;
    if (!barcode) return;

    navigator.clipboard.writeText(barcode).then(() => {
      this.boletoBarcodeCopied.set(true);
      setTimeout(() => this.boletoBarcodeCopied.set(false), 2000);
    });
  }

  cancelOrder(): void {
    const order = this.order();
    if (!order || this.canceling()) return;

    const confirmed = confirm(`Cancelar o pedido #${order.id.slice(0, 8)}? Essa ação não pode ser desfeita.`);
    if (!confirmed) return;

    this.canceling.set(true);
    this.cancelError.set(null);
    this.orderService.cancel(order.id).subscribe({
      next: (updated) => {
        this.canceling.set(false);
        this.order.set(updated);
      },
      error: (err) => {
        this.canceling.set(false);
        this.cancelError.set(err?.error?.detail ?? 'Não foi possível cancelar o pedido.');
      },
    });
  }

  stepIndex(status: string): number {
    return this.statusFlow.indexOf(status as never);
  }

  async downloadReceipt(): Promise<void> {
    const o = this.order();
    if (!o || this.generatingReceipt()) return;
    this.generatingReceipt.set(true);

    try {
      await this.buildAndSaveReceipt(o);
    } finally {
      this.generatingReceipt.set(false);
    }
  }

  private async buildAndSaveReceipt(o: Order): Promise<void> {
    const doc = new jsPDF();
    const marginX = 15;
    const pageWidth = doc.internal.pageSize.getWidth();
    const rightX = pageWidth - marginX;
    let y = 18;

    const logo = await this.loadLogoDataUrl();
    if (logo) {
      // Real dimensions 818x420 — keep that ratio so it never looks stretched.
      const logoWidth = 32;
      const logoHeight = (420 / 818) * logoWidth;
      doc.addImage(logo, 'PNG', marginX, y, logoWidth, logoHeight);
    }

    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text(SITE_NAME, rightX, y + 5, { align: 'right' });
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(9);
    doc.setTextColor(90);
    doc.text(SITE_ADDRESS, rightX, y + 11, { align: 'right', maxWidth: 120 });
    doc.text(`CNPJ ${SITE_CNPJ}`, rightX, y + 16, { align: 'right' });
    doc.setTextColor(0);

    y += 26;
    doc.setDrawColor(200);
    doc.line(marginX, y, rightX, y);
    y += 10;

    doc.setFontSize(13);
    doc.setFont('helvetica', 'bold');
    doc.text(`Comprovante do pedido #${o.id.slice(0, 8)}`, marginX, y);
    doc.setFont('helvetica', 'normal');
    y += 7;
    doc.setFontSize(10);
    doc.text(`Data: ${new Date(o.createdAt).toLocaleDateString('pt-BR')}`, marginX, y);
    y += 6;
    doc.text(`Status: ${this.statusLabels[o.status]} — Pagamento: ${this.paymentStatusLabels[o.paymentStatus]}`, marginX, y);
    y += 10;

    doc.setFontSize(11);
    doc.setFont('helvetica', 'bold');
    doc.text('Cliente', marginX, y);
    doc.setFont('helvetica', 'normal');
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
      doc.setFontSize(11);
      doc.setFont('helvetica', 'bold');
      doc.text('Endereço de entrega', marginX, y);
      doc.setFont('helvetica', 'normal');
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

    y += 8;
    autoTable(doc, {
      startY: y,
      margin: { left: marginX, right: marginX },
      head: [['Qtd', 'Item', 'Valor unit.', 'Subtotal']],
      body: o.items.map((item) => [
        String(item.quantity),
        this.itemDescription(item),
        item.unitPrice.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }),
        item.subtotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }),
      ]),
      headStyles: { fillColor: [212, 148, 170] },
      columnStyles: { 0: { cellWidth: 14, halign: 'center' }, 2: { halign: 'right' }, 3: { halign: 'right' } },
      styles: { fontSize: 9 },
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    y = (doc as any).lastAutoTable.finalY + 8;

    const totalsX = rightX - 45;
    doc.setFontSize(10);
    doc.text('Subtotal', totalsX, y, { align: 'right' });
    doc.text(o.itemsTotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), rightX, y, { align: 'right' });
    y += 6;
    doc.text('Frete', totalsX, y, { align: 'right' });
    doc.text(o.shippingCost.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), rightX, y, { align: 'right' });
    y += 6;
    if (o.couponDiscountAmount > 0) {
      doc.text(`Cupom ${o.couponCode}`, totalsX, y, { align: 'right' });
      doc.text(`-${o.couponDiscountAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}`, rightX, y, { align: 'right' });
      y += 6;
    }
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('Total', totalsX, y, { align: 'right' });
    doc.text(o.total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), rightX, y, { align: 'right' });

    const pageHeight = doc.internal.pageSize.getHeight();
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    doc.setTextColor(150);
    doc.text(`${SITE_NAME} — ${SITE_ADDRESS} — CNPJ ${SITE_CNPJ}`, pageWidth / 2, pageHeight - 10, { align: 'center', maxWidth: pageWidth - 2 * marginX });

    doc.save(`comprovante-pedido-${o.id.slice(0, 8)}.pdf`);
  }

  private itemDescription(item: OrderItem): string {
    if (!item.optionsJson) return item.productName;
    try {
      const options = JSON.parse(item.optionsJson) as { embroideryText?: string; threadColor?: string };
      const parts = [options.embroideryText, options.threadColor].filter(Boolean);
      return parts.length > 0 ? `${item.productName}\nBordado: ${parts.join(' — ')}` : item.productName;
    } catch {
      return item.productName;
    }
  }

  private async loadLogoDataUrl(): Promise<string | null> {
    try {
      const response = await fetch('/images/logo-atelie.png');
      const blob = await response.blob();
      return await new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result as string);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
      });
    } catch {
      return null;
    }
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
