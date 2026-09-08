import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Coupon } from '@shared/core/models/coupon.model';
import { CouponService } from '@shared/core/services/coupon.service';

@Component({
  selector: 'app-admin-coupon-list',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './admin-coupon-list.html',
})
export class AdminCouponList implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly couponService = inject(CouponService);

  readonly coupons = signal<Coupon[]>([]);
  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly createError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    code: ['', Validators.required],
    discountPercentage: [10, [Validators.required, Validators.min(1), Validators.max(99)]],
    expiresAt: [''],
    maxUses: [null as number | null],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.couponService.list().subscribe({
      next: (coupons) => {
        this.coupons.set(coupons);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.creating.set(true);
    this.createError.set(null);

    this.couponService
      .create({
        code: value.code,
        discountPercentage: value.discountPercentage,
        expiresAt: value.expiresAt ? new Date(value.expiresAt).toISOString() : null,
        maxUses: value.maxUses || null,
      })
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.form.reset({ code: '', discountPercentage: 10, expiresAt: '', maxUses: null });
          this.load();
        },
        error: (err) => {
          this.creating.set(false);
          this.createError.set(err?.error?.detail ?? 'Não foi possível criar o cupom.');
        },
      });
  }

  toggleActive(coupon: Coupon): void {
    this.couponService.setActive(coupon.id, !coupon.active).subscribe(() => this.load());
  }
}
