import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerSummary } from '@shared/core/models/customer.model';
import { CustomerAdminService } from '@shared/core/services/customer-admin.service';
import { CpfMaskPipe } from '@shared/shared/pipes/cpf-mask.pipe';

@Component({
  selector: 'app-admin-customer-list',
  standalone: true,
  imports: [DatePipe, CpfMaskPipe, RouterLink],
  templateUrl: './admin-customer-list.html',
})
export class AdminCustomerList implements OnInit {
  readonly customers = signal<CustomerSummary[]>([]);
  readonly loading = signal(true);
  readonly verifyingId = signal<string | null>(null);
  readonly removingId = signal<string | null>(null);

  constructor(private readonly customerAdminService: CustomerAdminService) {}

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.customerAdminService.list().subscribe({
      next: (customers) => {
        this.customers.set(customers);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  verifyEmail(customer: CustomerSummary): void {
    if (this.verifyingId()) return;
    this.verifyingId.set(customer.id);
    this.customerAdminService.verifyEmail(customer.id).subscribe({
      next: (updated) => {
        this.customers.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
        this.verifyingId.set(null);
      },
      error: () => this.verifyingId.set(null),
    });
  }

  removeCustomer(customer: CustomerSummary): void {
    if (this.removingId()) return;
    const confirmed = confirm(
      `Remover a conta de "${customer.name}"? Se houver pedidos associados, os dados pessoais serão anonimizados em vez de excluídos — o histórico de pedidos é sempre preservado.`,
    );
    if (!confirmed) return;

    this.removingId.set(customer.id);
    this.customerAdminService.remove(customer.id).subscribe({
      next: () => {
        this.removingId.set(null);
        this.load();
      },
      error: () => this.removingId.set(null),
    });
  }
}
