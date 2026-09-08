import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WHATSAPP_NUMBER } from '@shared/core/constants/site';
import { AuthService } from '@shared/core/services/auth.service';
import { ContactService } from '@shared/core/services/contact.service';
import { SeoService } from '@shared/core/services/seo.service';
import { PhoneMaskDirective } from '@shared/shared/directives/phone-mask.directive';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [ReactiveFormsModule, PhoneMaskDirective],
  templateUrl: './contact.html',
})
export class Contact implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly contactService = inject(ContactService);
  private readonly seo = inject(SeoService);

  readonly pieceTypes = ['Fralda de Ombro', 'Fralda de Boca', 'Kit Ombro e Boca', 'Outro'];
  readonly sizes = ['Padrão', 'Grande', 'Sob medida'];

  readonly form = this.fb.nonNullable.group({
    isCustomOrder: [false],
    customerName: ['', Validators.required],
    customerEmail: ['', Validators.email],
    customerPhone: ['', Validators.required],
    tipoPeca: [this.pieceTypes[0]],
    tamanho: [this.sizes[0]],
    tecido: [''],
    cor: [''],
    nomeBordado: [''],
    message: ['', Validators.required],
  });

  ngOnInit(): void {
    this.seo.update({
      title: 'Contato e Encomendas',
      description: 'Fale com o Ateliê Layette Baby pelo WhatsApp para dúvidas ou para encomendar uma peça personalizada, com tecido, cor e bordado à sua escolha.',
      path: '/contato',
    });

    const user = this.auth.currentUser();
    if (user) {
      this.form.patchValue({ customerName: user.name, customerEmail: user.email });
    }
  }

  readonly recordError = signal<string | null>(null);

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    window.open(this.buildWhatsAppUrl(), '_blank', 'noopener');

    // Best-effort: also records the message so it shows up in /admin/mensagens. A failure here
    // (backend down, etc.) must never block the WhatsApp conversation, which already opened above.
    const v = this.form.getRawValue();
    this.contactService
      .submit({
        name: v.customerName,
        email: v.customerEmail || `sem-email-${v.customerPhone.replace(/\D/g, '')}@contato.local`,
        phone: v.customerPhone,
        message: this.buildMessageBody(),
      })
      .subscribe({ error: () => this.recordError.set('não foi possível registrar a mensagem no painel, mas o WhatsApp já abriu normalmente') });
  }

  private buildMessageBody(): string {
    const v = this.form.getRawValue();
    const lines: string[] = [];

    if (v.isCustomOrder) {
      lines.push('Gostaria de fazer uma encomenda personalizada:');
      lines.push(`- Tipo de peça: ${v.tipoPeca}`);
      lines.push(`- Tamanho: ${v.tamanho}`);
      if (v.tecido) lines.push(`- Tecido desejado: ${v.tecido}`);
      if (v.cor) lines.push(`- Cor: ${v.cor}`);
      if (v.nomeBordado) lines.push(`- Nome para bordar: ${v.nomeBordado}`);
      lines.push('');
    }

    lines.push(v.message);
    return lines.join('\n');
  }

  private buildWhatsAppUrl(): string {
    const v = this.form.getRawValue();
    const lines = [`Olá! Meu nome é ${v.customerName}.`, '', this.buildMessageBody()];

    if (v.customerEmail) lines.push('', `E-mail: ${v.customerEmail}`);
    lines.push(`Telefone: ${v.customerPhone}`);

    const text = encodeURIComponent(lines.join('\n'));
    return `https://wa.me/${WHATSAPP_NUMBER}?text=${text}`;
  }
}
