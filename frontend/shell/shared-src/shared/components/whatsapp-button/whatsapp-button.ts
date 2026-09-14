import { Component, inject } from '@angular/core';
import { WHATSAPP_NUMBER } from '../../../core/constants/site';
import { CheckoutModalService } from '../../../core/services/checkout-modal.service';
import { CookieConsentService } from '../../../core/services/cookie-consent.service';

@Component({
  selector: 'app-whatsapp-button',
  standalone: true,
  templateUrl: './whatsapp-button.html',
})
export class WhatsappButton {
  readonly consent = inject(CookieConsentService);
  /** The floating button sat on top of the checkout modal's footer, covering "Confirmar pedido". */
  readonly checkoutModal = inject(CheckoutModalService);

  readonly href = `https://wa.me/${WHATSAPP_NUMBER}?text=${encodeURIComponent('Olá! Vim pelo site e gostaria de tirar uma dúvida.')}`;
}
