import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '@shared/core/services/seo.service';

@Component({
  selector: 'app-terms-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './terms-page.html',
})
export class TermsPage implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Termos de Uso',
      description: 'Termos de uso do Ateliê Layette Baby: condições de compra, prazos de produção, pagamento e política de trocas.',
      path: '/termos-de-uso',
    });
  }
}
