import { Routes } from '@angular/router';
import { loadRemoteModule } from '@angular-architects/native-federation';

export const routes: Routes = [
  {
    path: 'admin',
    loadChildren: () => loadRemoteModule('admin', './routes').then((m) => m.routes),
  },
  {
    path: '',
    loadChildren: () => loadRemoteModule('storefront', './routes').then((m) => m.routes),
  },
];
