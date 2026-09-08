import { registerLocaleData } from '@angular/common';
import { bootstrapApplication } from '@angular/platform-browser';
import localePt from './locale-pt';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Must happen here, not in main.ts: main.ts runs before Native Federation's import map is ready,
// so a static `@angular/common` import there fails to resolve ("Unable to resolve specifier").
// localePt itself is a local relative import (./locale-pt, not '@angular/common/locales/pt') so
// it bypasses the federation import map too — see locale-pt.ts for why.
registerLocaleData(localePt);

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
