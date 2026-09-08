import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withInMemoryScrolling } from '@angular/router';

import { routes } from './app.routes';
import { authInterceptor } from '@shared/core/interceptors/auth.interceptor';

// The shell is what actually calls bootstrapApplication (see bootstrap.ts) — a remote loaded via
// loadChildren only contributes its Routes array to the shell's router, never its own
// app.config.ts providers. So HttpClient/the auth interceptor/LOCALE_ID have to live here, not (or
// not only) in each remote's own config, or every HTTP call made while running under the shell
// goes out with no Authorization header and no pt-BR formatting.
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: LOCALE_ID, useValue: 'pt-BR' },
  ],
};
