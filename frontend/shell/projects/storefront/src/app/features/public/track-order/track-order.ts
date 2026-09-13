import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '@shared/core/services/auth.service';
import { OrderService } from '@shared/core/services/order.service';
import { SeoService } from '@shared/core/services/seo.service';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';

@Component({
  selector: 'app-track-order',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './track-order.html',
})
export class TrackOrder {
  private readonly fb = inject(FormBuilder);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);
  private readonly seo = inject(SeoService);
  readonly auth = inject(AuthService);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    orderNumber: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
  });

  constructor() {
    const user = this.auth.currentUser();
    if (user) this.form.patchValue({ email: user.email });

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
    // Customers often paste the number exactly as the e-mail shows it ("#722ee49d").
    this.orderService.lookup(orderNumber.trim().replace(/^#/, ''), email.trim()).subscribe({
      next: (order) => this.router.navigate(['/pedido', order.id]),
      error: (err) => {
        this.submitting.set(false);
        // Only a real "no match" says the order doesn't exist — a dropped connection or the rate
        // limit used to tell the customer their (correct) order number was wrong.
        const notFound = err instanceof HttpErrorResponse && (err.status === 404 || err.status === 400);
        this.errorMessage.set(notFound
          ? 'Pedido não encontrado. Confira o e-mail e o número do pedido.'
          : httpErrorMessage(err, 'Não foi possível consultar o pedido agora. Tente de novo em instantes.'));
      },
    });
  }
}
