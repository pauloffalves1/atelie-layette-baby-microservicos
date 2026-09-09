import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, filter, firstValueFrom, map, of, switchMap, tap } from 'rxjs';
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
  setUp(options: { session: string; env: 'SANDBOX' | 'PROD' }): void;
  authenticate3DS(request: {
    data: {
      customer: {
        name: string;
        email: string;
        phones: { country: string; area: string; number: string; type: string }[];
      };
      paymentMethod: {
        type: 'CREDIT_CARD';
        installments: number;
        card: { number: string; expMonth: string; expYear: string; holder: { name: string } };
      };
      amount: { value: number; currency: string };
      billingAddress: { street: string; number: string; regionCode: string; country: string; city: string; postalCode: string };
      dataOnly: boolean;
    };
  }): Promise<{ status: string; authenticationStatus?: string; id?: string }>;
};

interface ParsedPhone {
  country: string;
  area: string;
  number: string;
  type: string;
}

function parsePhone(phone: string): ParsedPhone | null {
  const digits = phone.replace(/\D/g, '');
  return digits.length < 10 ? null : { country: '55', area: digits.slice(0, 2), number: digits.slice(2), type: 'MOBILE' };
}

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

  readonly deliveryMethod = signal<'Entrega' | 'Retirada'>('Entrega');

  readonly couponCode = signal('');
  readonly couponApplying = signal(false);
  readonly couponError = signal<string | null>(null);
  readonly couponDiscountAmount = signal(0);
  readonly appliedCouponCode = signal<string | null>(null);

  readonly shippingCost = computed(() =>
    this.deliveryMethod() === 'Retirada'
      ? 0
      : this.shippingService.estimate(this.destinationState(), this.cart.totalItems(), this.cart.totalPrice(), this.destinationCity()),
  );

  readonly freeShippingThreshold = computed(() =>
    this.deliveryMethod() === 'Entrega' && this.destinationState()
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
  ) {
    // Address fields only matter (and only need to validate) when the order is actually shipped —
    // "Retirada" skips them entirely so the form doesn't stay stuck invalid over a blank address.
    effect(() => {
      const addressControls = [
        this.form.controls.zipCode,
        this.form.controls.street,
        this.form.controls.number,
        this.form.controls.neighborhood,
        this.form.controls.city,
        this.form.controls.state,
      ];
      const validators = this.deliveryMethod() === 'Entrega' ? [Validators.required] : [];
      addressControls.forEach((control) => {
        control.setValidators(validators);
        control.updateValueAndValidity({ emitEvent: false });
      });
    });
  }

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

      const [expMonth, expYearRaw] = (value.cardExpiry || '').split('/').map((part) => part.trim());
      const expYear = expYearRaw?.length === 2 ? `20${expYearRaw}` : expYearRaw || '';
      const cardNumber = (value.cardNumber || '').replace(/\D/g, '');

      const card = PagSeguro.encryptCard({
        publicKey,
        holder: value.cardHolder,
        number: cardNumber,
        expMonth: expMonth || '',
        expYear,
        securityCode: value.cardCvv,
      });

      if (card.hasErrors || !card.encryptedCard) {
        this.errorMessage.set('Dados do cartão inválidos. Confira o número, validade e CVV.');
        return;
      }

      this.authenticate3ds(value, cardNumber, expMonth || '', expYear)
        .then((threeDsAuthenticationId) =>
          this.submitOrder(value, 'CREDIT_CARD', card.encryptedCard, Number(value.installments) || 1, threeDsAuthenticationId),
        )
        // 3DS is a fraud-liability nicety, not a hard requirement to charge — any failure in the
        // authentication step itself (not the challenge outcome) still lets checkout proceed.
        .catch(() => this.submitOrder(value, 'CREDIT_CARD', card.encryptedCard, Number(value.installments) || 1));
    } else {
      this.submitOrder(value, 'PIX');
    }
  }

  /** Resolves to the 3DS authentication id on success, or null if the challenge wasn't completed — either way checkout proceeds. */
  private async authenticate3ds(
    value: ReturnType<typeof this.form.getRawValue>,
    cardNumber: string,
    expMonth: string,
    expYear: string,
  ): Promise<string | null> {
    const { session, environment } = await firstValueFrom(this.orderService.getThreeDsSession());
    PagSeguro.setUp({ session, env: environment });

    const phone = parsePhone(value.customerPhone);
    const result = await PagSeguro.authenticate3DS({
      data: {
        customer: { name: value.customerName, email: value.customerEmail, phones: phone ? [phone] : [] },
        paymentMethod: {
          type: 'CREDIT_CARD',
          installments: Number(value.installments) || 1,
          card: { number: cardNumber, expMonth, expYear, holder: { name: value.cardHolder } },
        },
        amount: { value: Math.round(this.total() * 100), currency: 'BRL' },
        billingAddress: {
          street: value.street,
          number: value.number,
          regionCode: value.state,
          country: 'BRA',
          city: value.city,
          postalCode: value.zipCode.replace(/\D/g, ''),
        },
        dataOnly: false,
      },
    });

    return result.status === 'AUTH_FLOW_COMPLETED' && result.authenticationStatus === 'AUTHENTICATED' ? result.id ?? null : null;
  }

  private submitOrder(
    value: ReturnType<typeof this.form.getRawValue>,
    paymentMethod: 'PIX' | 'CREDIT_CARD',
    encryptedCard?: string,
    installments?: number,
    threeDsAuthenticationId?: string | null,
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
        shippingAddressJson: this.deliveryMethod() === 'Retirada' ? null : JSON.stringify(shippingAddress),
        shippingCost: this.shippingCost(),
        deliveryMethod: this.deliveryMethod(),
        couponCode: this.appliedCouponCode(),
        paymentMethod,
        encryptedCard,
        installments,
        giftMessage: value.isGift && value.giftMessage ? value.giftMessage : null,
        threeDsAuthenticationId,
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
