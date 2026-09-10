import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AdminPermissionName, AdminSummary } from '@shared/core/models/auth.model';
import { AdminManagementService } from '@shared/core/services/admin-management.service';
import { AdminAuthService } from '@shared/core/services/admin-auth.service';

/** One entry per AdminPermission flag (backend enum) — label is what the checkbox shows in Portuguese. */
export const PERMISSION_OPTIONS: { value: AdminPermissionName; label: string }[] = [
  { value: 'Products', label: 'Produtos' },
  { value: 'Orders', label: 'Encomendas' },
  { value: 'Coupons', label: 'Cupons' },
  { value: 'Reviews', label: 'Avaliações' },
  { value: 'ContactMessages', label: 'Mensagens de contato' },
  { value: 'Newsletter', label: 'Newsletter' },
  { value: 'Customers', label: 'Clientes' },
  { value: 'SiteContent', label: 'Imagens do site e galeria' },
  { value: 'Dashboard', label: 'Dashboard e auditoria' },
  { value: 'AdminManagement', label: 'Gerenciar administradores' },
];

@Component({
  selector: 'app-admin-admin-list',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './admin-admin-list.html',
})
export class AdminAdminList implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(AdminManagementService);
  private readonly auth = inject(AdminAuthService);

  readonly permissionOptions = PERMISSION_OPTIONS;
  readonly admins = signal<AdminSummary[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly creating = signal(false);
  readonly createError = signal<string | null>(null);

  /** id of the admin whose permissions are being edited inline, or null when none is. */
  readonly editingId = signal<string | null>(null);
  readonly savingPermissions = signal(false);
  readonly editError = signal<string | null>(null);
  readonly editSelection = signal<Set<AdminPermissionName>>(new Set());

  readonly removingId = signal<string | null>(null);
  readonly removeError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });
  readonly newAdminPermissions = signal<Set<AdminPermissionName>>(new Set());

  get currentAdminId(): string | undefined {
    return this.auth.currentUser()?.id;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.service.list().subscribe({
      next: (admins) => {
        this.admins.set(admins);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.loadError.set(
          err?.status === 403
            ? 'Você não tem permissão para gerenciar administradores.'
            : 'Não foi possível carregar os administradores.',
        );
      },
    });
  }

  toggleNewAdminPermission(permission: AdminPermissionName, checked: boolean): void {
    const next = new Set(this.newAdminPermissions());
    if (checked) next.add(permission);
    else next.delete(permission);
    this.newAdminPermissions.set(next);
  }

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.creating.set(true);
    this.createError.set(null);

    this.service
      .create({ name: value.name, email: value.email, password: value.password, permissions: [...this.newAdminPermissions()] })
      .subscribe({
        next: () => {
          this.creating.set(false);
          this.form.reset({ name: '', email: '', password: '' });
          this.newAdminPermissions.set(new Set());
          this.load();
        },
        error: (err) => {
          this.creating.set(false);
          this.createError.set(err?.error?.detail ?? 'Não foi possível cadastrar o administrador.');
        },
      });
  }

  startEditingPermissions(admin: AdminSummary): void {
    this.editingId.set(admin.id);
    this.editError.set(null);
    this.editSelection.set(new Set(admin.permissions));
  }

  cancelEditingPermissions(): void {
    this.editingId.set(null);
    this.editError.set(null);
  }

  toggleEditPermission(permission: AdminPermissionName, checked: boolean): void {
    const next = new Set(this.editSelection());
    if (checked) next.add(permission);
    else next.delete(permission);
    this.editSelection.set(next);
  }

  savePermissions(admin: AdminSummary): void {
    this.savingPermissions.set(true);
    this.editError.set(null);

    this.service.updatePermissions(admin.id, [...this.editSelection()]).subscribe({
      next: () => {
        this.savingPermissions.set(false);
        this.editingId.set(null);
        this.load();
      },
      error: (err) => {
        this.savingPermissions.set(false);
        this.editError.set(err?.error?.detail ?? 'Não foi possível atualizar as permissões.');
      },
    });
  }

  remove(admin: AdminSummary): void {
    this.removingId.set(admin.id);
    this.removeError.set(null);

    this.service.remove(admin.id).subscribe({
      next: () => {
        this.removingId.set(null);
        this.load();
      },
      error: (err) => {
        this.removingId.set(null);
        this.removeError.set(err?.error?.detail ?? 'Não foi possível remover este administrador.');
      },
    });
  }

  permissionLabel(value: AdminPermissionName): string {
    return this.permissionOptions.find((p) => p.value === value)?.label ?? value;
  }
}
