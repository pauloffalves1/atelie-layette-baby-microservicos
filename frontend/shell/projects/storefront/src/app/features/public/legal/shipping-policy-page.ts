import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '@shared/core/services/seo.service';

@Component({
  selector: 'app-shipping-policy-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './shipping-policy-page.html',
})
export class ShippingPolicyPage implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Política de Produção e Envio',
      description:
        'Como funciona a produção sob encomenda do Ateliê Layette Baby, prazos de envio e as faixas de frete grátis por região.',
      path: '/politica-de-envio',
    });
  }
}
