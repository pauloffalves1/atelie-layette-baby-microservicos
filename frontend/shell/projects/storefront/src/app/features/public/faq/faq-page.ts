import { Component, ElementRef, Injector, OnInit, afterNextRender, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '@shared/core/services/seo.service';

@Component({
  selector: 'app-faq-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './faq-page.html',
})
export class FaqPage implements OnInit {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  constructor(private readonly seo: SeoService) {}

  ngOnInit(): void {
    this.seo.update({
      title: 'Perguntas Frequentes',
      description: 'Tire suas dúvidas sobre prazos, frete, retirada, pagamento e personalização das peças do Ateliê Layette Baby.',
      path: '/perguntas-frequentes',
    });

    // Built from the rendered <details> so the markup can never drift from the visible answers.
    afterNextRender(
      () => {
        const entries = [...this.host.nativeElement.querySelectorAll('details')].map((details) => ({
          question: details.querySelector('summary')?.textContent?.replace(/\s+/g, ' ').trim() ?? '',
          answer: [...details.querySelectorAll('p')].map((p) => p.textContent?.replace(/\s+/g, ' ').trim()).join(' '),
        }));
        this.seo.setFaq(entries.filter((e) => e.question && e.answer));
      },
      { injector: this.injector },
    );
  }
}
