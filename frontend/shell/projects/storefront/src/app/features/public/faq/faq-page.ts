import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '@shared/core/services/seo.service';

@Component({
  selector: 'app-faq-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './faq-page.html',
})
export class FaqPage implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Perguntas Frequentes',
      description: 'Tire suas dúvidas sobre prazos, frete, retirada, pagamento e personalização das peças do Ateliê Layette Baby.',
      path: '/perguntas-frequentes',
    });
  }
}
