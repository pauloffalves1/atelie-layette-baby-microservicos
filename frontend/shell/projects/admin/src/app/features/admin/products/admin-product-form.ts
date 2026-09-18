import { Component, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';
import { CustomerSummary } from '@shared/core/models/customer.model';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';
import { CustomerAdminService } from '@shared/core/services/customer-admin.service';
import { ProductService } from '@shared/core/services/product.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';
import { LoadError } from '@shared/shared/components/load-error/load-error';

/** ISO datetime -> `datetime-local` input value (local time, no seconds), or '' when absent. */
function toDatetimeLocal(iso: string | null): string {
  if (!iso) return '';
  const d = new Date(iso);
  const pad = (n: number) => n.toString().padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

@Component({
  selector: 'app-admin-product-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LoadError],
  templateUrl: './admin-product-form.html',
})
export class AdminProductForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AdminAuthService);

  /** Approving a test product (RF40) needs the "Testes" permission — without it the switch isn't shown, matching what the API accepts. */
  readonly canApproveTestProducts = computed(() => this.auth.hasPermission('Testing'));

  readonly isEditMode = signal(false);
  readonly loading = signal(false);
  readonly loadError = signal(false);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly customers = signal<CustomerSummary[]>([]);
  readonly selectedCustomerIds = signal<string[]>([]);
  readonly savingCustomers = signal(false);
  readonly customersSaved = signal(false);
  readonly uploadingImage = signal(false);
  readonly imageUploadError = signal<string | null>(null);
  readonly generatingDescription = signal(false);
  readonly generateDescriptionError = signal<string | null>(null);

  readonly galleryImages = signal<string[]>([]);
  readonly uploadingGalleryImage = signal(false);
  readonly galleryError = signal<string | null>(null);
  readonly savingGallery = signal(false);
  readonly gallerySaved = signal(false);

  readonly isOnPromotion = signal(false);
  readonly promotionDiscount = signal<number | null>(null);
  readonly promotionStartsAt = signal('');
  readonly promotionEndsAt = signal('');
  readonly savingPromotion = signal(false);
  readonly promotionSaved = signal(false);
  readonly promotionError = signal<string | null>(null);

  private productId: string | null = null;
  private leavingAfterSave = false;

  readonly justCreated = signal(false);
  readonly categories = signal<string[]>([]);

  /** Snapshots of what the server has, to tell whether each separately-saved section was edited. */
  private readonly savedGallery = signal<string[]>([]);
  private readonly savedCustomerIds = signal<string[]>([]);
  private readonly savedPromotion = signal({ discount: null as number | null, startsAt: '', endsAt: '' });

  readonly galleryDirty = computed(() => this.galleryImages().join('|') !== this.savedGallery().join('|'));
  readonly customersDirty = computed(
    () => [...this.selectedCustomerIds()].sort().join('|') !== [...this.savedCustomerIds()].sort().join('|'),
  );
  readonly promotionDirty = computed(() => {
    const saved = this.savedPromotion();
    return (
      (this.promotionDiscount() || null) !== (saved.discount || null) ||
      this.promotionStartsAt() !== saved.startsAt ||
      this.promotionEndsAt() !== saved.endsAt
    );
  });

  /** Typed category that no active product uses yet — likely a typo of an existing one. */
  readonly isNewCategory = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    category: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    imageUrl: [''],
    description: [''],
    featured: [false],
    productionLeadTimeDays: [null as number | null],
    isTest: [false],
  });

  /** Last saved value of the test-product switch (RF40) — it has its own endpoint, so it's only sent when it actually changed. */
  private readonly savedIsTest = signal(false);

  constructor(
    private readonly productService: ProductService,
    private readonly customerAdminService: CustomerAdminService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.customerAdminService.list().subscribe((customers) => this.customers.set(customers));
    this.productService.listCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.updateNewCategoryHint();
      },
      error: () => {},
    });
    this.form.controls.category.valueChanges.subscribe(() => this.updateNewCategoryHint());

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.productId = id;
    this.isEditMode.set(true);
    this.justCreated.set(this.route.snapshot.queryParamMap.has('criado'));
    this.load();
  }

  /** Edit mode only. The form stays hidden until the product loads — a blank form after a failed
   * load could otherwise be saved over the real product. */
  load(): void {
    if (!this.productId) return;
    this.loading.set(true);
    this.loadError.set(false);

    this.productService.getById(this.productId).subscribe({
      next: (product) => {
        this.form.patchValue({
          name: product.name,
          category: product.category,
          price: product.price,
          imageUrl: product.imageUrl ?? '',
          description: product.description ?? '',
          featured: product.featured,
          productionLeadTimeDays: product.productionLeadTimeDays,
          isTest: product.isTest ?? false,
        });
        this.savedIsTest.set(product.isTest ?? false);
        this.form.markAsPristine();
        this.selectedCustomerIds.set(product.allowedCustomerIds);
        this.savedCustomerIds.set(product.allowedCustomerIds);
        this.galleryImages.set(product.imageUrls);
        this.savedGallery.set(product.imageUrls);
        this.isOnPromotion.set(product.isOnPromotion);
        this.promotionDiscount.set(product.discountPercentage);
        this.promotionStartsAt.set(toDatetimeLocal(product.promotionStartsAt));
        this.promotionEndsAt.set(toDatetimeLocal(product.promotionEndsAt));
        this.savedPromotion.set({
          discount: product.discountPercentage,
          startsAt: toDatetimeLocal(product.promotionStartsAt),
          endsAt: toDatetimeLocal(product.promotionEndsAt),
        });
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      },
    });
  }

  private updateNewCategoryHint(): void {
    const typed = this.form.controls.category.value.trim().toLocaleLowerCase('pt-BR');
    this.isNewCategory.set(
      typed.length > 0 && this.categories().length > 0 && !this.categories().some((c) => c.toLocaleLowerCase('pt-BR') === typed),
    );
  }

  moveGalleryImage(index: number, delta: -1 | 1): void {
    this.galleryImages.update((urls) => {
      const target = index + delta;
      if (target < 0 || target >= urls.length) return urls;
      const next = [...urls];
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
  }

  previewUrl(): string {
    return resolveAssetUrl(this.form.value.imageUrl || '');
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingImage.set(true);
    this.imageUploadError.set(null);

    this.productService.uploadImage(file).subscribe({
      next: ({ url }) => {
        this.form.patchValue({ imageUrl: url });
        this.form.markAsDirty();
        this.uploadingImage.set(false);
      },
      error: (err) => {
        this.uploadingImage.set(false);
        this.imageUploadError.set(err?.error?.detail ?? 'Não foi possível enviar a imagem.');
      },
    });

    input.value = '';
  }

  generateDescription(): void {
    const { name, category } = this.form.getRawValue();
    if (!name || !category) {
      this.generateDescriptionError.set('Preencha nome e categoria antes de gerar a descrição.');
      return;
    }

    this.generatingDescription.set(true);
    this.generateDescriptionError.set(null);

    this.productService.generateDescription(name, category).subscribe({
      next: ({ description }) => {
        this.form.patchValue({ description });
        this.form.markAsDirty();
        this.generatingDescription.set(false);
      },
      error: () => {
        this.generatingDescription.set(false);
        this.generateDescriptionError.set('Não foi possível gerar a descrição agora. Tente de novo em instantes.');
      },
    });
  }

  toggleCustomer(customerId: string, checked: boolean): void {
    const current = this.selectedCustomerIds();
    this.selectedCustomerIds.set(
      checked ? [...current, customerId] : current.filter((id) => id !== customerId),
    );
  }

  onGalleryImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingGalleryImage.set(true);
    this.galleryError.set(null);

    this.productService.uploadImage(file).subscribe({
      next: ({ url }) => {
        this.galleryImages.update((urls) => [...urls, url]);
        this.uploadingGalleryImage.set(false);
      },
      error: (err) => {
        this.uploadingGalleryImage.set(false);
        this.galleryError.set(err?.error?.detail ?? 'Não foi possível enviar a imagem.');
      },
    });

    input.value = '';
  }

  removeGalleryImage(index: number): void {
    this.galleryImages.update((urls) => urls.filter((_, i) => i !== index));
  }

  saveGallery(): void {
    if (!this.productId) return;

    this.savingGallery.set(true);
    this.gallerySaved.set(false);
    const images = this.galleryImages();
    this.productService.setImages(this.productId, images).subscribe({
      next: () => {
        this.savedGallery.set(images);
        this.savingGallery.set(false);
        this.gallerySaved.set(true);
        setTimeout(() => this.gallerySaved.set(false), 2500);
      },
      error: () => this.savingGallery.set(false),
    });
  }

  resolveGalleryUrl(url: string): string {
    return resolveAssetUrl(url);
  }

  saveCustomerAccess(): void {
    if (!this.productId) return;

    this.savingCustomers.set(true);
    this.customersSaved.set(false);
    const customerIds = this.selectedCustomerIds();
    this.productService.setAllowedCustomers(this.productId, customerIds).subscribe({
      next: () => {
        this.savedCustomerIds.set(customerIds);
        this.savingCustomers.set(false);
        this.customersSaved.set(true);
        setTimeout(() => this.customersSaved.set(false), 2500);
      },
      error: () => this.savingCustomers.set(false),
    });
  }

  savePromotion(): void {
    if (!this.productId) return;

    if (!this.promotionDiscount() || !this.promotionStartsAt() || !this.promotionEndsAt()) {
      this.promotionError.set('Preencha o desconto e o período (início e fim) da promoção.');
      return;
    }

    this.savingPromotion.set(true);
    this.promotionError.set(null);

    this.productService
      .setPromotion(this.productId, {
        discountPercentage: this.promotionDiscount(),
        startsAt: new Date(this.promotionStartsAt()).toISOString(),
        endsAt: new Date(this.promotionEndsAt()).toISOString(),
      })
      .subscribe({
        next: (product) => {
          this.savingPromotion.set(false);
          this.savedPromotion.set({ discount: this.promotionDiscount(), startsAt: this.promotionStartsAt(), endsAt: this.promotionEndsAt() });
          this.isOnPromotion.set(product.isOnPromotion);
          this.promotionSaved.set(true);
          setTimeout(() => this.promotionSaved.set(false), 2500);
        },
        error: (err) => {
          this.savingPromotion.set(false);
          this.promotionError.set(err?.error?.detail ?? 'Não foi possível salvar a promoção.');
        },
      });
  }

  clearPromotion(): void {
    if (!this.productId) return;

    this.savingPromotion.set(true);
    this.promotionError.set(null);

    this.productService.setPromotion(this.productId, { discountPercentage: null, startsAt: null, endsAt: null }).subscribe({
      next: () => {
        this.savingPromotion.set(false);
        this.isOnPromotion.set(false);
        this.promotionDiscount.set(null);
        this.promotionStartsAt.set('');
        this.promotionEndsAt.set('');
        this.savedPromotion.set({ discount: null, startsAt: '', endsAt: '' });
      },
      error: (err) => {
        this.savingPromotion.set(false);
        this.promotionError.set(err?.error?.detail ?? 'Não foi possível remover a promoção.');
      },
    });
  }

  /** Anything the admin changed that no save button has persisted yet — guards leaving the page. */
  hasUnsavedChanges(): boolean {
    return !this.leavingAfterSave && (this.form.dirty || this.galleryDirty() || this.customersDirty() || this.promotionDirty());
  }

  /** Route `canDeactivate` hook (see app.routes.ts). */
  confirmLeave(): boolean {
    return !this.hasUnsavedChanges() || confirm('Há alterações não salvas neste produto. Sair mesmo assim e descartá-las?');
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) event.preventDefault();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    // Promotion needs its own validation (all three fields or none), so it isn't folded into this
    // save like gallery/access are — but silently dropping a half-edited promotion is worse.
    if (this.promotionDirty()) {
      this.errorMessage.set('A promoção tem alterações não salvas — clique em "Salvar promoção" (ou "Remover") antes de salvar o produto.');
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.errorMessage.set(null);

    const payload = {
      name: value.name,
      description: value.description || null,
      price: value.price,
      category: value.category.trim(),
      imageUrl: value.imageUrl || null,
      featured: value.featured,
      productionLeadTimeDays: value.productionLeadTimeDays || null,
    };

    const onError = (err: any) => {
      this.submitting.set(false);
      this.errorMessage.set(err?.error?.detail ?? 'Não foi possível salvar o produto.');
    };

    if (this.isEditMode() && this.productId) {
      const id = this.productId;
      // Gallery photos and exclusive access used to need their own "Salvar" clicks, and "Salvar
      // produto" navigated away without them — losing freshly uploaded photos. Save them together.
      const requests: Observable<unknown>[] = [this.productService.update(id, payload)];
      if (this.galleryDirty()) requests.push(this.productService.setImages(id, this.galleryImages()));
      if (this.customersDirty()) requests.push(this.productService.setAllowedCustomers(id, this.selectedCustomerIds()));
      if (value.isTest !== this.savedIsTest()) requests.push(this.productService.setTest(id, value.isTest));

      forkJoin(requests).subscribe({ next: () => this.leaveAfterSave(['/admin/produtos']), error: onError });
    } else {
      // A new product can only get gallery photos, a promotion and exclusive access once it exists,
      // so land on its edit page (instead of back on the list) to make those next steps obvious.
      this.productService.create(payload).subscribe({
        next: (product) => this.leaveAfterSave(['/admin/produtos', product.id, 'editar'], { criado: 1 }),
        error: onError,
      });
    }
  }

  private leaveAfterSave(commands: unknown[], queryParams?: Record<string, unknown>): void {
    this.leavingAfterSave = true;
    this.router.navigate(commands, { queryParams });
  }
}
