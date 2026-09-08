import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { Order, ORDER_STATUS_LABELS } from '@shared/core/models/order.model';
import { AuthService } from '@shared/core/services/auth.service';
import { OrderService } from '@shared/core/services/order.service';

@Component({
  selector: 'app-my-account',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './my-account.html',
})
export class MyAccount implements OnInit {
  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly statusLabels = ORDER_STATUS_LABELS;

  readonly emailVerified = signal(true);
  readonly resendingVerification = signal(false);
  readonly verificationSent = signal(false);

  readonly confirmingDelete = signal(false);
  readonly deletePassword = signal('');
  readonly deleting = signal(false);
  readonly deleteError = signal<string | null>(null);

  constructor(
    readonly auth: AuthService,
    private readonly orderService: OrderService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.orderService.listMine().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });

    this.auth.getProfile().subscribe({
      next: (profile) => this.emailVerified.set(profile.emailVerified),
      error: () => {},
    });
  }

  resendVerification(): void {
    this.resendingVerification.set(true);
    this.auth.resendVerification().subscribe({
      next: () => {
        this.resendingVerification.set(false);
        this.verificationSent.set(true);
      },
      error: () => this.resendingVerification.set(false),
    });
  }

  startDeleteAccount(): void {
    this.confirmingDelete.set(true);
    this.deleteError.set(null);
  }

  cancelDeleteAccount(): void {
    this.confirmingDelete.set(false);
    this.deletePassword.set('');
    this.deleteError.set(null);
  }

  confirmDeleteAccount(): void {
    if (!this.deletePassword()) {
      this.deleteError.set('Informe sua senha para confirmar.');
      return;
    }

    this.deleting.set(true);
    this.deleteError.set(null);

    this.auth.deleteAccount(this.deletePassword()).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: (err) => {
        this.deleting.set(false);
        this.deleteError.set(err?.error?.detail ?? 'Não foi possível excluir a conta.');
      },
    });
  }
}
