import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomerSummary } from '@shared/core/models/customer.model';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';
import { CustomerAdminService } from '@shared/core/services/customer-admin.service';
import { whatsappUrl } from '@shared/core/utils/contact-links';

/** Lowercase, accent-free — so "joao" finds "João". */
function normalizeForSearch(value: string | null | undefined): string {
  return (value ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}
import { CpfMaskPipe } from '@shared/shared/pipes/cpf-mask.pipe';
import { LoadError } from '@shared/shared/components/load-error/load-error';

@Component({
  selector: 'app-admin-customer-list',
  standalone: true,
  imports: [DatePipe, CpfMaskPipe, RouterLink, LoadError],
  templateUrl: './admin-customer-list.html',
})
export class AdminCustomerList implements OnInit {
  private readonly auth = inject(AdminAuthService);

  readonly customers = signal<CustomerSummary[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly verifyingId = signal<string | null>(null);
  readonly removingId = signal<string | null>(null);
  readonly search = signal('');

  /** Only an admin who holds "Testes" approves test users (RF40) — for everyone else the column isn't even rendered. */
  readonly canApproveTestUsers = computed(() => this.auth.hasPermission('Testing'));
  readonly testingId = signal<string | null>(null);
  readonly testError = signal<string | null>(null);

  /** The admin customer endpoint already returns every account (unpaginated), so search filters
   * in memory — name/e-mail ignore case and accents, phone/CPF match on digits alone. */
  readonly filteredCustomers = computed(() => {
    const term = normalizeForSearch(this.search().trim());
    if (!term) return this.customers();
    const digits = term.replace(/\D/g, '');
    return this.customers().filter(
      (c) =>
        normalizeForSearch(c.name).includes(term) ||
        normalizeForSearch(c.email).includes(term) ||
        (digits.length >= 3 && ((c.phone ?? '').replace(/\D/g, '').includes(digits) || (c.cpf ?? '').replace(/\D/g, '').includes(digits))),
    );
  });

  whatsappLink(customer: CustomerSummary): string | null {
    return whatsappUrl(customer.phone);
  }

  constructor(private readonly customerAdminService: CustomerAdminService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.customerAdminService.list().subscribe({
      next: (customers) => {
        this.customers.set(customers);
        this.loading.set(false);
      },
      error: () => {
          this.loading.set(false);
          this.loadError.set(true);
        },
    });
  }

  /**
   * Approves/revokes a test user (RF40). The flag rides in the customer's token, so it only takes
   * effect on her next login — the warning next to the switch says so.
   */
  toggleTestUser(customer: CustomerSummary): void {
    if (this.testingId()) return;
    this.testingId.set(customer.id);
    this.testError.set(null);
    this.customerAdminService.setTest(customer.id, !customer.isTest).subscribe({
      next: (updated) => {
        this.customers.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
        this.testingId.set(null);
      },
      error: () => {
        this.testingId.set(null);
        this.testError.set('Não foi possível alterar o acesso de teste desta cliente.');
      },
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
      customer.isAnonymized
        ? `Excluir de vez a conta "${customer.name}"? Só funciona se ela não tiver nenhum pedido — caso ainda tenha, nada muda.`
        : `Remover a conta de "${customer.name}"? Se houver pedidos associados, os dados pessoais serão anonimizados em vez de excluídos — o histórico de pedidos é sempre preservado.`,
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
