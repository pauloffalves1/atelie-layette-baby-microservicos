import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { OrderService } from '@shared/core/services/order.service';
import { SeoService } from '@shared/core/services/seo.service';

@Component({
  selector: 'app-track-order',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './track-order.html',
})
export class TrackOrder {
  private readonly fb = inject(FormBuilder);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    orderNumber: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
  });

  constructor() {
    this.seo.update({
      title: 'Rastrear pedido',
      description: 'Consulte o status da sua encomenda informando o e-mail e o número do pedido.',
      path: '/rastrear-pedido',
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const { orderNumber, email } = this.form.getRawValue();
    this.orderService.lookup(orderNumber.trim(), email.trim()).subscribe({
      next: (order) => this.router.navigate(['/pedido', order.id]),
      error: () => {
        this.submitting.set(false);
        this.errorMessage.set('Pedido não encontrado. Confira o e-mail e o número do pedido.');
      },
    });
  }
}
