import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { SITE_NAME } from '@shared/core/constants/site';
import { OrderService } from '@shared/core/services/order.service';
import { OrderConfirmationView } from '@shared/shared/components/order-confirmation-view/order-confirmation-view';

@Component({
  selector: 'app-order-confirmation',
  standalone: true,
  imports: [OrderConfirmationView],
  templateUrl: './order-confirmation.html',
})
export class OrderConfirmation implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly title = inject(Title);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  readonly orderId = this.route.snapshot.paramMap.get('id')!;

  ngOnInit(): void {
    this.orderService.getById(this.orderId).subscribe({
      next: (order) => this.title.setTitle(`Pedido #${order.id.slice(0, 8)} — ${SITE_NAME}`),
      error: () => {},
    });
  }

  goToShop(): void {
    this.router.navigateByUrl('/loja');
  }
}
