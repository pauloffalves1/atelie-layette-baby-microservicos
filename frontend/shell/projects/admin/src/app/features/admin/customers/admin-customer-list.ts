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

  constructor(private readonly customerAdminService: CustomerAdminService) {}

  ngOnInit(): void {
    this.customerAdminService.list().subscribe({
      next: (customers) => {
        this.customers.set(customers);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
