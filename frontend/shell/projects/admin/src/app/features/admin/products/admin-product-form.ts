import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CustomerSummary } from '@shared/core/models/customer.model';
import { CustomerAdminService } from '@shared/core/services/customer-admin.service';
import { ProductService } from '@shared/core/services/product.service';
import { resolveAssetUrl } from '@shared/core/utils/asset-url';

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
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './admin-product-form.html',
})
export class AdminProductForm implements OnInit {
  private readonly fb = inject(FormBuilder);

  readonly isEditMode = signal(false);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly customers = signal<CustomerSummary[]>([]);
  readonly selectedCustomerIds = signal<string[]>([]);
  readonly savingCustomers = signal(false);
  readonly customersSaved = signal(false);
  readonly uploadingImage = signal(false);
  readonly imageUploadError = signal<string | null>(null);

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

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    category: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0.01)]],
    imageUrl: [''],
    description: [''],
    featured: [false],
  });

  constructor(
    private readonly productService: ProductService,
    private readonly customerAdminService: CustomerAdminService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.customerAdminService.list().subscribe((customers) => this.customers.set(customers));

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    this.productId = id;
    this.isEditMode.set(true);
    this.loading.set(true);

    this.productService.getById(id).subscribe({
      next: (product) => {
        this.form.patchValue({
          name: product.name,
          category: product.category,
          price: product.price,
          imageUrl: product.imageUrl ?? '',
          description: product.description ?? '',
          featured: product.featured,
        });
        this.selectedCustomerIds.set(product.allowedCustomerIds);
        this.galleryImages.set(product.imageUrls);
        this.isOnPromotion.set(product.isOnPromotion);
        this.promotionDiscount.set(product.discountPercentage);
        this.promotionStartsAt.set(toDatetimeLocal(product.promotionStartsAt));
        this.promotionEndsAt.set(toDatetimeLocal(product.promotionEndsAt));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
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
        this.uploadingImage.set(false);
      },
      error: (err) => {
        this.uploadingImage.set(false);
        this.imageUploadError.set(err?.error?.detail ?? 'Não foi possível enviar a imagem.');
      },
    });

    input.value = '';
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
    this.productService.setImages(this.productId, this.galleryImages()).subscribe({
      next: () => {
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
    this.productService.setAllowedCustomers(this.productId, this.selectedCustomerIds()).subscribe({
      next: () => {
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
      },
      error: (err) => {
        this.savingPromotion.set(false);
        this.promotionError.set(err?.error?.detail ?? 'Não foi possível remover a promoção.');
      },
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

    const payload = {
      name: value.name,
      description: value.description || null,
      price: value.price,
      category: value.category,
      imageUrl: value.imageUrl || null,
      featured: value.featured,
    };

    const onSuccess = () => this.router.navigate(['/admin/produtos']);
    const onError = (err: any) => {
      this.submitting.set(false);
      this.errorMessage.set(err?.error?.detail ?? 'Não foi possível salvar o produto.');
    };

    if (this.isEditMode() && this.productId) {
      this.productService.update(this.productId, payload).subscribe({ next: onSuccess, error: onError });
    } else {
      this.productService.create(payload).subscribe({ next: onSuccess, error: onError });
    }
  }
}
