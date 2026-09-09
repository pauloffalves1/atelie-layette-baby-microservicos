import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, switchMap, tap } from 'rxjs';
import { AuthService } from '@shared/core/services/auth.service';
import { CartService } from '@shared/core/services/cart.service';
import { CepService } from '@shared/core/services/cep.service';
import { CouponService } from '@shared/core/services/coupon.service';
import { OrderService } from '@shared/core/services/order.service';
import { ShippingService } from '@shared/core/services/shipping.service';
import { ShippingAddress } from '@shared/core/models/order.model';
import { PhoneMaskDirective } from '@shared/shared/directives/phone-mask.directive';

declare const PagSeguro: {
  encryptCard(options: {
    publicKey: string;
    holder: string;
    number: string;
    expMonth: string;
    expYear: string;
    securityCode: string;
  }): { encryptedCard?: string; hasErrors: boolean; errors?: { code: string; message: string }[] };
};

const PAGBANK_SDK_URL = 'https://assets.pagseguro.com.br/checkout-sdk-js/rc/dist/browser/pagseguro.min.js';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CurrencyPipe, ReactiveFormsModule, RouterLink, PhoneMaskDirective],
  templateUrl: './checkout.html',
})
export class Checkout implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cepService = inject(CepService);
  private readonly shippingService = inject(ShippingService);
  private readonly couponService = inject(CouponService);

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly cepLoading = signal(false);
  readonly cepError = signal<string | null>(null);
  readonly destinationState = signal('');
  readonly destinationCity = signal('');

  readonly paymentMethod = signal<'PIX' | 'CREDIT_CARD'>('PIX');
  readonly cardPublicKey = signal<string | null>(null);
  readonly cardSdkReady = signal(false);

  readonly couponCode = signal('');
  readonly couponApplying = signal(false);
  readonly couponError = signal<string | null>(null);
  readonly couponDiscountAmount = signal(0);
  readonly appliedCouponCode = signal<string | null>(null);

  readonly shippingCost = computed(() =>
    this.shippingService.estimate(
      this.destinationState(),
      this.cart.totalItems(),
      this.cart.totalPrice(),
      this.destinationCity(),
    ),
  );

  readonly freeShippingThreshold = computed(() =>
    this.destinationState()
      ? this.shippingService.freeShippingThreshold(this.destinationState(), this.destinationCity())
      : null,
  );

  readonly freeShippingRemaining = computed(() => {
    const threshold = this.freeShippingThreshold();
    return threshold === null ? null : Math.max(0, threshold - this.cart.totalPrice());
  });

  readonly total = computed(() => Math.max(0, this.cart.totalPrice() + this.shippingCost() - this.couponDiscountAmount()));

  readonly form = this.fb.nonNullable.group({
    customerName: ['', Validators.required],
    customerEmail: ['', [Validators.required, Validators.email]],
    customerPhone: ['', Validators.required],
    customerCpf: ['', [Validators.required, Validators.pattern(/^\d{3}\.?\d{3}\.?\d{3}-?\d{2}$/)]],
    zipCode: ['', Validators.required],
    street: ['', Validators.required],
    number: ['', Validators.required],
    complement: [''],
    neighborhood: ['', Validators.required],
    city: ['', Validators.required],
    state: ['', Validators.required],
    notes: [''],
    isGift: [false],
    giftMessage: [''],
    cardNumber: [''],
    cardHolder: [''],
    cardExpiry: [''],
    cardCvv: [''],
    installments: [1],
  });

  constructor(
    readonly cart: CartService,
    private readonly orderService: OrderService,
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    if (this.cart.items().length === 0) {
      this.router.navigate(['/carrinho']);
      return;
    }

    const user = this.auth.currentUser();
    if (user) {
      this.form.patchValue({ customerName: user.name, customerEmail: user.email });

      this.auth.getProfile().subscribe((profile) => {
        this.form.patchValue({
          customerName: profile.name,
          customerEmail: profile.email,
          customerPhone: profile.phone ?? '',
          customerCpf: profile.cpf ?? '',
        });
      });

      this.orderService.listMine().subscribe((orders) => {
        const lastWithAddress = orders.find((o) => o.shippingAddressJson);
        if (!lastWithAddress?.shippingAddressJson) return;

        const address = JSON.parse(lastWithAddress.shippingAddressJson) as ShippingAddress;
        this.form.patchValue({
          zipCode: address.zipCode,
          street: address.street,
          number: address.number,
          complement: address.complement ?? '',
          neighborhood: address.neighborhood,
          city: address.city,
          state: address.state,
        });
      });
    }

    this.form.controls.state.valueChanges.subscribe((state) => this.destinationState.set(state));
    this.form.controls.city.valueChanges.subscribe((city) => this.destinationCity.set(city));

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

    this.loadPagBankSdk();
    this.orderService.getCardEncryptionPublicKey().subscribe({
      next: ({ publicKey }) => this.cardPublicKey.set(publicKey),
      error: () => this.cardPublicKey.set(null),
    });
  }

  private loadPagBankSdk(): void {
    if (document.querySelector(`script[src="${PAGBANK_SDK_URL}"]`)) {
      this.cardSdkReady.set(true);
      return;
    }

    const script = document.createElement('script');
    script.src = PAGBANK_SDK_URL;
    script.onload = () => this.cardSdkReady.set(true);
    document.body.appendChild(script);
  }

  applyCoupon(): void {
    const code = this.couponCode().trim();
    if (!code) return;

    this.couponApplying.set(true);
    this.couponError.set(null);

    this.couponService.validate(code, this.cart.totalPrice()).subscribe({
      next: (result) => {
        this.couponApplying.set(false);
        if (!result.valid) {
          this.couponError.set(result.error ?? 'Cupom inválido.');
          this.couponDiscountAmount.set(0);
          this.appliedCouponCode.set(null);
          return;
        }
        this.couponDiscountAmount.set(result.discountAmount);
        this.appliedCouponCode.set(code.toUpperCase());
      },
      error: (err) => {
        this.couponApplying.set(false);
        this.couponError.set(err?.error?.detail ?? 'Não foi possível validar o cupom.');
      },
    });
  }

  removeCoupon(): void {
    this.couponCode.set('');
    this.couponDiscountAmount.set(0);
    this.appliedCouponCode.set(null);
    this.couponError.set(null);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    if (this.paymentMethod() === 'CREDIT_CARD') {
      const publicKey = this.cardPublicKey();
      if (!this.cardSdkReady() || !publicKey || typeof PagSeguro === 'undefined') {
        this.errorMessage.set('O pagamento com cartão ainda está carregando. Aguarde alguns instantes e tente novamente.');
        return;
      }

      const [expMonth, expYear] = (value.cardExpiry || '').split('/').map((part) => part.trim());
      const card = PagSeguro.encryptCard({
        publicKey,
        holder: value.cardHolder,
        number: (value.cardNumber || '').replace(/\D/g, ''),
        expMonth: expMonth || '',
        expYear: expYear?.length === 2 ? `20${expYear}` : expYear || '',
        securityCode: value.cardCvv,
      });

      if (card.hasErrors || !card.encryptedCard) {
        this.errorMessage.set('Dados do cartão inválidos. Confira o número, validade e CVV.');
        return;
      }

      this.submitOrder(value, 'CREDIT_CARD', card.encryptedCard, Number(value.installments) || 1);
    } else {
      this.submitOrder(value, 'PIX');
    }
  }

  private submitOrder(
    value: ReturnType<typeof this.form.getRawValue>,
    paymentMethod: 'PIX' | 'CREDIT_CARD',
    encryptedCard?: string,
    installments?: number,
  ): void {
    this.submitting.set(true);
    this.errorMessage.set(null);

    const shippingAddress = {
      street: value.street,
      number: value.number,
      complement: value.complement || null,
      neighborhood: value.neighborhood,
      city: value.city,
      state: value.state,
      zipCode: value.zipCode,
    };

    this.orderService
      .createStoreOrder({
        customerName: value.customerName,
        customerEmail: value.customerEmail,
        customerPhone: value.customerPhone || null,
        customerCpf: value.customerCpf,
        notes: value.notes || null,
        shippingAddressJson: JSON.stringify(shippingAddress),
        shippingCost: this.shippingCost(),
        couponCode: this.appliedCouponCode(),
        paymentMethod,
        encryptedCard,
        installments,
        giftMessage: value.isGift && value.giftMessage ? value.giftMessage : null,
        items: this.cart.items().map((item) => ({
          productId: item.product.id,
          productName: item.product.name,
          unitPrice: item.product.effectivePrice,
          quantity: item.quantity,
          optionsJson:
            item.embroideryText || item.threadColor
              ? JSON.stringify({ embroideryText: item.embroideryText ?? undefined, threadColor: item.threadColor ?? undefined })
              : null,
        })),
      })
      .subscribe({
        next: (order) => {
          if (paymentMethod === 'CREDIT_CARD' && order.paymentStatus === 'Recusado') {
            this.submitting.set(false);
            this.errorMessage.set(
              order.paymentDeclineReason
                ? `Pagamento recusado: ${order.paymentDeclineReason}. Confira os dados do cartão ou tente outro cartão.`
                : 'Pagamento recusado pela operadora do cartão. Confira os dados ou tente outro cartão.',
            );
            return;
          }

          this.cart.clear();
          this.router.navigate(['/pedido', order.id]);
        },
        error: (err) => {
          this.submitting.set(false);
          this.errorMessage.set(err?.error?.detail ?? 'Não foi possível finalizar o pedido. Tente novamente.');
        },
      });
  }
}
