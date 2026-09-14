import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable, catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap, tap } from 'rxjs';
import { Order, ORDER_STATUS_LABELS } from '@shared/core/models/order.model';
import { CustomerAddress } from '@shared/core/models/customer-address.model';
import { AuthService } from '@shared/core/services/auth.service';
import { CepService } from '@shared/core/services/cep.service';
import { CustomerAddressService } from '@shared/core/services/customer-address.service';
import { OrderService } from '@shared/core/services/order.service';
import { phoneDigitsValidator } from '@shared/core/utils/br-documents';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';
import { LoadError } from '@shared/shared/components/load-error/load-error';
import { PasswordToggleDirective } from '@shared/shared/directives/password-toggle.directive';
import { PhoneMaskDirective } from '@shared/shared/directives/phone-mask.directive';


@Component({
  selector: 'app-my-account',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, RouterLink, ReactiveFormsModule, LoadError, PasswordToggleDirective, PhoneMaskDirective],
  templateUrl: './my-account.html',
})
export class MyAccount implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cepService = inject(CepService);
  private readonly addressService = inject(CustomerAddressService);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly ordersError = signal(false);
  readonly addressesError = signal(false);
  readonly statusLabels = ORDER_STATUS_LABELS;
  readonly cancelingId = signal<string | null>(null);
  readonly cancelError = signal<string | null>(null);

  readonly emailVerified = signal(true);
  readonly resendingVerification = signal(false);
  readonly verificationSent = signal(false);
  readonly verificationError = signal<string | null>(null);

  /** "Meus dados": until now a typo in the name or an old WhatsApp number could only be fixed by
   * messaging the ateliê. E-mail and CPF are shown read-only (they identify the account). */
  readonly profileLoaded = signal(false);
  readonly profileEmail = signal('');
  readonly profileCpf = signal<string | null>(null);
  readonly profileSaving = signal(false);
  readonly profileSaved = signal(false);
  readonly profileError = signal<string | null>(null);
  readonly profileForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    phone: ['', [Validators.required, phoneDigitsValidator]],
  });

  readonly passwordSaving = signal(false);
  readonly passwordSaved = signal(false);
  readonly passwordError = signal<string | null>(null);
  readonly passwordForm = this.fb.nonNullable.group(
    {
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
    },
    {
      validators: (group: AbstractControl): ValidationErrors | null =>
        group.get('confirmPassword')?.value && group.get('newPassword')?.value !== group.get('confirmPassword')?.value
          ? { mismatch: true }
          : null,
    },
  );

  readonly addressBusyId = signal<string | null>(null);
  readonly addressActionError = signal<string | null>(null);

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
    this.loadOrders();

    this.auth.getProfile().subscribe({
      next: (profile) => {
        this.emailVerified.set(profile.emailVerified);
        this.profileEmail.set(profile.email);
        this.profileCpf.set(profile.cpf);
        this.profileForm.reset({ name: profile.name, phone: profile.phone ?? '' });
        this.profileLoaded.set(true);
      },
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

  /** A failed load must not read as "Você ainda não fez nenhuma encomenda" — nor unlock the
   * no-orders account-deletion copy below it. */
  loadOrders(): void {
    this.loading.set(true);
    this.ordersError.set(false);
    this.orderService.listMine().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.ordersError.set(true);
      },
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

  loadAddresses(): void {
    this.addressesLoading.set(true);
    this.addressesError.set(false);
    this.addressService.list().subscribe({
      next: (addresses) => {
        this.addresses.set(addresses);
        this.addressesLoading.set(false);
      },
      error: () => {
        this.addressesLoading.set(false);
        this.addressesError.set(true);
      },
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
    if (this.addressBusyId()) return;
    if (!confirm(`Remover o endereço "${address.label}"?`)) return;
    this.runAddressAction(address, this.addressService.remove(address.id), `Não foi possível remover o endereço "${address.label}".`);
  }

  setDefaultAddress(address: CustomerAddress): void {
    if (this.addressBusyId()) return;
    this.runAddressAction(address, this.addressService.setDefault(address.id), `Não foi possível tornar "${address.label}" o endereço padrão.`);
  }

  /** Both used to fail silently (the card just stayed as it was) and could be clicked repeatedly. */
  private runAddressAction(address: CustomerAddress, request: Observable<unknown>, failure: string): void {
    this.addressBusyId.set(address.id);
    this.addressActionError.set(null);
    request.subscribe({
      next: () => {
        this.addressBusyId.set(null);
        this.loadAddresses();
      },
      error: (err) => {
        this.addressBusyId.set(null);
        this.addressActionError.set(httpErrorMessage(err, failure));
      },
    });
  }

  /** "Fralda de Ombro Nuvem, Kit Ursinho e mais 1" — the card used to say only "3 item(ns)". */
  itemsSummary(order: Order): string {
    const names = order.items.map((item) => (item.quantity > 1 ? `${item.quantity}× ${item.productName}` : item.productName));
    if (names.length <= 2) return names.join(' e ');
    return `${names.slice(0, 2).join(', ')} e mais ${names.length - 2}`;
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    this.profileSaving.set(true);
    this.profileSaved.set(false);
    this.profileError.set(null);
    const { name, phone } = this.profileForm.getRawValue();
    this.auth.updateProfile({ name: name.trim(), phone: phone.trim() }).subscribe({
      next: (profile) => {
        this.profileSaving.set(false);
        this.profileForm.reset({ name: profile.name, phone: profile.phone ?? '' });
        this.profileSaved.set(true);
      },
      error: (err) => {
        this.profileSaving.set(false);
        this.profileError.set(httpErrorMessage(err, 'Não foi possível salvar seus dados.'));
      },
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    this.passwordSaving.set(true);
    this.passwordSaved.set(false);
    this.passwordError.set(null);
    const { currentPassword, newPassword } = this.passwordForm.getRawValue();
    this.auth.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.passwordSaving.set(false);
        this.passwordForm.reset();
        this.passwordSaved.set(true);
      },
      error: (err) => {
        this.passwordSaving.set(false);
        this.passwordError.set(httpErrorMessage(err, 'Não foi possível alterar a senha.', 'Senha atual incorreta.'));
      },
    });
  }

  resendVerification(): void {
    this.resendingVerification.set(true);
    this.verificationError.set(null);
    this.auth.resendVerification().subscribe({
      next: () => {
        this.resendingVerification.set(false);
        this.verificationSent.set(true);
      },
      error: (err) => {
        this.resendingVerification.set(false);
        this.verificationError.set(httpErrorMessage(err, 'Não foi possível reenviar agora. Tente de novo em instantes.'));
      },
    });
  }

  scrollToSection(id: string): void {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
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
    if (this.deleting()) return;
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
