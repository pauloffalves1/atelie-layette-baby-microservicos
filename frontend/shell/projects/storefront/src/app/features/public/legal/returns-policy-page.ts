import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '@shared/core/services/seo.service';

@Component({
  selector: 'app-returns-policy-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './returns-policy-page.html',
})
export class ReturnsPolicyPage implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Política de Trocas e Devoluções',
      description:
        'Como trocar uma peça do Ateliê Layette Baby: prazo de 7 dias, condições, peça com defeito e frete da devolução.',
      path: '/politica-de-devolucao',
    });
  }
}
