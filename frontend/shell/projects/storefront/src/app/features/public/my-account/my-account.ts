import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap, tap } from 'rxjs';
import { Order, ORDER_STATUS_LABELS } from '@shared/core/models/order.model';
import { CustomerAddress } from '@shared/core/models/customer-address.model';
import { AuthService } from '@shared/core/services/auth.service';
import { CepService } from '@shared/core/services/cep.service';
import { CustomerAddressService } from '@shared/core/services/customer-address.service';
import { OrderService } from '@shared/core/services/order.service';

@Component({
  selector: 'app-my-account',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink, ReactiveFormsModule],
  templateUrl: './my-account.html',
})
export class MyAccount implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cepService = inject(CepService);
  private readonly addressService = inject(CustomerAddressService);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly statusLabels = ORDER_STATUS_LABELS;
  readonly cancelingId = signal<string | null>(null);
  readonly cancelError = signal<string | null>(null);

  readonly emailVerified = signal(true);
  readonly resendingVerification = signal(false);
  readonly verificationSent = signal(false);

  readonly confirmingDelete = signal(false);
  readonly deletePassword = signal('');
  readonly deleting = signal(false);
  readonly deleteError = signal<string | null>(null);

  readonly addresses = signal<CustomerAddress[]>([]);
  readonly addressesLoading = signal(true);
  readonly addressFormOpen = signal(false);
  readonly editingAddressId = signal<string | null>(null);
  readonly addressSaving = signal(false);
  readonly addressError = signal<string | null>(null);
  readonly cepLoading = signal(false);
  readonly cepError = signal<string | null>(null);

  readonly addressForm = this.fb.nonNullable.group({
    label: ['', Validators.required],
    zipCode: ['', Validators.required],
    street: ['', Validators.required],
    number: ['', Validators.required],
    complement: [''],
    neighborhood: ['', Validators.required],
    city: ['', Validators.required],
    state: ['', Validators.required],
    isDefault: [false],
  });

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

    this.loadAddresses();

    this.addressForm.controls.zipCode.valueChanges
      .pipe(
        map((value) => value.replace(/\D/g, '')),
        distinctUntilChanged(),
        tap(() => this.cepError.set(null)),
        filter((digits) => digits.length === 8),
        tap(() => this.cepLoading.set(true)),
        debounceTime(300),
        switchMap((digits) => this.cepService.lookup(digits).pipe(catchError(() => of(null)))),
      )
      .subscribe((address) => {
        this.cepLoading.set(false);

        if (!address || address.erro) {
          this.cepError.set('CEP não encontrado. Confira o número ou preencha o endereço manualmente.');
          return;
        }

        this.addressForm.patchValue({
          street: address.logradouro,
          neighborhood: address.bairro,
          city: address.localidade,
          state: address.uf,
        });
      });
  }

  cancelOrder(order: Order): void {
    if (this.cancelingId()) return;
    const confirmed = confirm(`Cancelar o pedido #${order.id.slice(0, 8)}? Essa ação não pode ser desfeita.`);
    if (!confirmed) return;

    this.cancelingId.set(order.id);
    this.cancelError.set(null);
    this.orderService.cancel(order.id).subscribe({
      next: (updated) => {
        this.cancelingId.set(null);
        this.orders.update((list) => list.map((o) => (o.id === updated.id ? updated : o)));
      },
      error: (err) => {
        this.cancelingId.set(null);
        this.cancelError.set(err?.error?.detail ?? 'Não foi possível cancelar o pedido.');
      },
    });
  }

  private loadAddresses(): void {
    this.addressesLoading.set(true);
    this.addressService.list().subscribe({
      next: (addresses) => {
        this.addresses.set(addresses);
        this.addressesLoading.set(false);
      },
      error: () => this.addressesLoading.set(false),
    });
  }

  startAddAddress(): void {
    this.editingAddressId.set(null);
    this.addressError.set(null);
    this.addressForm.reset({ label: '', zipCode: '', street: '', number: '', complement: '', neighborhood: '', city: '', state: '', isDefault: false });
    this.addressFormOpen.set(true);
  }

  startEditAddress(address: CustomerAddress): void {
    this.editingAddressId.set(address.id);
    this.addressError.set(null);
    this.addressForm.reset({
      label: address.label,
      zipCode: address.zipCode,
      street: address.street,
      number: address.number,
      complement: address.complement ?? '',
      neighborhood: address.neighborhood,
      city: address.city,
      state: address.state,
      isDefault: address.isDefault,
    });
    this.addressFormOpen.set(true);
  }

  cancelAddressForm(): void {
    this.addressFormOpen.set(false);
    this.editingAddressId.set(null);
    this.addressError.set(null);
  }

  saveAddress(): void {
    if (this.addressForm.invalid) {
      this.addressForm.markAllAsTouched();
      return;
    }

    const value = this.addressForm.getRawValue();
    const request = {
      label: value.label,
      street: value.street,
      number: value.number,
      complement: value.complement || null,
      neighborhood: value.neighborhood,
      city: value.city,
      state: value.state,
      zipCode: value.zipCode,
      isDefault: value.isDefault,
    };

    this.addressSaving.set(true);
    this.addressError.set(null);

    const editingId = this.editingAddressId();
    const save$ = editingId ? this.addressService.update(editingId, request) : this.addressService.create(request);

    save$.subscribe({
      next: () => {
        this.addressSaving.set(false);
        this.addressFormOpen.set(false);
        this.editingAddressId.set(null);
        this.loadAddresses();
      },
      error: (err) => {
        this.addressSaving.set(false);
        this.addressError.set(err?.error?.detail ?? 'Não foi possível salvar o endereço.');
      },
    });
  }

  removeAddress(address: CustomerAddress): void {
    if (!confirm(`Remover o endereço "${address.label}"?`)) return;
    this.addressService.remove(address.id).subscribe({ next: () => this.loadAddresses() });
  }

  setDefaultAddress(address: CustomerAddress): void {
    this.addressService.setDefault(address.id).subscribe({ next: () => this.loadAddresses() });
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
