import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { SITE_NAME } from '@shared/core/constants/site';
import { Order } from '@shared/core/models/order.model';
import { OrderService } from '@shared/core/services/order.service';

type FakePaymentMethod = 'pix' | 'cartao';

@Component({
  selector: 'app-fake-payment',
  standalone: true,
  imports: [CurrencyPipe],
  templateUrl: './fake-payment.html',
})
export class FakePayment implements OnInit {
  readonly order = signal<Order | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly submitting = signal(false);
  readonly method = signal<FakePaymentMethod>('pix');

  private readonly title = inject(Title);
  private orderId!: string;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly orderService: OrderService,
  ) {}

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('orderId')!;
    this.orderService.getById(this.orderId).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
        this.title.setTitle(`Pagamento (simulação) — ${SITE_NAME}`);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  selectMethod(method: FakePaymentMethod): void {
    this.method.set(method);
  }

  confirm(approved: boolean): void {
    this.submitting.set(true);
    this.orderService.simulatePayment(this.orderId, approved).subscribe({
      next: () => this.router.navigate(['/pedido', this.orderId]),
      error: () => this.submitting.set(false),
    });
  }
}
