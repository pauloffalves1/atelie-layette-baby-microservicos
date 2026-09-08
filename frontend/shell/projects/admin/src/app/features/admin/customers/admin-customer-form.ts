import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap, tap } from 'rxjs';
import { CepService } from '@shared/core/services/cep.service';
import { CustomerAdminService } from '@shared/core/services/customer-admin.service';
import { PhoneMaskDirective } from '@shared/shared/directives/phone-mask.directive';

@Component({
  selector: 'app-admin-customer-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PhoneMaskDirective],
  templateUrl: './admin-customer-form.html',
})
export class AdminCustomerForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly customerAdminService = inject(CustomerAdminService);
  private readonly cepService = inject(CepService);

  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly cepLoading = signal(false);
  readonly cepError = signal<string | null>(null);

  private customerId!: string;

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    cpf: ['', [Validators.required, Validators.pattern(/^\d{3}\.?\d{3}\.?\d{3}-?\d{2}$/)]],
    phone: ['', Validators.required],
    zipCode: [''],
    street: [''],
    number: [''],
    complement: [''],
    neighborhood: [''],
    city: [''],
    state: [''],
  });

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id')!;

    this.customerAdminService.getById(this.customerId).subscribe({
      next: (customer) => {
        this.form.patchValue({
          name: customer.name,
          email: customer.email,
          cpf: customer.cpf ?? '',
          phone: customer.phone ?? '',
          zipCode: customer.addressZipCode ?? '',
          street: customer.addressStreet ?? '',
          number: customer.addressNumber ?? '',
          complement: customer.addressComplement ?? '',
          neighborhood: customer.addressNeighborhood ?? '',
          city: customer.addressCity ?? '',
          state: customer.addressState ?? '',
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });

    this.form.controls.zipCode.valueChanges
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

        this.form.patchValue({
          street: address.logradouro,
          neighborhood: address.bairro,
          city: address.localidade,
          state: address.uf,
        });
      });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.errorMessage.set(null);

    this.customerAdminService
      .update(this.customerId, {
        name: value.name,
        email: value.email,
        cpf: value.cpf,
        phone: value.phone || null,
        addressStreet: value.street || null,
        addressNumber: value.number || null,
        addressComplement: value.complement || null,
        addressNeighborhood: value.neighborhood || null,
        addressCity: value.city || null,
        addressState: value.state || null,
        addressZipCode: value.zipCode || null,
      })
      .subscribe({
        next: () => this.router.navigate(['/admin/clientes']),
        error: (err) => {
          this.submitting.set(false);
          this.errorMessage.set(err?.error?.detail ?? 'Não foi possível salvar os dados do cliente.');
        },
      });
  }
}
