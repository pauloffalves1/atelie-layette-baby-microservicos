import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Coupon } from '@shared/core/models/coupon.model';
import { CouponService } from '@shared/core/services/coupon.service';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';
import { LoadError } from '@shared/shared/components/load-error/load-error';

/** A coupon that expires before it could ever be used is almost always a typo in the date. */
function notInThePast(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string;
  return value && new Date(value).getTime() <= Date.now() ? { past: true } : null;
}

export type CouponState = { label: string; tone: 'success' | 'secondary' | 'warning'; hint?: string };

@Component({
  selector: 'app-admin-coupon-list',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe, LoadError],
  templateUrl: './admin-coupon-list.html',
})
export class AdminCouponList implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly couponService = inject(CouponService);

  readonly coupons = signal<Coupon[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly creating = signal(false);
  readonly createError = signal<string | null>(null);
  readonly created = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly togglingId = signal<string | null>(null);
  readonly copiedCode = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    // Same rule as Coupon.Create on the backend (letters and digits only), so it's caught while typing.
    code: ['', [Validators.required, Validators.pattern(/^\s*[A-Za-z0-9]+\s*$/)]],
    discountPercentage: [10, [Validators.required, Validators.min(1), Validators.max(99)]],
    expiresAt: ['', notInThePast],
    maxUses: [null as number | null, Validators.min(1)],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.couponService.list().subscribe({
      next: (coupons) => {
        this.coupons.set(coupons);
        this.loading.set(false);
      },
      error: () => {
          this.loading.set(false);
          this.loadError.set(true);
        },
    });
  }

  /** Why a coupon is or isn't usable — "Inválido" alone left the admin guessing. */
  state(coupon: Coupon): CouponState {
    if (!coupon.active) return { label: 'Desativado', tone: 'secondary', hint: 'Não pode ser usado até ser ativado de novo.' };
    if (coupon.expiresAt && new Date(coupon.expiresAt).getTime() <= Date.now()) {
      return { label: 'Expirado', tone: 'secondary', hint: 'A data de validade já passou.' };
    }
    if (coupon.maxUses && coupon.usesCount >= coupon.maxUses) {
      return { label: 'Esgotado', tone: 'warning', hint: 'Atingiu o limite de usos.' };
    }
    if (!coupon.isValid) return { label: 'Indisponível', tone: 'secondary' };
    return { label: 'Válido', tone: 'success' };
  }

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const code = value.code.trim().toUpperCase();
    this.creating.set(true);
    this.createError.set(null);
    this.created.set(null);

    this.couponService
      .create({
        code,
        discountPercentage: value.discountPercentage,
        expiresAt: value.expiresAt ? new Date(value.expiresAt).toISOString() : null,
        maxUses: value.maxUses || null,
      })
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.created.set(code);
          this.form.reset({ code: '', discountPercentage: 10, expiresAt: '', maxUses: null });
          this.load();
        },
        error: (err) => {
          this.creating.set(false);
          this.createError.set(httpErrorMessage(err, 'Não foi possível criar o cupom.'));
        },
      });
  }

  toggleActive(coupon: Coupon): void {
    if (this.togglingId()) return;
    this.togglingId.set(coupon.id);
    this.actionError.set(null);
    this.couponService.setActive(coupon.id, !coupon.active).subscribe({
      next: () => {
        this.togglingId.set(null);
        this.load();
      },
      error: (err) => {
        this.togglingId.set(null);
        this.actionError.set(httpErrorMessage(err, `Não foi possível ${coupon.active ? 'desativar' : 'ativar'} o cupom ${coupon.code}.`));
      },
    });
  }

  copyCode(code: string): void {
    navigator.clipboard.writeText(code).then(() => {
      this.copiedCode.set(code);
      setTimeout(() => this.copiedCode.set(null), 2000);
    });
  }
}
