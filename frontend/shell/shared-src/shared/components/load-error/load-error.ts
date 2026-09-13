import { Component, input, output } from '@angular/core';

/**
 * Shown instead of a list/form when its data failed to load. Pages used to only stop the spinner,
 * so an API outage rendered the page's empty state ("Você ainda não fez nenhuma encomenda",
 * "Nenhum produto cadastrado") — or, on edit pages, a blank form that could be saved over the real
 * record. This makes the failure explicit and retryable.
 */
@Component({
  selector: 'app-load-error',
  standalone: true,
  templateUrl: './load-error.html',
})
export class LoadError {
  readonly message = input('Não foi possível carregar estas informações.');
  readonly retry = output<void>();
}
