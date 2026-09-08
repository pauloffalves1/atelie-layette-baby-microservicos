import { Component, inject } from '@angular/core';
import { WHATSAPP_NUMBER } from '../../../core/constants/site';
import { CookieConsentService } from '../../../core/services/cookie-consent.service';

@Component({
  selector: 'app-whatsapp-button',
  standalone: true,
  templateUrl: './whatsapp-button.html',
})
export class WhatsappButton {
  readonly consent = inject(CookieConsentService);

  readonly href = `https://wa.me/${WHATSAPP_NUMBER}?text=${encodeURIComponent('Olá! Vim pelo site e gostaria de tirar uma dúvida.')}`;
}
