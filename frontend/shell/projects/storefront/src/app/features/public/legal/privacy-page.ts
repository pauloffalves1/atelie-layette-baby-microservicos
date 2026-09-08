import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '@shared/core/services/seo.service';

@Component({
  selector: 'app-privacy-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './privacy-page.html',
})
export class PrivacyPage implements OnInit {
  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Política de Privacidade',
      description: 'Como o Ateliê Layette Baby coleta, usa e protege seus dados pessoais, em conformidade com a LGPD.',
      path: '/politica-de-privacidade',
    });
  }
}
