import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, Injector, OnInit, afterNextRender, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, filter, map, of, startWith, switchMap, tap } from 'rxjs';
import { THREAD_COLORS } from '@shared/core/constants/thread-colors';
import { CustomerSummary } from '@shared/core/models/customer.model';
import { Product } from '@shared/core/models/product.model';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';
import { CepService } from '@shared/core/services/cep.service';
import { CustomerAdminService } from '@shared/core/services/customer-admin.service';
import { OrderService } from '@shared/core/services/order.service';
import { ProductService } from '@shared/core/services/product.service';
import { cpfValidator, phoneDigitsValidator } from '@shared/core/utils/br-documents';
import { formatCpf } from '@shared/core/utils/format-cpf';
import { httpErrorMessage } from '@shared/core/utils/http-error-message';
import { parseWhatsappOrderMessage } from '@shared/core/utils/whatsapp-order-parser';
import { CpfMaskDirective } from '@shared/shared/directives/cpf-mask.directive';
import { PhoneMaskDirective } from '@shared/shared/directives/phone-mask.directive';

type ItemGroup = FormGroup<{
  productId: FormControl<string>;
  productName: FormControl<string>;
  embroideryText: FormControl<string>;
  threadColor: FormControl<string>;
  quantity: FormControl<number>;
  unitPrice: FormControl<number>;
}>;

/**
 * "Registrar encomenda": orders closed on WhatsApp — the only way to buy while on-line payment is
 * off — or in person never reached the system, so they were missing from the dashboard, the orders
 * list, the customer's tracking and the status e-mails. The customer's pre-filled WhatsApp summary
 * can be pasted to fill the form (parseWhatsappOrderMessage).
 */
@Component({
  selector: 'app-admin-order-create',
  standalone: true,
  imports: [CurrencyPipe, FormsModule, ReactiveFormsModule, RouterLink, PhoneMaskDirective, CpfMaskDirective],
  templateUrl: './admin-order-create.html',
})
export class AdminOrderCreate implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly orderService = inject(OrderService);
  private readonly productService = inject(ProductService);
  private readonly customerAdmin = inject(CustomerAdminService);
  private readonly cepService = inject(CepService);
  private readonly router = inject(Router);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  readonly threadColors = THREAD_COLORS;
  readonly products = signal<Product[]>([]);
  readonly canSearchCustomers = inject(AdminAuthService).hasPermission('Customers');

  readonly customers = signal<CustomerSummary[] | null>(null);
  readonly customerQuery = signal('');
  readonly linkedCustomer = signal<CustomerSummary | null>(null);
  readonly customerMatches = computed(() => {
    const query = this.customerQuery().trim().toLowerCase();
    const digits = query.replace(/\D/g, '');
    if (query.length < 2) return [];
    return (this.customers() ?? [])
      .filter((c) => !c.isAnonymized)
      .filter((c) => c.name.toLowerCase().includes(query) || c.email.toLowerCase().includes(query)
        || (digits.length >= 4 && (c.phone ?? '').replace(/\D/g, '').includes(digits)))
      .slice(0, 6);
  });

  readonly pasteOpen = signal(false);
  readonly pasteText = signal('');
  readonly pasteSummary = signal<string | null>(null);
  readonly pasteWarnings = signal<string[]>([]);

  readonly saving = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly cepLoading = signal(false);
  readonly cepError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    customerName: ['', Validators.required],
    customerEmail: ['', [Validators.required, Validators.email]],
    customerPhone: ['', [Validators.required, phoneDigitsValidator]],
    customerCpf: ['', [Validators.required, cpfValidator]],
    deliveryMethod: this.fb.nonNullable.control<'Entrega' | 'Retirada'>('Entrega'),
    zipCode: [''],
    street: [''],
    number: [''],
    complement: [''],
    neighborhood: [''],
    city: [''],
    state: [''],
    shippingCost: [0, [Validators.required, Validators.min(0)]],
    paymentReceived: [false],
    notifyCustomer: [true],
    notes: [''],
    isGift: [false],
    recipientName: [''],
    giftMessage: [''],
    items: this.fb.array<ItemGroup>([this.newItem()]),
  });

  private readonly formValue = toSignal(
    this.form.valueChanges.pipe(map(() => this.form.getRawValue()), startWith(this.form.getRawValue())),
    { requireSync: true },
  );
  readonly deliveryMethod = computed(() => this.formValue().deliveryMethod);
  readonly itemsTotal = computed(() =>
    this.formValue().items.reduce((sum, item) => sum + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0), 0),
  );
  readonly total = computed(() => this.itemsTotal() + (this.deliveryMethod() === 'Entrega' ? Number(this.formValue().shippingCost) || 0 : 0));

  constructor() {
    // Address only matters when shipping.
    effect(() => {
      const required = this.deliveryMethod() === 'Entrega';
      const c = this.form.controls;
      for (const control of [c.zipCode, c.street, c.number, c.neighborhood, c.city, c.state]) {
        control.setValidators(required ? [Validators.required] : []);
        control.updateValueAndValidity({ emitEvent: false });
      }
    });
  }

  get items(): FormArray<ItemGroup> {
    return this.form.controls.items;
  }

  ngOnInit(): void {
    this.productService.listAllForAdmin(1, 200).subscribe({ next: (result) => this.products.set(result.items), error: () => {} });

    this.form.controls.zipCode.valueChanges
      .pipe(
        map((value) => value.replace(/\D/g, '')),
        distinctUntilChanged(),
        tap(() => this.cepError.set(null)),
        filter((digits) => digits.length === 8),
        tap(() => this.cepLoading.set(true)),
        debounceTime(300),
        switchMap((digits) => this.cepService.lookup(digits).pipe(catchError(() => of(null)))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((address) => {
        this.cepLoading.set(false);
        if (!address || address.erro) {
          this.cepError.set('CEP não encontrado — preencha o endereço manualmente.');
          return;
        }
        this.form.patchValue({ street: address.logradouro, neighborhood: address.bairro, city: address.localidade, state: address.uf });
      });
  }

  private newItem(data?: Partial<{ productId: string; productName: string; embroideryText: string; threadColor: string; quantity: number; unitPrice: number }>): ItemGroup {
    return this.fb.nonNullable.group({
      productId: [data?.productId ?? ''],
      productName: [data?.productName ?? '', Validators.required],
      embroideryText: [data?.embroideryText ?? ''],
      threadColor: [data?.threadColor ?? ''],
      quantity: [data?.quantity ?? 1, [Validators.required, Validators.min(1)]],
      unitPrice: [data?.unitPrice ?? 0, [Validators.required, Validators.min(0)]],
    });
  }

  addItem(): void {
    this.items.push(this.newItem());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) this.items.removeAt(index);
  }

  /** Picking a catalog product fills its name and current price (still editable — the agreed price wins). */
  onProductSelected(index: number, productId: string): void {
    const product = this.products().find((p) => p.id === productId);
    if (!product) return;
    this.items.at(index).patchValue({ productName: product.name, unitPrice: product.effectivePrice });
  }

  loadCustomers(): void {
    if (!this.canSearchCustomers || this.customers() !== null) return;
    this.customers.set([]);
    this.customerAdmin.list().subscribe({ next: (list) => this.customers.set(list), error: () => this.customers.set([]) });
  }

  selectCustomer(customer: CustomerSummary): void {
    this.linkedCustomer.set(customer);
    this.customerQuery.set('');
    this.form.patchValue({
      customerName: customer.name,
      customerEmail: customer.email,
      customerPhone: customer.phone ?? this.form.controls.customerPhone.value,
      customerCpf: customer.cpf ? formatCpf(customer.cpf) : this.form.controls.customerCpf.value,
    });
    const c = this.form.controls;
    if (!c.street.value && customer.addressStreet) {
      this.form.patchValue({
        zipCode: customer.addressZipCode ?? '',
        street: customer.addressStreet ?? '',
        number: customer.addressNumber ?? '',
        complement: customer.addressComplement ?? '',
        neighborhood: customer.addressNeighborhood ?? '',
        city: customer.addressCity ?? '',
        state: customer.addressState ?? '',
      });
    }
  }

  unlinkCustomer(): void {
    this.linkedCustomer.set(null);
  }

  applyPastedMessage(): void {
    const parsed = parseWhatsappOrderMessage(this.pasteText());
    const warnings: string[] = [];
    if (parsed.items.length === 0 && !parsed.customerName && !parsed.deliveryMethod) {
      this.pasteSummary.set(null);
      this.pasteWarnings.set(['Não reconheci um resumo de pedido nesse texto. Cole a mensagem que a cliente enviou pelo botão "Enviar pedido pelo WhatsApp" do site.']);
      return;
    }

    if (parsed.items.length > 0) {
      this.items.clear();
      for (const item of parsed.items) {
        const product = this.products().find((p) => p.name.trim().toLowerCase() === item.name.trim().toLowerCase());
        if (!product) warnings.push(`"${item.name}" não corresponde a um produto cadastrado — ficou como item avulso.`);
        this.items.push(this.newItem({
          productId: product?.id ?? '',
          productName: product?.name ?? item.name,
          embroideryText: item.embroideryText ?? '',
          threadColor: item.threadColor && THREAD_COLORS.includes(item.threadColor) ? item.threadColor : '',
          quantity: item.quantity,
          unitPrice: item.unitPrice,
        }));
      }
    }

    const patch: Record<string, unknown> = {};
    if (parsed.customerName) patch['customerName'] = parsed.customerName;
    if (parsed.customerEmail) patch['customerEmail'] = parsed.customerEmail;
    if (parsed.customerPhone) patch['customerPhone'] = parsed.customerPhone;
    if (parsed.deliveryMethod) patch['deliveryMethod'] = parsed.deliveryMethod;
    if (parsed.address) Object.assign(patch, parsed.address);
    if (parsed.shippingCost !== null) patch['shippingCost'] = parsed.shippingCost;
    if (parsed.isGift) Object.assign(patch, { isGift: true, recipientName: parsed.recipientName ?? '', giftMessage: parsed.giftMessage ?? '' });
    const notes = [parsed.notes, parsed.couponCode ? `Cliente usou o cupom ${parsed.couponCode} no site (−R$ ${parsed.couponDiscount?.toFixed(2).replace('.', ',')}).` : null]
      .filter(Boolean).join(' ');
    if (notes) patch['notes'] = notes;
    this.form.patchValue(patch);
    this.form.markAsDirty();

    if (parsed.couponCode) warnings.push(`A mensagem tem o cupom ${parsed.couponCode}: se o desconto vale, ajuste os preços dos itens (ficou anotado nas observações).`);
    if (parsed.deliveryMethod === 'Entrega' && !parsed.address) warnings.push('O endereço ficou "a combinar" — preencha antes de registrar.');
    if (parsed.deliveryMethod === 'Entrega' && parsed.shippingCost === null) warnings.push('A mensagem não tinha frete calculado — informe o valor combinado.');
    if (!this.form.controls.customerCpf.value) warnings.push('O resumo não traz o CPF — peça à cliente (é obrigatório no pedido).');

    // Same e-mail as a registered customer → link the order to her account ("Minhas encomendas").
    if (this.canSearchCustomers && parsed.customerEmail) {
      const link = (list: CustomerSummary[]) => {
        const match = list.find((c) => c.email.toLowerCase() === parsed.customerEmail!.toLowerCase() && !c.isAnonymized);
        if (match) this.selectCustomer(match);
      };
      if (this.customers()?.length) link(this.customers()!);
      else this.customerAdmin.list().subscribe({ next: (list) => { this.customers.set(list); link(list); }, error: () => {} });
    }

    this.pasteSummary.set(`Preenchido a partir da mensagem: ${parsed.items.length} ${parsed.items.length === 1 ? 'item' : 'itens'}. Confira os campos antes de registrar.`);
    this.pasteWarnings.set(warnings);
    this.pasteOpen.set(false);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      afterNextRender(() => {
        const field = document.querySelector<HTMLElement>('app-admin-order-create .is-invalid');
        field?.scrollIntoView({ behavior: 'smooth', block: 'center' });
        field?.focus({ preventScroll: true });
      }, { injector: this.injector });
      return;
    }

    const v = this.form.getRawValue();
    const delivery = v.deliveryMethod;
    this.saving.set(true);
    this.saveError.set(null);
    this.orderService
      .createManual({
        customerId: this.linkedCustomer()?.id ?? null,
        customerName: v.customerName.trim(),
        customerEmail: v.customerEmail.trim(),
        customerPhone: v.customerPhone.trim(),
        customerCpf: v.customerCpf,
        deliveryMethod: delivery,
        shippingAddressJson: delivery === 'Entrega'
          ? JSON.stringify({ street: v.street, number: v.number, complement: v.complement || null, neighborhood: v.neighborhood, city: v.city, state: v.state.toUpperCase(), zipCode: v.zipCode })
          : null,
        shippingCost: delivery === 'Entrega' ? Number(v.shippingCost) || 0 : 0,
        items: v.items.map((item) => ({
          productId: item.productId || null,
          productName: item.productName.trim(),
          unitPrice: Number(item.unitPrice) || 0,
          quantity: Number(item.quantity) || 1,
          optionsJson: item.embroideryText.trim() || item.threadColor
            ? JSON.stringify({ embroideryText: item.embroideryText.trim() || undefined, threadColor: item.threadColor || undefined })
            : null,
        })),
        paymentReceived: v.paymentReceived,
        notifyCustomer: v.notifyCustomer,
        notes: v.notes.trim() || null,
        giftMessage: v.isGift ? v.giftMessage.trim() || null : null,
        recipientName: v.isGift ? v.recipientName.trim() || null : null,
      })
      .subscribe({
        next: (order) => {
          this.form.markAsPristine();
          this.router.navigate(['/admin/encomendas', order.id], { state: { created: true } });
        },
        error: (err) => {
          this.saving.set(false);
          this.saveError.set(httpErrorMessage(err, 'Não foi possível registrar a encomenda.'));
        },
      });
  }
}
