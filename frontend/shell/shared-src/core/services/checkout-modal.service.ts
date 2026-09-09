import { Injectable, signal } from '@angular/core';

export type CheckoutModalStep = 'cart' | 'delivery' | 'payment' | 'confirmation';

@Injectable({ providedIn: 'root' })
export class CheckoutModalService {
  readonly isOpen = signal(false);
  readonly step = signal<CheckoutModalStep>('cart');
  readonly confirmedOrderId = signal<string | null>(null);

  open(step: CheckoutModalStep = 'cart'): void {
    this.step.set(step);
    this.isOpen.set(true);
  }

  close(): void {
    this.isOpen.set(false);
    this.step.set('cart');
    this.confirmedOrderId.set(null);
  }

  goTo(step: CheckoutModalStep): void {
    this.step.set(step);
  }

  showConfirmation(orderId: string): void {
    this.confirmedOrderId.set(orderId);
    this.step.set('confirmation');
  }
}
